using System.Text;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Auth.DTOs;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LMS.Application.Features.Auth.Services;

public class InvitationService(IAppDbContext context, IConfiguration configuration, IEmailService emailService, IAuthenticationService authService) : IInvitationService
{
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IConfiguration _configuration = configuration;

    public async Task<string> InviteUserAsync(InviteUserRequest request, Guid inviterExternalId)
    {
        var inviter = await context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.ExternalId == inviterExternalId)
            ?? throw new AppException(404, "Inviter not found", "INVITER_NOT_FOUND");

        if (await context.Users.AnyAsync(u => u.Email == request.Email))
            throw new AppException(400, "This email is already registered in our system.", "EMAIL_ALREADY_EXISTS");


        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.ExternalId == request.RoleExternalId && r.TenantId == inviter.TenantId)
            ?? throw new AppException(404, "Role not found", "ROLE_NOT_FOUND");

        var position = await context.Positions
            .FirstOrDefaultAsync(p => p.ExternalId == request.PositionExternalId && p.TenantId == inviter.TenantId)
            ?? throw new AppException(404, "Position not found", "POSITION_NOT_FOUND");

        var manager = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == request.ManagerExternalId && u.TenantId == inviter.TenantId)
            ?? throw new AppException(404, "Manager not found", "MANAGER_NOT_FOUND");

        var user = new User
        {
            TenantId = inviter.TenantId,
            Name = request.Name,
            Email = request.Email,
            Gender = request.Gender,
            RoleId = role.Id,
            PositionId = position.Id,
            ManagerId = manager.Id,
            Status = UserStatus.Pending,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = Guid.NewGuid().ToString("N");
        var invite = new UserInvite
        {
            UserId = user.Id,
            Token = token,
            ExpiryDate = DateTime.UtcNow.AddHours(24)
        };
        context.UserInvites.Add(invite);
        await context.SaveChangesAsync();

        var inviteLink = $"{_configuration["App:FrontendUrl"]}/set-password?token={token}";
        _ = Task.Run(async () =>
        {
            try { await emailService.SendInviteEmailAsync(user.Email, user.Name, inviteLink); }
            catch (Exception ex) { Console.WriteLine($"[EMAIL_ERROR] Invite: {ex.Message}"); }
        });

        return token;
    }

    public async Task<string> SetPasswordAsync(SetPasswordRequest request)
    {
        var invite = await context.UserInvites
            .Include(i => i.User)
            .ThenInclude(u => u.Tenant)
            .FirstOrDefaultAsync(i => i.Token == request.Token && !i.IsUsed && i.ExpiryDate > DateTime.UtcNow);

        if (invite == null) throw new AppException(400, "Invalid or expired invite token.", "INVALID_TOKEN");

        invite.User.PasswordHash = _passwordHasher.HashPassword(invite.User, request.Password);
        invite.User.Status = UserStatus.Activated;
        invite.IsUsed = true;

        var companyLeaveTypes = await context.LeaveTypes.Where(t => t.TenantId == invite.User.TenantId).ToListAsync();

        foreach (var lt in companyLeaveTypes)
        {
            context.LeaveBalances.Add(new LeaveBalance
            {
                TenantId = invite.User.TenantId,
                UserId = invite.User.Id,
                LeaveTypeId = lt.Id,
                Balance = lt.DefaultAnnualAllowence,
                Year = DateTime.Now.Year
            });
        }

        await context.SaveChangesAsync();

        var token = authService.GenerateToken(invite.User, invite.User.Tenant.Domain);
        return await authService.CreateExchangeCodeAsync(token);
    }

    public async Task<InviteDetailsResponse> GetInviteDetailsAsync(string token)
    {
        var invite = await context.UserInvites
            .Include(i => i.User)
            .ThenInclude(u => u.Tenant)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsUsed && i.ExpiryDate > DateTime.UtcNow) ?? throw new Exception("Invalid or expired invite token.");

        return new InviteDetailsResponse(
            invite.User.Tenant.Name,
            invite.User.Tenant.Domain,
            invite.User.ExternalId.ToString(),
            invite.User.Name,
            invite.User.Email
        );
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, string subdomain)
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Domain.ToLower() == subdomain.ToLower()) ?? throw new AppException(400, "Invalid subdomain.", "INVALID_SUBDOMAIN");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id) ?? throw new AppException(404, "User not found.", "USER_NOT_FOUND");

        user.ResetPasswordToken = Guid.NewGuid().ToString("N");
        user.ResetTokenExpiresAt = DateTime.UtcNow.AddHours(24);

        await context.SaveChangesAsync();

        var resetLink = $"{_configuration["App:FrontendUrl"]}/reset-password?token={user.ResetPasswordToken}";
        _ = Task.Run(async () =>
        {
            try { await emailService.SendForgotPasswordEmailAsync(user.Email, user.Name, resetLink); }
            catch (Exception ex) { Console.WriteLine($"[EMAIL_ERROR] Reset: {ex.Message}"); }
        });

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == request.Token) ?? throw new AppException(400, "Invalid or expired token.", "INVALID_TOKEN");

        if (user.ResetTokenExpiresAt < DateTime.UtcNow)
        {
            user.ResetPasswordToken = null;
            user.ResetTokenExpiresAt = null;
            await context.SaveChangesAsync();
            throw new AppException(400, "Token has expired.", "TOKEN_EXPIRED");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.ResetPasswordToken = null;
        user.ResetTokenExpiresAt = null;

        await context.SaveChangesAsync();

        return true;
    }
}
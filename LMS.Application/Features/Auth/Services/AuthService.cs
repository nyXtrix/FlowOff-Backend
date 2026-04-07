using LMS.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LMS.Application.Features.Auth.DTOs;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LMS.Application.Common.Modals;
using LMS.Domain.Enums;
using Microsoft.AspNetCore.WebUtilities;
using LMS.Domain.Entities.Leave;

namespace LMS.Application.Features.Auth.Services;

public class AuthService(IAppDbContext context, IConfiguration configuration, IEmailService emailService) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request)
    {

        string decodeToken;
        try
        {
            var bytes = WebEncoders.Base64UrlDecode(request.RegistrationToken);
            decodeToken = Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            throw new AppException(400, "Invalid registration token format", "INVALID_TOKEN");
        }

        var lead = await context.TenantLeads.FirstOrDefaultAsync(l => l.Email == request.AdminEmail && l.RegistrationToken == decodeToken && l.TokenExpiresAt > DateTime.UtcNow);

        if(lead == null)
        {
            throw new AppException(403, "Invalid or expired registration token. Please request new invite", "UNAUTHORIZED_REGISTRATION");
        }

        if(await context.Tenants.AnyAsync(t => t.Domain.Equals(request.Subdomain, StringComparison.CurrentCultureIgnoreCase)))
        {
            throw new AppException(400, "Subdomain already exists.", "SUBDOMAIN_ALREADY_EXISTS");
        }
        var tenant = new Tenant
        {
            Name = request.CompanyName,
            Domain = request.Subdomain.ToLower()
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var adminRole = new Role
        {
            TenantId = tenant.Id,
            Name = "Super Admin",
            Level = 100
        };
        context.Roles.Add(adminRole);
        await context.SaveChangesAsync();

        var adminPosition = new Position
        {
            TenantId = tenant.Id,  
            RoleId = adminRole.Id,
            Name = "Company Administrator",
            Level = 100
        };
        context.Positions.Add(adminPosition);
        await context.SaveChangesAsync();

        var admin = new User
        {
            TenantId = tenant.Id,
            Name = request.AdminName,
            Email = request.AdminEmail,
            Status = UserStatus.Activated,
            PositionId = adminPosition.Id,
            RoleId = adminRole.Id
        };
        
        admin.PasswordHash = _passwordHasher.HashPassword(admin, request.AdminPassword);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var loginLink = $"{_configuration["App:FrontendUrl"]}/{tenant.Domain}/login";
        await emailService.SendWelcomeEmailAsync(admin.Email, admin.Name, tenant.Name, loginLink);

        context.TenantLeads.Remove(lead);
        await context.SaveChangesAsync();

        return new AuthResponse(
            GenerateJwtToken(admin, tenant.Domain,3),
            admin.Name,
            admin.Email,
            tenant.Domain
        );
    }

    public async Task<string> InviteUserAsync(InviteUserRequest request, int adminUserId)
    {
        var admin = await context.Users.FindAsync(adminUserId) ?? throw new Exception("Admin not found.");

        var user = new User
        {
            TenantId = admin.TenantId,
            Name = request.Name,
            Email = request.Email,
            Status = UserStatus.Pending,
            PositionId = 1 
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
        await emailService.SendInviteEmailAsync(user.Email, user.Name, inviteLink);

        return token;
    }

    public async Task<AuthResponse?> SetPasswordAsync(SetPasswordRequest request)
    {
        var invite = await context.UserInvites
            .Include(i => i.User)
            .ThenInclude(u => u.Tenant)
            .FirstOrDefaultAsync(i => i.Token == request.Token && !i.IsUsed && i.ExpiryDate > DateTime.UtcNow);

        if (invite == null) return null;

        invite.User.PasswordHash = _passwordHasher.HashPassword(invite.User, request.Password);
        invite.User.Status = UserStatus.Activated;
        invite.IsUsed = true;

        var companyLeaveTypes = await context.LeaveTypes.Where(t => t.TenantId == invite.User.TenantId).ToListAsync();

        foreach(var lt in companyLeaveTypes)
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
        
        return new AuthResponse(
            GenerateJwtToken(invite.User, invite.User.Tenant.Domain, 3),
            invite.User.Name,
            invite.User.Email,
            invite.User.Tenant.Domain
        );
    }

    public async Task<InviteDetailsResponse> GetInviteDetailsAsync(string token)
    {
        var invite = await context.UserInvites
            .Include(i => i.User)
            .ThenInclude(u => u.Tenant) 
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsUsed && i.ExpiryDate > DateTime.UtcNow) ?? throw new Exception("Invalid or expired invite token.");

        return new InviteDetailsResponse(
            invite.User.Tenant.Name,
            invite.User.Name,
            invite.User.Email
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string subdomain)
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Domain.ToLower() == subdomain.ToLower()) ?? throw new AppException(400, "Invalid subdomain.", "INVALID_SUBDOMAIN");

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id) ?? throw new AppException(401, "Invalid credentials.", "INVALID_CREDENTIALS");

        if (user.Status != UserStatus.Activated) 
            throw new AppException(403, "Account not activated. Please use the link sent to your email.", "ACCOUNT_NOT_ACTIVATED");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password);
        if (result == PasswordVerificationResult.Failed) 
            throw new AppException(401, "Invalid credentials.", "INVALID_CREDENTIALS");

        return new AuthResponse(
            GenerateJwtToken(user, tenant.Domain,7),
            user.Name,
            user.Email,
            tenant.Domain
        );
    }

    private string GenerateJwtToken(User user, string subdomain, int days)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("TenantId", user.TenantId.ToString()),
            new("Subdomain", subdomain)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "a_very_long_secret_key_that_is_at_least_32_chars_long"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(days),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, string subdomain)
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Domain.ToLower() == subdomain.ToLower()) ?? throw new AppException(400, "Invalid subdomain.", "INVALID_SUBDOMAIN");
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id) ?? throw new AppException(404, "User not found.", "USER_NOT_FOUND");

        user.ResetPasswordToken = Guid.NewGuid().ToString("N");
        user.ResetTokenExpiresAt = DateTime.UtcNow.AddHours(24);

        await context.SaveChangesAsync();

        var resetLink = $"{_configuration["App:FrontendUrl"]}/reset-password?token={user.ResetPasswordToken}";
        await emailService.SendForgotPasswordEmailAsync(user.Email, user.Name, resetLink);

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

    public async Task<AuthResponse> GetCurrentUserAsync(int userId)
    {
        var user = await context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new AppException(404, "User not found.", "USER_NOT_FOUND");

        return new AuthResponse(
            null!,
            user.Name,
            user.Email,
            user.Tenant.Domain
        );
    }

    public async Task SubmitContactInquiryAsync(ContactRequest request)
    {
        var lead = new TenantLead
        {
            Name = request.Name,
            Email = request.Email,
            Message = request.Message,
            Purpose = request.Purpose,
        };
        context.TenantLeads.Add(lead);

        var appUrl =  _configuration["App:FrontendUrl"] ?? "http://localhost:5173";

        await context.SaveChangesAsync();

        if(request.Purpose == InquiryPurpose.GetLms)
        {
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(lead.RegistrationToken));
            
            var setupLink = $"{appUrl}/register?token={encodedToken}";

            await emailService.SendEmailAsync(lead.Email, "Finish Settingx   Up your LMS!",
            $"<h1>Welcome {lead.Name}!</h1> <p>Set up your company here: <a href='{setupLink}'>Get Started</a></p>"
            );
        }
        
    }
}

using System.Text;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Auth.DTOs;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using LMS.Application.Common.Email;

namespace LMS.Application.Features.Auth.Services;

public class OnboardingService(IAppDbContext context, IConfiguration configuration, IEmailService emailService, IAuthenticationService authService) : IOnboardingService
{
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IConfiguration _configuration = configuration;

    public async Task<string> RegisterCompanyAsync(RegisterCompanyRequest request)
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

        var lead = await context.TenantLeads.FirstOrDefaultAsync(l =>
            l.Email.ToLower() == request.AdminEmail.ToLower() &&
            l.RegistrationToken == decodeToken &&
            l.TokenExpiresAt > DateTime.UtcNow);

        if (lead == null)
        {
            throw new AppException(403, "Invalid or expired registration token. Please request new invite", "UNAUTHORIZED_REGISTRATION");
        }

        var normalizedDomain = request.Subdomain.ToLower();

        if (await context.Tenants.AnyAsync(t => t.Domain == normalizedDomain))
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
        };
        context.Roles.Add(adminRole);
        await context.SaveChangesAsync();

        var adminPosition = new Position
        {
            TenantId = tenant.Id,
            RoleId = adminRole.Id,
            Name = "Company Administrator",
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
        _ = Task.Run(async () =>
        {
            try { await emailService.SendWelcomeEmailAsync(admin.Email, admin.Name, tenant.Name, loginLink); }
            catch (Exception ex) { Console.WriteLine($"[EMAIL_ERROR] Welcome: {ex.Message}"); }
        });

        context.TenantLeads.Remove(lead);
        await context.SaveChangesAsync();

        return await authService.LoginAsync(new LoginRequest(request.AdminEmail, request.AdminPassword), tenant.Domain);
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

        var appUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";

        await context.SaveChangesAsync();

        if (request.Purpose == InquiryPurpose.GetLms)
        {
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(lead.RegistrationToken));
            var setupLink = $"{appUrl}/register?token={encodedToken}";
            var htmlMessage = EmailTemplates.GetLeadInquirySetupTemplate(lead.Name, setupLink);

            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendEmailAsync(lead.Email, "Finish Setting Up your LMS!", htmlMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BACKGROUND_EMAIL_ERROR] {ex.Message}");
                }
            });
        }

    }

    public async Task<TenantLeadDetailsResponse> GetTenantLeadDetailsAsync(string token)
    {
        var bytes = WebEncoders.Base64UrlDecode(token);
        var RegistrationToken = Encoding.UTF8.GetString(bytes);

        var lead = await context.TenantLeads.FirstOrDefaultAsync(l => l.RegistrationToken == RegistrationToken && l.TokenExpiresAt > DateTime.UtcNow) ??
                          throw new AppException(404, "Invalid or expired registration token.", "INVALID_TOKEN");

        return new TenantLeadDetailsResponse(lead.Name, lead.Email);
    }

}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Auth.DTOs;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Application.Features.Auth.Services;

public class AuthenticationService(IAppDbContext context, IConfiguration configuration, ICacheService cache) : IAuthenticationService
{
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IConfiguration _configuration = configuration;
    private readonly ICacheService _cache = cache;

    public async Task<string> LoginAsync(LoginRequest request, string subdomain)
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Domain.ToLower() == subdomain.ToLower()) ?? throw new AppException(400, "Invalid subdomain.", "INVALID_SUBDOMAIN");

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id) ?? throw new AppException(401, "Invalid credentials.", "INVALID_CREDENTIALS");

        if (user.Status != UserStatus.Activated)
            throw new AppException(403, "Account not activated. Please use the link sent to your email.", "ACCOUNT_NOT_ACTIVATED");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new AppException(401, "Invalid credentials.", "INVALID_CREDENTIALS");

        var token = GenerateToken(user, tenant.Domain);
        return await CreateExchangeCodeAsync(token);
    }

    public async Task<AuthResponse> GetCurrentUserAsync(Guid externalId)
    {
        var cacheKey = $"upr_{externalId}";
        var cached = await _cache.GetAsync<AuthResponse>(cacheKey);
        if (cached != null) return cached;

        var user = await context.Users
            .Include(u => u.Tenant)
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permissions)
            .Include(u => u.UserPermissionOverrides)
                .ThenInclude(upo => upo.Permissions)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId)
                ?? throw new AppException(404, "User not found.", "USER_NOT_FOUND");

        var grantedPermissions = user.Role.RolePermissions.Select(rp => rp.Permissions.Name).ToHashSet();
        foreach (var ov in user.UserPermissionOverrides)
        {
            if (ov.IsAllowed) grantedPermissions.Add(ov.Permissions.Name);
            else grantedPermissions.Remove(ov.Permissions.Name);
        }
        var appPermissions = new AppPermissions();
        foreach (var name in grantedPermissions)
        {
            var parts = name.Split('.');
            if (parts.Length != 2) continue;

            var moduleKey = parts[0];
            if (!Enum.TryParse<ActionType>(parts[1], out var actionType)) continue;

            if (!appPermissions.ContainsKey(moduleKey))
                appPermissions[moduleKey] = new ModulePermission { Scope = user.Role.Scope };

            appPermissions[moduleKey].Actions.Add(actionType);
        }

        var hasReportees = await context.Users.AnyAsync(u => u.ManagerId == user.Id);
        if (hasReportees)
        {
            if (!appPermissions.ContainsKey("APPROVALS"))
            {
                appPermissions["APPROVALS"] = new ModulePermission { Scope = ScopeType.TEAM };
            }
            else if (appPermissions["APPROVALS"].Scope == ScopeType.SELF)
            {
                appPermissions["APPROVALS"].Scope = ScopeType.TEAM;
            }

            var actions = appPermissions["APPROVALS"].Actions;

            if (!actions.Contains(ActionType.VIEW)) actions.Add(ActionType.VIEW);
            if (!actions.Contains(ActionType.APPROVE)) actions.Add(ActionType.APPROVE);
            if (!actions.Contains(ActionType.REJECT)) actions.Add(ActionType.REJECT);
        }

        var response = new AuthResponse(
            user.ExternalId.ToString(),
            user.FirstName,
            user.LastName,
            user.Email,
            user.Tenant.Domain,
            user.Gender,
            user.Status,
            user.Role.Name,
            appPermissions,
            user.Tenant.Name
        );

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
        return response;
    }



    public async Task<IdentifyResponse> IdentifyUserAsync(string email)
    {
        var cacheKey = $"id_{email.ToLower()}";
        var cached = await _cache.GetAsync<IdentifyResponse>(cacheKey);

        if (cached != null) return cached;

        var user = await context.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null) return new IdentifyResponse(null, UserLoginStatus.NotFound, false);

        var loginStatus = user.Status switch
        {
            UserStatus.Activated => UserLoginStatus.Active,
            UserStatus.Pending => UserLoginStatus.SetPasswordRequired,
            UserStatus.InActive or UserStatus.Terminated => UserLoginStatus.Inactive,
            _ => UserLoginStatus.NotFound
        };


        var response = new IdentifyResponse(user.Tenant.Domain, loginStatus, true);

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }

    public async Task<string> CreateExchangeCodeAsync(string token)
    {
        var code = Guid.NewGuid().ToString("N");
        await _cache.SetAsync($"exchange_{code}", token, TimeSpan.FromSeconds(60));
        return code;
    }

    public async Task<string> ExchangeCodeAsync(string code)
    {
        var token = await _cache.GetAsync<string>($"exchange_{code}");
        if (string.IsNullOrEmpty(token)) throw new AppException(400, "Invalid or expired exchange code", "INVALID_CODE");

        await _cache.RemoveAsync($"exchange_{code}");
        return token;
    }

    public string GenerateToken(User user, string subdomain)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ExternalId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("Subdomain", subdomain),
            new("TenantId", user.TenantId.ToString())
        };


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "a_very_long_secret_key_that_is_at_least_32_chars_long"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
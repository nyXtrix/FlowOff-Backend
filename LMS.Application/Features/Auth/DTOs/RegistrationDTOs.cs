using LMS.Domain.Enums;

namespace LMS.Application.Features.Auth.DTOs;

public record RegisterCompanyRequest(string RegistrationToken, string CompanyName, string Subdomain, string AdminName, string AdminEmail, string AdminPassword);
public record TenantLeadDetailsResponse(string Name, string Email);
public record ContactRequest(string Name, string Email, string Message, InquiryPurpose Purpose);

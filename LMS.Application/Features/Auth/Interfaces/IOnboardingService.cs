using LMS.Application.Features.Auth.DTOs;

namespace LMS.Application.Features.Auth.Interfaces;

public interface IOnboardingService
{
    Task<string> RegisterCompanyAsync(RegisterCompanyRequest request);
    Task SubmitContactInquiryAsync(ContactRequest request);
    Task<TenantLeadDetailsResponse> GetTenantLeadDetailsAsync(string token);
}
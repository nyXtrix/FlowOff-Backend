using Microsoft.AspNetCore.Mvc;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Application.Features.Auth.DTOs;
using FluentValidation;
using LMS.Application.Common.Modals;

namespace LMS.API.Controllers.Auth;


[ApiController]
[Route("api/v1/auth/[controller]")]
public class OnboardingController(IOnboardingService onboardingService) : ControllerBase
{


    [HttpPost("register-company")]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyRequest request)
    {
        var exchangeCode = await onboardingService.RegisterCompanyAsync(request);
        return Ok(new LoginResponse(exchangeCode));
    }

    [HttpPost("contact")]
    public async Task<IActionResult> SubmitInquiry([FromBody] ContactRequest request)
    {
        await onboardingService.SubmitContactInquiryAsync(request);

        return Ok(new { message = "Inquiry received" });
    }

    [HttpGet("lead-details")]
    public async Task<IActionResult> GetLeadDetails([FromQuery] string token)
    {
        var result = await onboardingService.GetTenantLeadDetailsAsync(token);

        return Ok(result);
    }

}
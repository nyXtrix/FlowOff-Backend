using LMS.Application.Features.Dashboard.DTOs;

namespace LMS.Application.Features.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<UserDashboardResponse> GetUserDashboardAsync(Guid userExternalId, int tenantId);
    Task<UserDashboardResponse> GetAdminDashboardAsync(int tenantId);
}

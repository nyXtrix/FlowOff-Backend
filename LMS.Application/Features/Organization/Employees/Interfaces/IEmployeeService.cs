using LMS.Application.Common.DTOs;
using LMS.Application.Features.Employees.DTOs;
using LMS.Domain.Entities.Auth;

namespace LMS.Application.Features.Employees.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeLookupResponse>> GetEmployeeLookupsAsync(string query, int tenantId);
    Task<PaginatedResult<EmployeeListResponse>> GetEmployeesAsync(QueryRequest request, Guid userExternalId, int tenantId);
    Task<List<EmployeeListResponse>> GetRecentInvitesAsync(int tenantId);
    Task<EmployeeProfileResponse> GetEmployeeProfileAsync(Guid employeeExternalId, Guid userExternalId, int tenantId);
}

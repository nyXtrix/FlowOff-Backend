
using LMS.Application.Common.DTOs;
using LMS.Application.Features.Organization.Department.DTOs;

namespace LMS.Application.Features.Organization.Department.Interfaces;

public interface IDepartmentService
{
    Task<Guid> CreateDepartmentAsync(CreateDepartmentRequest request, int tenantId);
    Task UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, int tenantId);
    Task DeleteDepartmentAsync(Guid id, int tenantId);
    Task<PaginatedResult<DepartmentManagementResponse>> GetDepartmentManagementAsync(int tenantId, QueryRequest request);
}
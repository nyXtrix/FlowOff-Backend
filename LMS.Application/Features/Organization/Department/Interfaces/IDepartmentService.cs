
using LMS.Application.Features.Organization.Department.DTOs;

namespace LMS.Application.Features.Organization.Department.Interfaces;

public interface IDepartmentService
{
    Task<Guid> CreateDepartmentAsync(CreateDepartmentRequest request, int tenantId);
    Task<List<GetDepartments>> GetDepartmentsAsync(int tenantId);
}
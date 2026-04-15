using LMS.Application.Features.Employees.DTOs;

namespace LMS.Application.Features.Employees.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeLookupResponse>> GetEmployeeLookupsAsync(string query, int tenantId);
}

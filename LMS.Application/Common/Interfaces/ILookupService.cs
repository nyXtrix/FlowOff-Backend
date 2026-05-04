namespace LMS.Application.Common.Interfaces;

public record LookupResponse(string Label, string Value);

public interface ILookupService
{
    Task <List<LookupResponse>> GetGenderLookupAsync();
    Task<List<LookupResponse>> GetLeaveTypeLookupAsync(int tenantId);
    Task<List<LookupResponse>> GetDepartmentLookupAsync(int tenantId);
    Task<List<LookupResponse>> GetRoleLookupAsync(int tenantId);
}
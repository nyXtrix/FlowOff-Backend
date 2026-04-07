namespace LMS.Application.Features.Leaves.DTOs;

public record CreateLeaveTypeRequest(
    string Name,
    string? Description,
    int? MaxCancelableStep,
    int DefaultAnnualAllowence
);
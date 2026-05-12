using LMS.Application.Common.DTOs;

namespace LMS.Application.Features.Organization.Team.DTOs;

public record TeamSummaryResponse(
    int TotalMembers,
    int OnLeaveToday,
    int AvailableToday,
    bool IsPublicHoliday,
    string? HolidayName,
    bool IsWeekOff
);

public record TeamMemberResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string DepartmentName,
    string RoleName,
    string Status,
    DateTime JoinedDate,
    bool OnLeaveToday,
    string? LeaveType,
    DateTime LeaveReturnDate
);

public record TeamResponse(
    TeamSummaryResponse Summary,
    PaginatedResult<TeamMemberResponse> TeamMembers
);

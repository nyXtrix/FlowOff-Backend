using LMS.Domain.Enums.Users;

namespace LMS.Application.Features.Auth.DTOs;

public record BulkUserInviteResponse(Guid BulkInviteUserExternalID, string FileName, int TotalRows, BulkInvitedUserStatus Status);

public record BulkUserInvitedStatus(int TotalRows, int ProcessedRows, int SuccessCount, int FailureCount, BulkInvitedUserStatus Status);
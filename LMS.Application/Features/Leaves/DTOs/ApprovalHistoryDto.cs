namespace LMS.Application.Features.Leaves.DTOs;

public record ApprovalHistoryDto(
    int Sequence,
    string ApproverName,
    string Status,
    string? Comments,
    DateTime? ActionDate
);
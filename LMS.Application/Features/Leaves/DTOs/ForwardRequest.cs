namespace LMS.Application.Features.Leaves.DTOs;

public record ForwardRequest(
    int ApprovelId,
    int NextApproverId
);
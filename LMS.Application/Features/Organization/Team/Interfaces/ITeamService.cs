using LMS.Application.Common.DTOs;
using LMS.Application.Features.Organization.Team.DTOs;

namespace LMS.Application.Features.Organization.Team.Interfaces;

public interface ITeamService
{
    Task<TeamResponse> GetTeamAsync(Guid userExternalId, int tenantId, QueryRequest request);
}
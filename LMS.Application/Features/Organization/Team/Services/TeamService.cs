using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Common.Security;
using LMS.Application.Features.Organization.Team.DTOs;
using LMS.Application.Features.Organization.Team.Interfaces;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Organization.Team.Services;

public class TeamService(IAppDbContext context, IPermissionResolver permissionResolver, ICacheService cache) : ITeamService
{
    public async Task<TeamResponse> GetTeamAsync(Guid userExternalId, int tenantId, QueryRequest request)
    {
        var cacheKey = $"team_{userExternalId}_{request.Page}_{request.PageSize}_{request.SearchTerm}_{string.Join("_", request.Filters?.Select(f => f.Key + f.Value) ?? new List<string>())}";
        var cached = await cache.GetAsync<TeamResponse>(cacheKey);
        if (cached != null) return cached;

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId && u.TenantId == tenantId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var permissions = await permissionResolver.ResolveForUserAsync(user);

        if (!permissions.TryGetValue("TEAM", out var teamPermission))
        {
            throw new AppException(403, "Access denied for team module", "FORBIDDEN");
        }

        var query = context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Where(u => u.TenantId == tenantId && u.Status == UserStatus.Activated && u.Id != user.Id)
            .AsQueryable();

        if (teamPermission.Scope == ScopeType.TEAM)
        {
            query = query.Where(u => u.ManagerId == user.Id);
        }
        else if (teamPermission.Scope == ScopeType.DEPARTMENT)
        {
            query = query.Where(u => u.DepartmentId == user.DepartmentId);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(search) || u.LastName.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
        }

        var teamUserIds = await query.Select(u => u.Id).ToListAsync();
        var today = DateTime.UtcNow.Date;

        var onLeaveRequests = await context.LeaveRequests
            .Include(r => r.LeaveType)
            .Where(r => teamUserIds.Contains(r.UserId) && r.Status == LeaveStatus.Approved
                        && r.StartDate.Date <= today && r.EndDate.Date >= today)
            .ToListAsync();

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.TenantId == tenantId && h.Date == today);
        var allHolidays = await context.Holidays.Where(h => h.TenantId == tenantId && h.Date >= today).Select(h => h.Date.Date).ToListAsync();

        var weekOffDays = new HashSet<int>();
        if (tenant != null && !string.IsNullOrWhiteSpace(tenant.WeekoffDays))
        {
            weekOffDays = tenant.WeekoffDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => int.TryParse(s, out var d) ? d : -1)
                                  .Where(d => d != -1)
                                  .ToHashSet();
        }
        var isWeekOff = weekOffDays.Contains((int)today.DayOfWeek);

        var totalCount = await query.CountAsync();
        var pagedUsers = await query.OrderBy(u => u.FirstName).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

        var items = pagedUsers.Select(u =>
        {
            var activeLeave = onLeaveRequests.FirstOrDefault(r => r.UserId == u.Id);
            var backOnDate = DateTime.MinValue;

            if (activeLeave != null)
            {
                backOnDate = activeLeave.EndDate.Date.AddDays(1);
                while (weekOffDays.Contains((int)backOnDate.DayOfWeek) || allHolidays.Contains(backOnDate.Date))
                {
                    backOnDate = backOnDate.AddDays(1);
                }
            }

            return new TeamMemberResponse(
                u.ExternalId,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Department?.Name ?? "N/A",
                u.Role?.Name ?? "N/A",
                (int)u.Status,
                u.CreatedAt,
                activeLeave != null,
                activeLeave?.LeaveType?.Name,
                backOnDate
            );
        }).ToList();

        var response = new TeamResponse(
            new TeamSummaryResponse(
                teamUserIds.Count,
                onLeaveRequests.Count,
                teamUserIds.Count - onLeaveRequests.Count,
                holiday != null,
                holiday?.Name,
                isWeekOff
            ),
            new PaginatedResult<TeamMemberResponse>(items, totalCount)
        );

        await cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
        return response;
    }
}

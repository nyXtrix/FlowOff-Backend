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

public class TeamServices(IAppDbContext context, IPermissionResolver permissionResolver) : ITeamService
{
    public async Task<TeamResponse> GetTeamAsync(Guid userExternalId, int tenantId, QueryRequest request)
    {

        var today = DateTime.UtcNow.Date;

        var user = await context.Users.Include(u => u.Role)
                        .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permissions)
                        .Include(u => u.UserPermissionOverrides).ThenInclude(ov => ov.Permissions).FirstOrDefaultAsync(u => u.ExternalId == userExternalId && u.TenantId == tenantId)
                         ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var permission = await permissionResolver.ResolveForUserAsync(user);

        if (!permission.TryGetValue("TEAM", out var teamPermission))
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

        var teamUserIds = await query.Select(u => u.Id).ToListAsync() ?? new List<int>();

        var onLeaveRequests = await context.LeaveRequests.Include(r => r.LeaveType).Where(r => teamUserIds.Contains(r.UserId) && r.Status == LeaveStatus.Approved
                                                   && r.StartDate.Date <= today && r.EndDate.Date >= today).ToListAsync() ?? new List<LeaveRequest>();

        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.TenantId == tenantId && h.Date == today);

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        var isWeekOff = false;
        if (tenant != null && !string.IsNullOrWhiteSpace(tenant.WeekoffDays))
        {
            var weekOffDays = tenant.WeekoffDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => int.TryParse(s, out var d) ? d : -1)
                                  .ToHashSet();
            isWeekOff = weekOffDays.Contains((int)today.DayOfWeek);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(search) || u.LastName.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();
        var pagedUser = await query.OrderBy(u => u.FirstName).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

        var items = pagedUser.Select(u =>
          {
              var activeLeave = onLeaveRequests.FirstOrDefault(r => r.UserId == u.Id);

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
                 activeLeave?.EndDate ?? DateTime.MinValue
                );
          }).ToList();


        return new TeamResponse(
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
    }
}
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Dashboard.DTOs;
using LMS.Application.Features.Dashboard.Interfaces;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Dashboard.Services;

public class DashboardService(IAppDbContext context) : IDashboardService
{
    public async Task<UserDashboardResponse> GetUserDashboardAsync(Guid userExternalId, int tenantId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new Exception("User not found");

        var currentYear = DateTime.UtcNow.Year;
        
        var leaveBalances = await context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.UserId == user.Id && b.Year == currentYear)
            .ToListAsync();

        var balanceItems = leaveBalances.Select(b => new LeaveBalanceItem
        {
            LeaveType = b.LeaveType.Name,
            Consumed = b.LeaveType.DefaultAnnualAllowence - b.Balance,
            Total = b.LeaveType.DefaultAnnualAllowence
        }).ToList();

        var pendingRequestsCount = await context.LeaveRequests
            .Where(r => r.UserId == user.Id && (r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.InProgress))
            .CountAsync();
        var upcomingHoliday = await context.Holidays
            .Where(h => h.TenantId == tenantId && h.Date >= DateTime.UtcNow.Date)
            .OrderBy(h => h.Date)
            .FirstOrDefaultAsync();

        decimal usedThisYear = balanceItems.Sum(b => b.Consumed);
        decimal totalRemaining = leaveBalances.Sum(b => b.Balance);
        
        var statCards = new List<StatCardDto>
        {
            new StatCardDto
            {
                Type = "PRIMARY_BALANCE",
                Title = "Total Balance",
                Value = totalRemaining,
                Subtitle = "Remaining days"
            },
            new StatCardDto
            {
                Type = "USED_THIS_YEAR",
                Title = "Used This Year",
                Value = usedThisYear,
                Subtitle = "Across all types"
            },
            new StatCardDto
            {
                Type = "PENDING_REQUESTS",
                Title = "Pending Requests",
                Value = pendingRequestsCount,
                Subtitle = "Awaiting approval"
            },
            new StatCardDto
            {
                Type = "UPCOMING_HOLIDAY",
                Title = "Upcoming Holiday",
                Value = upcomingHoliday != null ? upcomingHoliday.Name : "None",
                Subtitle = upcomingHoliday != null ? upcomingHoliday.Date.ToString("MMM d, yyyy") : ""
            }
        };

        var recentRequests = await context.LeaveRequests
            .Include(r => r.LeaveType)
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.UpdatedAt)
            .Take(5)
            .ToListAsync();

        var activities = new List<RecentActivityItem>();

        foreach (var req in recentRequests)
        {
            string type = req.Status switch
            {
                LeaveStatus.Pending => "submitted",
                LeaveStatus.InProgress => "submitted",
                LeaveStatus.Approved => "approved",
                LeaveStatus.Cancelled => "cancelled",
                LeaveStatus.Rejected => "rejected",
                _ => "update"
            };

            string title = req.Status switch
            {
                LeaveStatus.Pending => "Leave request submitted",
                LeaveStatus.InProgress => "Leave request submitted",
                LeaveStatus.Approved => "Leave approved",
                LeaveStatus.Cancelled => "Leave cancelled",
                LeaveStatus.Rejected => "Leave rejected",
                _ => "Leave request updated"
            };

            string description = req.Status switch
            {
                LeaveStatus.Pending => $"You submitted a {req.LeaveType.Name} request for {req.StartDate:MMM d}.",
                LeaveStatus.InProgress => $"You submitted a {req.LeaveType.Name} request for {req.StartDate:MMM d}.",
                LeaveStatus.Approved => $"Your {req.LeaveType.Name} request for {req.StartDate:MMM d} was approved.",
                LeaveStatus.Cancelled => $"You cancelled your {req.LeaveType.Name} request.",
                LeaveStatus.Rejected => $"Your {req.LeaveType.Name} request was rejected.",
                _ => $"Your {req.LeaveType.Name} request status changed."
            };

            activities.Add(new RecentActivityItem
            {
                Id = $"req-{req.ExternalId}",
                Type = type,
                Title = title,
                Description = description,
                Timestamp = req.UpdatedAt
            });
        }

        var recentHolidays = await context.Holidays
            .Where(h => h.TenantId == tenantId && h.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(h => h.CreatedAt)
            .Take(3)
            .ToListAsync();

        foreach (var hol in recentHolidays)
        {
            activities.Add(new RecentActivityItem
            {
                Id = $"hol-{hol.ExternalId}",
                Type = "holiday",
                Title = "Holiday announced",
                Description = $"Company-wide holiday announced for {hol.Name}.",
                Timestamp = hol.CreatedAt
            });
        }

        activities = activities.OrderByDescending(a => a.Timestamp).Take(5).ToList();

        return new UserDashboardResponse
        {
            StatCards = statCards,
            LeaveBalances = balanceItems,
            RecentActivities = activities
        };
    }

    public async Task<UserDashboardResponse> GetAdminDashboardAsync(int tenantId)
    {
        var today = DateTime.UtcNow.Date;
        var currentYear = DateTime.UtcNow.Year;

        var totalEmployees = await context.Users
            .Where(u => u.TenantId == tenantId && u.Status == UserStatus.Activated && u.Role.Code != "SUPER_ADMIN")
            .CountAsync();

        var departmentCount = await context.Departments
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .CountAsync();

        var activeLeavesToday = await context.LeaveRequests
            .Where(r => r.User.TenantId == tenantId
                     && r.Status == LeaveStatus.Approved
                     && r.StartDate.Date <= today
                     && r.EndDate.Date >= today)
            .CountAsync();

        var pendingApprovals = await context.LeaveRequests
            .Where(r => r.User.TenantId == tenantId
                     && (r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.InProgress))
            .CountAsync();

        decimal orgHealth = totalEmployees > 0
            ? Math.Round((decimal)(totalEmployees - activeLeavesToday) / totalEmployees * 100, 1)
            : 100;

        var statCards = new List<StatCardDto>
        {
            new StatCardDto
            {
                Type = "TOTAL_EMPLOYEES",
                Title = "Total Employees",
                Value = totalEmployees,
                Subtitle = $"Across {departmentCount} departments"
            },
            new StatCardDto
            {
                Type = "ACTIVE_LEAVES",
                Title = "Active Leaves Today",
                Value = activeLeavesToday,
                Subtitle = totalEmployees > 0
                    ? $"{Math.Round((decimal)activeLeavesToday / totalEmployees * 100, 1)}% of workforce"
                    : "0% of workforce"
            },
            new StatCardDto
            {
                Type = "PENDING_APPROVALS",
                Title = "Pending Approvals",
                Value = pendingApprovals,
                Subtitle = "Requires attention"
            },
            new StatCardDto
            {
                Type = "ORG_HEALTH",
                Title = "Org Health",
                Value = $"{orgHealth}%",
                Subtitle = "Availability score"
            }
        };

        var usersWithBalances = await context.Users
            .Include(u => u.Department)
            .Where(u => u.TenantId == tenantId && u.Status == UserStatus.Activated)
            .Select(u => new
            {
                DepartmentName = u.Department != null ? u.Department.Name : "No Department",
                Balances = context.LeaveBalances
                    .Include(b => b.LeaveType)
                    .Where(b => b.UserId == u.Id && b.Year == currentYear)
                    .ToList()
            })
            .ToListAsync();

        var leaveBalances = usersWithBalances
            .GroupBy(u => u.DepartmentName)
            .Select(g => new LeaveBalanceItem
            {
                LeaveType = g.Key,
                Consumed = g.SelectMany(u => u.Balances)
                            .Sum(b => b.LeaveType.DefaultAnnualAllowence - b.Balance),
                Total = g.SelectMany(u => u.Balances)
                          .Sum(b => b.LeaveType.DefaultAnnualAllowence)
            })
            .OrderByDescending(d => d.Consumed)
            .ToList();

        var activities = new List<RecentActivityItem>();

        var recentRequests = await context.LeaveRequests
            .Include(r => r.LeaveType)
            .Include(r => r.User)
            .Where(r => r.User.TenantId == tenantId
                     && (r.Status == LeaveStatus.Approved || r.Status == LeaveStatus.Rejected))
            .OrderByDescending(r => r.UpdatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var req in recentRequests)
        {
            var fullName = $"{req.User.FirstName} {req.User.LastName}";
            var type = req.Status == LeaveStatus.Approved ? "approved" : "rejected";
            var title = req.Status == LeaveStatus.Approved ? "Leave Approved" : "Leave Rejected";
            var description = req.Status == LeaveStatus.Approved
                ? $"{fullName}'s {req.LeaveType.Name} request was approved."
                : $"{fullName}'s {req.LeaveType.Name} request was rejected.";

            activities.Add(new RecentActivityItem
            {
                Id = $"req-{req.ExternalId}",
                Type = type.ToUpper(),
                Title = title,
                Description = description,
                Timestamp = req.UpdatedAt
            });
        }

        var recentInvites = await context.UserInvites
            .Include(i => i.User)
            .Where(i => i.User.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(3)
            .ToListAsync();

        foreach (var invite in recentInvites)
        {
            activities.Add(new RecentActivityItem
            {
                Id = $"inv-{invite.ExternalId}",
                Type = "INVITED",
                Title = "New Employee Invited",
                Description = $"{invite.User.Email} was invited to join the organization.",
                Timestamp = invite.CreatedAt
            });
        }

        activities = activities.OrderByDescending(a => a.Timestamp).Take(5).ToList();

        return new UserDashboardResponse
        {
            StatCards = statCards,
            LeaveBalances = leaveBalances,
            RecentActivities = activities
        };
    }
}

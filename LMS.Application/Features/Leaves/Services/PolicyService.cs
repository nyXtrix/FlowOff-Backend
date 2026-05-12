using System.Text.Json;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Organization;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Policy;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class PolicyService(IAppDbContext context) : IPolicyService
{
    public async Task<Guid> CreatePolicyAsync(CreatePolicyRequest request, int tenantId)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var scopeTypeEnum = Enum.Parse<PolicyScope>(request.ScopeType, true);

            var existingScopes = await context.PolicyScopes
                .Where(s => s.TenantId == tenantId && s.ScopeType == scopeTypeEnum && s.ScopeValue == request.ScopeValue)
                .ToListAsync();

            if (existingScopes.Any())
            {
                var usageIds = existingScopes.Select(s => s.UsagePolicyId).Where(id => id.HasValue).Distinct().ToList();
                var weekOffIds = existingScopes.Select(s => s.WeekOffPolicyId).Where(id => id.HasValue).Distinct().ToList();
                var balanceIds = existingScopes.Select(s => s.BalancePolicyId).Where(id => id.HasValue).Distinct().ToList();

                if (usageIds.Any()) context.LeaveUsagePolicies.RemoveRange(context.LeaveUsagePolicies.Where(p => usageIds.Contains(p.Id)));
                if (weekOffIds.Any()) context.WeekOffPolicies.RemoveRange(context.WeekOffPolicies.Where(p => weekOffIds.Contains(p.Id)));
                if (balanceIds.Any()) context.BalancePolicies.RemoveRange(context.BalancePolicies.Where(p => balanceIds.Contains(p.Id)));
                
                context.PolicyScopes.RemoveRange(existingScopes);
                await context.SaveChangesAsync();
            }

            var weekOffPolicy = new WeekOffPolicy
            {
                TenantId = tenantId,
                Name = $"{request.Name} - WeekOff",
                RulesJson = JsonSerializer.Serialize(request.Calendar.WeekOffs),
                AllowCompOff = request.Calendar.AllowCompOff
            };
            context.WeekOffPolicies.Add(weekOffPolicy);

            var usagePolicy = new LeaveUsagePolicy
            {
                TenantId = tenantId,
                Name = $"{request.Name} - Usage",
                SandwichEnabled = request.Usage.SandwichEnabled,
                IncludeHolidaysInSandwich = request.Usage.IncludeHolidaysInSandwich,
                IncludeWeekendsInSandwich = request.Usage.IncludeWeekendsInSandwich,
                AdvanceNoticeDays = request.Usage.AdvanceNoticeDays,
                AllowBackdated = request.Usage.AllowBackdated,
                MaxFutureDays = request.Usage.MaxFutureDays,
                MinServiceDaysRequired = request.Usage.MinServiceDaysRequired,
                MaxConsecutiveDays = request.Usage.MaxConsecutiveDays
            };
            context.LeaveUsagePolicies.Add(usagePolicy);

            await context.SaveChangesAsync();

            foreach (var ruleReq in request.ApprovalRules)
            {
                var approvalRule = new ApprovalRule
                {
                    TenantId = tenantId,
                    Name = ruleReq.Name,
                    Priority = ruleReq.Priority,
                    Mode = Enum.Parse<WorkflowApprovalMode>(ruleReq.ApprovalMode, true),
                    ConditionJson = JsonSerializer.Serialize(ruleReq.Conditions),
                    IsActive = true
                };

                foreach (var stepReq in ruleReq.Steps)
                {
                    approvalRule.Steps.Add(new ApprovalStep
                    {
                        Sequence = stepReq.Order,
                        ApproverType = Enum.Parse<ApproverType>(stepReq.ApproverType, true),
                        ApproverValue = stepReq.RoleId ?? ""
                    });
                }
                context.ApprovalRules.Add(approvalRule);
            }

            if (request.BalancePolicies != null && request.BalancePolicies.Any())
            {
                foreach (var balanceReq in request.BalancePolicies)
                {
                    var leaveType = await context.LeaveTypes
                        .FirstOrDefaultAsync(lt => lt.Code == balanceReq.LeaveTypeId && lt.TenantId == tenantId)
                        ?? throw new AppException(404, $"Leave type {balanceReq.LeaveTypeId} not found", "NOT_FOUND");

                    var balancePolicy = new BalancePolicy
                    {
                        TenantId = tenantId,
                        LeaveTypeId = leaveType.Id,
                        Name = $"{request.Name} - {balanceReq.LeaveTypeId} Balance",
                        AllowCarryForward = balanceReq.AllowCarryForward,
                        MaxCarryForwardLimit = balanceReq.MaxCarryForward,
                        AllowNegativeBalance = balanceReq.AllowNegativeBalance,
                        MaxNegativeLimit = balanceReq.MaxNegativeLimit,
                        RoundingRule = Enum.Parse<RoundingRule>(balanceReq.RoundingRule, true)
                    };
                    context.BalancePolicies.Add(balancePolicy);
                    await context.SaveChangesAsync();

                    context.PolicyScopes.Add(new PolicyScopes
                    {
                        TenantId = tenantId,
                        Name = $"{request.Name} - {balanceReq.LeaveTypeId}",
                        ScopeType = scopeTypeEnum,
                        ScopeValue = request.ScopeValue,
                        Priority = request.Priority,
                        BalancePolicyId = balancePolicy.Id,
                        WeekOffPolicyId = weekOffPolicy.Id,
                        UsagePolicyId = usagePolicy.Id,
                        IsActive = true
                    });
                }
            }
            else
            {
                context.PolicyScopes.Add(new PolicyScopes
                {
                    TenantId = tenantId,
                    Name = request.Name,
                    ScopeType = scopeTypeEnum,
                    ScopeValue = request.ScopeValue,
                    Priority = request.Priority,
                    WeekOffPolicyId = weekOffPolicy.Id,
                    UsagePolicyId = usagePolicy.Id,
                    IsActive = true
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Guid.NewGuid();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PolicySummaryDto>> GetAllPoliciesAsync(int tenantId)
    {
        return await context.PolicyScopes
            .Where(s => s.TenantId == tenantId)
            .GroupBy(s => new { s.ScopeType, s.ScopeValue })
            .Select(g => g.First())
            .Select(s => new PolicySummaryDto(
                s.Name ?? "Unnamed Policy",
                s.ScopeType.ToString(),
                s.ScopeValue ?? "",
                s.Priority,
                s.IsActive
            ))
            .ToListAsync();
    }

    public async Task<CreatePolicyRequest?> GetPolicyByScopeAsync(LMS.Domain.Enums.Policy.PolicyScope scopeType, string scopeValue, int tenantId)
    {
        var scopes = await context.PolicyScopes
            .Where(s => s.TenantId == tenantId && s.ScopeType == scopeType && s.ScopeValue == scopeValue)
            .ToListAsync();

        if (!scopes.Any()) return null;

        var primaryScope = scopes.First();

        var weekOff = await context.WeekOffPolicies.FindAsync(primaryScope.WeekOffPolicyId);
        var weekOffReq = weekOff != null ? new WeekOffPolicyRequest(
            JsonSerializer.Deserialize<List<WeekOffRuleDto>>(weekOff.RulesJson ?? "[]") ?? new(),
            weekOff.AllowCompOff
        ) : null;

        var usage = await context.LeaveUsagePolicies.FindAsync(primaryScope.UsagePolicyId);
        var usageReq = usage != null ? new UsagePolicyRequest(
            usage.SandwichEnabled,
            usage.IncludeWeekendsInSandwich,
            usage.IncludeHolidaysInSandwich,
            usage.AdvanceNoticeDays,
            usage.AllowBackdated,
            usage.MaxFutureDays,
            usage.MinServiceDaysRequired,
            usage.MaxConsecutiveDays
        ) : null;

        var balanceIds = scopes.Select(s => s.BalancePolicyId).Where(id => id != null).ToList();
        var balances = await context.BalancePolicies
            .Include(b => b.LeaveType)
            .Where(b => balanceIds.Contains(b.Id))
            .Select(b => new BalancePolicyRequest(
                b.LeaveType != null ? b.LeaveType.Code : "UNKNOWN",
                b.AllowCarryForward,
                b.MaxCarryForwardLimit,
                b.AllowNegativeBalance,
                b.MaxNegativeLimit,
                b.RoundingRule.ToString()
            ))
            .ToListAsync();

        var approvals = await context.ApprovalRules
            .Include(r => r.Steps)
            .Where(r => r.TenantId == tenantId)
            .Select(r => new ApprovalRuleRequest(
                r.Name,
                r.Priority,
                r.Mode.ToString(),
                JsonSerializer.Deserialize<object>(r.ConditionJson ?? "{}", (JsonSerializerOptions?)null) ?? new { },
                r.Steps.Select(s => new ApprovalStepRequest(s.Sequence, s.ApproverType.ToString(), s.ApproverValue ?? "")).ToList()
            ))
            .ToListAsync();

        return new CreatePolicyRequest(
            primaryScope.Name ?? "Unnamed Policy",
            primaryScope.ScopeType.ToString(),
            primaryScope.ScopeValue ?? "",
            primaryScope.Priority,
            weekOffReq ?? new WeekOffPolicyRequest(new(), false),
            usageReq ?? new UsagePolicyRequest(false, false, false, 0, false, 0, 0, 0),
            approvals,
            balances
        );
    }
}   
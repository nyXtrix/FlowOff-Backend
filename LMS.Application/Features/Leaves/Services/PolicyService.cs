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
                var policyIds = existingScopes.SelectMany(s => new[] { s.UsagePolicyId, s.WeekOffPolicyId, s.BalancePolicyId })
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                if (policyIds.Any())
                {
                    var policiesToDelete = await context.LeavePolicies.Where(p => policyIds.Contains(p.Id)).ToListAsync();
                    context.LeavePolicies.RemoveRange(policiesToDelete);
                }
                
                var existingWorkflowRules = await context.WorkflowRules.Where(r => r.TenantId == tenantId).ToListAsync();
                if (existingWorkflowRules.Any()) context.WorkflowRules.RemoveRange(existingWorkflowRules);

                context.PolicyScopes.RemoveRange(existingScopes);
                await context.SaveChangesAsync();
            }

            var weekOffPolicy = new LeavePolicy
            {
                TenantId = tenantId,
                Name = $"{request.Name} - WeekOff",
                Type = LeavePolicyType.WeekOff,
                ConfigJson = JsonSerializer.Serialize(new { 
                    Rules = request.Calendar.WeekOffs,
                    AllowCompOff = request.Calendar.AllowCompOff
                })
            };
            context.LeavePolicies.Add(weekOffPolicy);

            var usagePolicy = new LeavePolicy
            {
                TenantId = tenantId,
                Name = $"{request.Name} - Usage",
                Type = LeavePolicyType.Usage,
                ConfigJson = JsonSerializer.Serialize(new {
                    SandwichEnabled = request.Usage.SandwichEnabled,
                    IncludeHolidaysInSandwich = request.Usage.IncludeHolidaysInSandwich,
                    IncludeWeekendsInSandwich = request.Usage.IncludeWeekendsInSandwich,
                    AdvanceNoticeDays = request.Usage.AdvanceNoticeDays,
                    AllowBackdated = request.Usage.AllowBackdated,
                    MaxFutureDays = request.Usage.MaxFutureDays,
                    MinServiceDaysRequired = request.Usage.MinServiceDaysRequired,
                    MaxConsecutiveDays = request.Usage.MaxConsecutiveDays
                })
            };
            context.LeavePolicies.Add(usagePolicy);

            await context.SaveChangesAsync();

            foreach (var ruleReq in request.ApprovalRules)
            {
                if (!Enum.TryParse<WorkflowApprovalMode>(ruleReq.ApprovalMode, true, out var mode))
                {
                    mode = WorkflowApprovalMode.Sequential;
                }

                var workflowRule = new WorkflowRule
                {
                    TenantId = tenantId,
                    Name = ruleReq.Name,
                    Priority = ruleReq.Priority,
                    Mode = mode,
                    ConditionJson = JsonSerializer.Serialize(ruleReq.Conditions),
                    IsActive = true
                };

                foreach (var stepReq in ruleReq.Steps)
                {
                    if (!Enum.TryParse<ApproverType>(stepReq.ApproverType, true, out var stepType))
                    {
                        stepType = ApproverType.MANAGER;
                    }

                    workflowRule.Steps.Add(new WorkflowStep
                    {
                        Sequence = stepReq.Order,
                        ApproverType = stepType,
                        ApproverValue = stepReq.RoleId ?? ""
                    });
                }
                context.WorkflowRules.Add(workflowRule);
            }

            if (request.BalancePolicies != null && request.BalancePolicies.Any())
            {
                foreach (var balanceReq in request.BalancePolicies)
                {
                    var leaveType = await context.LeaveTypes
                        .FirstOrDefaultAsync(lt => lt.Code == balanceReq.LeaveTypeId && lt.TenantId == tenantId)
                        ?? throw new AppException(404, $"Leave type {balanceReq.LeaveTypeId} not found", "NOT_FOUND");

                    var balancePolicy = new LeavePolicy
                    {
                        TenantId = tenantId,
                        LeaveTypeId = leaveType.Id,
                        Name = $"{request.Name} - {balanceReq.LeaveTypeId} Balance",
                        Type = LeavePolicyType.Balance,
                        ConfigJson = JsonSerializer.Serialize(new {
                            AllowCarryForward = balanceReq.AllowCarryForward,
                            MaxCarryForwardLimit = balanceReq.MaxCarryForward,
                            AllowNegativeBalance = balanceReq.AllowNegativeBalance,
                            MaxNegativeLimit = balanceReq.MaxNegativeLimit,
                            RoundingRule = balanceReq.RoundingRule
                        })
                    };
                    context.LeavePolicies.Add(balancePolicy);
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
                (int)s.ScopeType,
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

        var weekOff = await context.LeavePolicies.FindAsync(primaryScope.WeekOffPolicyId);
        var weekOffConfig = weekOff != null ? JsonSerializer.Deserialize<JsonElement>(weekOff.ConfigJson) : default;
        var weekOffReq = weekOff != null ? new WeekOffPolicyRequest(
            weekOffConfig.TryGetProperty("Rules", out var r) ? JsonSerializer.Deserialize<List<WeekOffRuleDto>>(r.GetRawText()) ?? new() : new(),
            weekOffConfig.TryGetProperty("AllowCompOff", out var a) && a.GetBoolean()
        ) : null;

        var usage = await context.LeavePolicies.FindAsync(primaryScope.UsagePolicyId);
        var usageConfig = usage != null ? JsonSerializer.Deserialize<JsonElement>(usage.ConfigJson) : default;
        var usageReq = usage != null ? new UsagePolicyRequest(
            usageConfig.TryGetProperty("SandwichEnabled", out var se) && se.GetBoolean(),
            usageConfig.TryGetProperty("IncludeWeekendsInSandwich", out var iws) && iws.GetBoolean(),
            usageConfig.TryGetProperty("IncludeHolidaysInSandwich", out var ihs) && ihs.GetBoolean(),
            usageConfig.TryGetProperty("AdvanceNoticeDays", out var and) ? and.GetInt32() : 0,
            usageConfig.TryGetProperty("AllowBackdated", out var abd) && abd.GetBoolean(),
            usageConfig.TryGetProperty("MaxFutureDays", out var mfd) ? mfd.GetInt32() : 0,
            usageConfig.TryGetProperty("MinServiceDaysRequired", out var msr) ? msr.GetInt32() : 0,
            usageConfig.TryGetProperty("MaxConsecutiveDays", out var mcd) ? mcd.GetInt32() : 0
        ) : null;

        var balanceIds = scopes.Select(s => s.BalancePolicyId).Where(id => id != null).ToList();
        var balances = await context.LeavePolicies
            .Include(b => b.LeaveType)
            .Where(b => balanceIds.Contains(b.Id))
            .ToListAsync();
            
        var balanceRequests = balances.Select(b => {
            var bConfig = JsonSerializer.Deserialize<JsonElement>(b.ConfigJson);
            return new BalancePolicyRequest(
                b.LeaveType != null ? b.LeaveType.Code : "UNKNOWN",
                bConfig.TryGetProperty("AllowCarryForward", out var acf) && acf.GetBoolean(),
                bConfig.TryGetProperty("MaxCarryForwardLimit", out var mcfl) ? mcfl.GetDecimal() : 0,
                bConfig.TryGetProperty("AllowNegativeBalance", out var anb) && anb.GetBoolean(),
                bConfig.TryGetProperty("MaxNegativeLimit", out var mnl) ? mnl.GetDecimal() : 0,
                bConfig.TryGetProperty("RoundingRule", out var rr) ? rr.GetString() ?? "None" : "None"
            );
        }).ToList();

        var approvals = await context.WorkflowRules
            .Include(r => r.Steps)
            .Where(r => r.TenantId == tenantId && r.IsActive)
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
            balanceRequests
        );
    }
}
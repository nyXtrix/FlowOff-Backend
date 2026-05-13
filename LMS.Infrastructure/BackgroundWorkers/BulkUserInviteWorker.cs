using LMS.Application.Common.Interfaces;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Enums;
using LMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LMS.Application.Features.Organization.Employees.Services;

namespace LMS.Infrastructure.BackgroundWorkers;

public class BulkUserInviteWorker(IServiceScopeFactory scopeFactory, ILogger<BulkUserInviteWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var bulkService = scope.ServiceProvider.GetRequiredService<BulkUserInviteService>();

                    try 
                    {
                        await ResetDailyQuotasAsync(context);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[BULK_QUOTA_ERROR] Failed to reset quotas");
                    }

                    var queuedOperations = await context.BulkUserInvites
                        .Where(b => b.Status == LMS.Domain.Enums.Users.BulkInvitedUserStatus.Queued)
                        .ToListAsync(stoppingToken);

                    foreach (var op in queuedOperations)
                    {
                        try 
                        {
                            await bulkService.ProcessUploadAsync(op.ExternalId);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "[BULK_PROCESS_ERROR] Failed to process upload {ExternalId}", op.ExternalId);
                        }
                    }

                    var activeTenants = await context.Tenants
                        .Where(t => t.RemainingDailyInvites > 0)
                        .ToListAsync(stoppingToken);

                    foreach (var tenant in activeTenants)
                    {
                        var pendingUsers = await context.Users
                            .Where(u => u.TenantId == tenant.Id && u.BulkUserInvitedId != null && u.Status == UserStatus.Pending)
                            .Where(u => !context.UserInvites.Any(i => i.UserId == u.Id))
                            .Take(tenant.RemainingDailyInvites)
                            .ToListAsync(stoppingToken);

                        if (pendingUsers.Any())
                        {
                            logger.LogInformation("Processing {Count} bulk invites for tenant {TenantId}", pendingUsers.Count, tenant.Id);
                        }

                        foreach (var user in pendingUsers)
                        {
                            try
                            {
                                var token = Guid.NewGuid().ToString("N");
                                var invite = new UserInvite
                                {
                                    UserId = user.Id,
                                    Token = token,
                                    ExpiryDate = DateTime.UtcNow.AddHours(24)
                                };
                                context.UserInvites.Add(invite);
                                
                                var baseUrl = configuration["App:FrontendUrl"] ?? "https://flowoff.vercel.app";
                                var inviteLink = $"{baseUrl.TrimEnd('/')}/set-password?token={token}";
                                var fullName = $"{user.FirstName} {user.LastName}";
                                
                                await emailService.SendInviteEmailAsync(user.Email, fullName, inviteLink);
                                logger.LogInformation("Bulk invitation email sent to {Email}", user.Email);

                                tenant.RemainingDailyInvites--;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "[BULK_INVITE_ERROR] Failed to send email to {Email}", user.Email);
                                user.Status = UserStatus.InActive;
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[BULK_WORKER_WAITING] Database or schema might not be ready");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private static async Task ResetDailyQuotasAsync(IAppDbContext context)
    {
        var today = DateTime.UtcNow.Date;
        await RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(
            context.Database,
            "UPDATE \"Tenants\" SET \"RemainingDailyInvites\" = \"DailyInviteLimit\", \"LastQuotaResetDate\" = {0} WHERE \"LastQuotaResetDate\" < {0}", 
            today);
    }
}

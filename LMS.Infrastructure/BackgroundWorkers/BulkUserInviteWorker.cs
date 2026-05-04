using LMS.Application.Common.Interfaces;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Enums;
using LMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace LMS.Infrastructure.BackgroundWorkers;

public class BulkUserInviteWorker(IServiceScopeFactory scopeFactory) : BackgroundService
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

                    await ResetDailyQuotasAsync(context);

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
                                
                                var inviteLink = $"{configuration["App:FrontendUrl"]}/set-password?token={token}";
                                var fullName = $"{user.FirstName} {user.LastName}";
                                await emailService.SendInviteEmailAsync(user.Email, fullName, inviteLink);

                                var bulkOperation = await context.BulkUserInvites.FindAsync(user.BulkUserInvitedId);
                                if (bulkOperation != null)
                                {
                                    bulkOperation.ProcessedRows++;
                                    bulkOperation.SuccessCount++;
                                    if (bulkOperation.ProcessedRows >= bulkOperation.TotalRows)
                                        bulkOperation.Status = LMS.Domain.Enums.Users.BulkInvitedUserStatus.Completed;
                                }

                                tenant.RemainingDailyInvites--;
                            }
                            catch (Exception ex)
                            {
                                var bulkOperation = await context.BulkUserInvites.FindAsync(user.BulkUserInvitedId);
                                if (bulkOperation != null)
                                {
                                    bulkOperation.ProcessedRows++;
                                    bulkOperation.FailureCount++;
                                }
                                Console.WriteLine($"[BULK_INVITE_ERROR] {user.Email}: {ex.Message}");
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BULK_WORKER_WAITING] Database or schema not ready: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
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

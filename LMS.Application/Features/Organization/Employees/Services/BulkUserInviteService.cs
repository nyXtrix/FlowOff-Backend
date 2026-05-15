using System.Data;
using System.Text.Json;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Domain.Entities.Users;
using LMS.Domain.Enums.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sylvan.Data.Csv;
using ExcelDataReader; 
using LMS.Application.Features.Organization.Employees.DTOs;

namespace LMS.Application.Features.Organization.Employees.Services;

public class BulkUserInviteService(IAppDbContext context, IStorageService storageService)
{
    public async Task<Guid> UploadFromFileAsync(Stream fileStream, string fileName, int tenantId)
    {
        var bulkInvite = new BulkUserInvite
        {
            TenantId = tenantId,
            ExternalId = Guid.NewGuid(),
            FileName = fileName,
            Status = BulkInvitedUserStatus.Queued,
            TotalRows = 0,
            ProcessedRows = 0,
            CreatedAt = DateTime.UtcNow
        };

        var fileIdentifier = await storageService.UploadFileAsync(fileStream, fileName, "bulk-invites");
        bulkInvite.FilePath = fileIdentifier;
        
        try 
        {
            using (var stream = await storageService.DownloadFileAsync(fileIdentifier))
            using (var reader = FileReader(stream, fileName))
            {
                int count = 0;
                while (reader.Read())
                {
                    var email = reader.GetValue(2)?.ToString();
                    if (!string.IsNullOrWhiteSpace(email)) count++;
                }
                bulkInvite.TotalRows = count;
            }
        }
        catch(Exception ex)
        {
            
        }

        context.BulkUserInvites.Add(bulkInvite);
        await context.SaveChangesAsync();

        return bulkInvite.ExternalId;
    }

    public async Task ProcessUploadAsync(Guid bulkInviteId)
    {
        var bulkInvite = await context.BulkUserInvites
            .FirstOrDefaultAsync(x => x.ExternalId == bulkInviteId);

        if (bulkInvite == null || string.IsNullOrEmpty(bulkInvite.FilePath)) return;

        try 
        {
            bulkInvite.Status = BulkInvitedUserStatus.Processing;
            await context.SaveChangesAsync();

            using (var fileStream = await storageService.DownloadFileAsync(bulkInvite.FilePath))
            using (var reader = FileReader(fileStream, bulkInvite.FileName))
            {
                var dbConnection = RelationalDatabaseFacadeExtensions.GetDbConnection(context.Database);
                var connection = (NpgsqlConnection)dbConnection;
                if (connection.State != ConnectionState.Open) await connection.OpenAsync();

                await CreateTempTableAsync(connection);
                
                int importedCount = 0;
                using (var writer = connection.BeginBinaryImport("COPY temp_user_import (first_name, last_name, email, role_name, dept_name, manager_email, gender) FROM STDIN (FORMAT BINARY)"))
                {
                    while (reader.Read())
                    {
                        var email = reader.GetValue(2)?.ToString();
                        if (string.IsNullOrWhiteSpace(email)) continue;

                        writer.StartRow();
                        writer.Write(reader.GetValue(0)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(reader.GetValue(1)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(email, NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(reader.GetValue(3)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(reader.GetValue(4)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(reader.GetValue(5)?.ToString(), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(reader.GetValue(6)?.ToString() ?? "NotSpecified", NpgsqlTypes.NpgsqlDbType.Text);
                        importedCount++;
                    }
                    await writer.CompleteAsync();
                }

                if (importedCount > 0)
                {
                    await MigrateFromTempAsync(connection, bulkInvite.TenantId, bulkInvite);
                    await ResolveManagerAsync(connection, bulkInvite.TenantId);
                }
                else 
                {
                    throw new AppException(400, "No valid data rows found in the uploaded file.", "EMPTY_FILE");
                }
            }

            await storageService.DeleteFileAsync(bulkInvite.FilePath);

            if (bulkInvite.Status == BulkInvitedUserStatus.Processing)
            {
                if (bulkInvite.FailureCount > 0 && bulkInvite.SuccessCount > 0) 
                    bulkInvite.Status = BulkInvitedUserStatus.PartiallyFailed;
                else if (bulkInvite.FailureCount > 0 && bulkInvite.SuccessCount == 0)
                    bulkInvite.Status = BulkInvitedUserStatus.Failed;
                else
                    bulkInvite.Status = BulkInvitedUserStatus.Completed;
            }
        }
        catch (Exception ex)
        {
            bulkInvite.Status = BulkInvitedUserStatus.Failed;
            bulkInvite.ErrorMessage = ex.Message;
        }
        finally 
        {
            await context.SaveChangesAsync();
        }
    }

    public async Task<BulkUserInvitedStatus> GetStatusAsync(Guid bulkUserInviteId)
    {
        var bulkUserInvite = await context.BulkUserInvites
            .FirstOrDefaultAsync(j => j.ExternalId == bulkUserInviteId)
            ?? throw new AppException(404, "Bulk upload not found", "UPLOAD_NOT_FOUND");

        return new BulkUserInvitedStatus(
            bulkUserInvite.TotalRows,
            bulkUserInvite.ProcessedRows,
            bulkUserInvite.SuccessCount,
            bulkUserInvite.FailureCount,
            (int)bulkUserInvite.Status,
            bulkUserInvite.ExternalId,
            bulkUserInvite.ErrorMessage
        );
    }

    private static async Task CreateTempTableAsync(NpgsqlConnection connection)
    {
        var sql = @"
            CREATE TEMP TABLE temp_user_import(
                first_name TEXT, last_name TEXT, email TEXT, role_name TEXT, dept_name TEXT, manager_email TEXT, gender TEXT
            ); 
            CREATE TEMP TABLE temp_import_results(
                email TEXT, status INT, error_message TEXT
            );";

        using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task MigrateFromTempAsync(NpgsqlConnection connection, int tenantId, BulkUserInvite bulkInvite)
    {
        try 
        {
            var logDuplicatesSql = @"
                INSERT INTO temp_import_results (email, status, error_message)
                SELECT email, 1, 'Duplicate email in file'
                FROM temp_user_import
                WHERE ctid NOT IN (SELECT MIN(ctid) FROM temp_user_import GROUP BY email);";
            using (var cmd = new NpgsqlCommand(logDuplicatesSql, connection)) await cmd.ExecuteNonQueryAsync();

            var dedupeSql = @"DELETE FROM temp_user_import WHERE ctid NOT IN (SELECT MIN(ctid) FROM temp_user_import GROUP BY email);";
            using (var cmd = new NpgsqlCommand(dedupeSql, connection)) await cmd.ExecuteNonQueryAsync();

            var skipExistingSql = $@"
                INSERT INTO temp_import_results (email, status, error_message)
                SELECT t.email, 2, 'User already exists'
                FROM temp_user_import t
                JOIN ""Users"" u ON t.email = u.""Email"" AND u.""TenantId"" = {tenantId};";
            using (var cmd = new NpgsqlCommand(skipExistingSql, connection)) await cmd.ExecuteNonQueryAsync();

            var invalidRolesSql = $@"
                INSERT INTO temp_import_results (email, status, error_message)
                SELECT t.email, 1, 'Invalid Role Name: ' || COALESCE(t.role_name, 'NULL')
                FROM temp_user_import t
                LEFT JOIN ""Roles"" r ON LOWER(r.""Name"") = LOWER(t.role_name) AND r.""TenantId"" = {tenantId}
                WHERE r.""Id"" IS NULL AND t.email NOT IN (SELECT email FROM temp_import_results);";
            using (var cmd = new NpgsqlCommand(invalidRolesSql, connection)) await cmd.ExecuteNonQueryAsync();

            var invalidDeptsSql = $@"
                INSERT INTO temp_import_results (email, status, error_message)
                SELECT t.email, 1, 'Invalid Department Name: ' || COALESCE(t.dept_name, 'NULL')
                FROM temp_user_import t
                LEFT JOIN ""Departments"" d ON LOWER(d.""Name"") = LOWER(t.dept_name) AND d.""TenantId"" = {tenantId}
                WHERE d.""Id"" IS NULL AND t.email NOT IN (SELECT email FROM temp_import_results);";
            using (var cmd = new NpgsqlCommand(invalidDeptsSql, connection)) await cmd.ExecuteNonQueryAsync();

            var circularRefSql = $@"
                INSERT INTO temp_import_results (email, status, error_message)
                WITH RECURSIVE AllRelationships AS (
                    SELECT email, manager_email FROM temp_user_import WHERE manager_email IS NOT NULL AND manager_email <> ''
                    UNION ALL
                    SELECT u.""Email"", COALESCE(m.""Email"", u.""UnresolvedManagerEmail"")
                    FROM ""Users"" u LEFT JOIN ""Users"" m ON u.""ManagerId"" = m.""Id""
                    WHERE u.""TenantId"" = {tenantId} AND (u.""ManagerId"" IS NOT NULL OR u.""UnresolvedManagerEmail"" IS NOT NULL)
                ),
                Hierarchy AS (
                    SELECT email, manager_email, ARRAY[email] as path, false as is_cycle FROM AllRelationships
                    UNION ALL
                    SELECT h.email, r.manager_email, h.path || r.manager_email, r.manager_email = ANY(h.path)
                    FROM Hierarchy h JOIN AllRelationships r ON h.manager_email = r.email WHERE NOT h.is_cycle
                )
                SELECT DISTINCT email, 1, 'Circular reporting detected'
                FROM Hierarchy WHERE is_cycle AND email NOT IN (SELECT email FROM temp_import_results);";
            using (var cmd = new NpgsqlCommand(circularRefSql, connection)) await cmd.ExecuteNonQueryAsync();

            var migrateSql = $@"
                INSERT INTO ""Users"" (""TenantId"", ""FirstName"", ""LastName"", ""Email"", ""Status"", ""RoleId"", ""DepartmentId"", ""UnresolvedManagerEmail"", ""BulkUserInvitedId"", ""ExternalId"", ""CreatedAt"", ""UpdatedAt"", ""Gender"", ""PermissionOverridesJson"")
                SELECT 
                    {tenantId}, t.first_name, t.last_name, t.email, 1, r.""Id"", d.""Id"", t.manager_email, {bulkInvite.Id}, gen_random_uuid(), NOW(), NOW(),
                    CASE WHEN LOWER(t.gender) = 'male' THEN 1 WHEN LOWER(t.gender) = 'female' THEN 2 WHEN LOWER(t.gender) IN ('other','others') THEN 3 ELSE 0 END,
                    '[]'
                FROM temp_user_import t 
                JOIN ""Roles"" r ON LOWER(r.""Name"") = LOWER(t.role_name) AND r.""TenantId"" = {tenantId}
                LEFT JOIN ""Departments"" d ON LOWER(d.""Name"") = LOWER(t.dept_name) AND d.""TenantId"" = {tenantId}
                WHERE t.email NOT IN (SELECT email FROM temp_import_results);";
            using (var cmd = new NpgsqlCommand(migrateSql, connection)) await cmd.ExecuteNonQueryAsync();

            var results = new List<BulkRowResultDto>();
            using (var cmd = new NpgsqlCommand("SELECT email, status, error_message FROM temp_import_results", connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    results.Add(new BulkRowResultDto
                    {
                        Email = reader.GetString(0),
                        Status = reader.GetInt32(1),
                        ErrorMessage = reader.IsDBNull(2) ? null : reader.GetString(2)
                    });
                }
            }

            bulkInvite.RowResultsJson = JsonSerializer.Serialize(results);
            bulkInvite.SuccessCount = await context.Users.CountAsync(u => u.BulkUserInvitedId == bulkInvite.Id);
            bulkInvite.FailureCount = results.Count(r => r.Status == 1);
            bulkInvite.ProcessedRows = bulkInvite.SuccessCount + results.Count;
        }
        catch (Exception ex)
        {
            throw new AppException(500, $"Migration failed: {ex.Message}", "MIGRATION_ERROR");
        }
        finally 
        {
            using var dropCmd = new NpgsqlCommand("DROP TABLE IF EXISTS temp_user_import; DROP TABLE IF EXISTS temp_import_results;", connection);
            await dropCmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task ResolveManagerAsync(NpgsqlConnection connection, int tenantId)
    {
        var sql = $@"
            UPDATE ""Users"" u 
            SET ""ManagerId"" = m.""Id"", ""UnresolvedManagerEmail"" = NULL 
            FROM ""Users"" m 
            WHERE u.""TenantId"" = {tenantId}
              AND u.""UnresolvedManagerEmail"" = m.""Email"" 
              AND u.""TenantId"" = m.""TenantId"" 
              AND u.""Id"" != m.""Id"";";

        using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static IDataReader FileReader(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();

        if (ext == ".csv") 
        {
            return CsvDataReader.Create(new StreamReader(stream));
        }

        if (ext == ".xlsx" || ext == ".xls") 
        {
            return ExcelReaderFactory.CreateReader(stream);
        }

        throw new AppException(400, "Unsupported file format", "UNSUPPORTED_FILE");
    }

    public async Task<Guid?> GetActiveBulkInviteAsync(int tenantId)
    {
        var bulkUserInvite = await context.BulkUserInvites
            .Where(x => x.TenantId == tenantId && 
                (x.Status == BulkInvitedUserStatus.Queued || x.Status == BulkInvitedUserStatus.Processing))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (bulkUserInvite == null) return null;

        if (bulkUserInvite.Status == BulkInvitedUserStatus.Processing && 
            bulkUserInvite.UpdatedAt < DateTime.UtcNow.AddMinutes(-10))
        {
            bulkUserInvite.Status = BulkInvitedUserStatus.Failed;
            bulkUserInvite.ErrorMessage = "Upload timed out or was interrupted.";
            await context.SaveChangesAsync();
            return null;
        }

        return bulkUserInvite.ExternalId;
    }

    public async Task<List<BulkUserInviteDto>> GetHistoryAsync(int tenantId)
    {
        return await context.BulkUserInvites
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BulkUserInviteDto
            {
                ExternalId = x.ExternalId,
                FileName = x.FileName,
                TotalRows = x.TotalRows,
                SuccessCount = x.SuccessCount,
                FailureCount = x.FailureCount,
                Status = (int)x.Status,
                ErrorMessage = x.ErrorMessage,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<BulkUserInviteDetailsDto> GetBulkInviteDetailsAsync(Guid externalId)
    {
        var invite = await context.BulkUserInvites
            .FirstOrDefaultAsync(x => x.ExternalId == externalId)
            ?? throw new AppException(404, "Bulk invite not found", "INVITE_NOT_FOUND");

        var results = string.IsNullOrEmpty(invite.RowResultsJson) 
            ? new List<BulkRowResultDto>() 
            : JsonSerializer.Deserialize<List<BulkRowResultDto>>(invite.RowResultsJson) ?? new List<BulkRowResultDto>();

        return new BulkUserInviteDetailsDto
        {
            ExternalId = invite.ExternalId,
            FileName = invite.FileName,
            Status = (int)invite.Status,
            TotalRows = invite.TotalRows,
            SuccessCount = invite.SuccessCount,
            FailureCount = invite.FailureCount,
            Results = results
        };
    }
}

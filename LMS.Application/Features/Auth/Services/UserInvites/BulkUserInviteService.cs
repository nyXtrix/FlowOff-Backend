using System.Data;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Domain.Entities.Users;
using LMS.Domain.Enums.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sylvan.Data.Csv;
using ExcelDataReader; 
using LMS.Application.Features.Auth.DTOs;

namespace LMS.Application.Features.Auth.Services.UserInvites;

public class BulkUserInviteService(IAppDbContext context)
{
    public async Task<Guid> UploadFromFileAsync(Stream fileStream, string fileName, int tenantId)
    {
        var bulkInvite = new BulkUserInvite
        {
            TenantId = tenantId,
            FileName = fileName,
            TotalRows = 0,
            Status = BulkInvitedUserStatus.Queued
        };

        context.BulkUserInvites.Add(bulkInvite);
        await context.SaveChangesAsync();

        using var reader = FileReader(fileStream, fileName);
        
        var dbConnection = RelationalDatabaseFacadeExtensions.GetDbConnection(context.Database);
        var connection = (NpgsqlConnection)dbConnection;
        
        if (connection.State != ConnectionState.Open) 
            await connection.OpenAsync();

        await CreateTempTableAsync(connection);
        
        using (var writer = connection.BeginBinaryImport("COPY temp_user_import (first_name, last_name, email, role_name, dept_name, manager_email, gender) FROM STDIN (FORMAT BINARY)"))
        {
            int rowCount = 0;
            while (reader.Read())
            {
                writer.StartRow();
                writer.Write(reader.GetValue(0)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                writer.Write(reader.GetValue(1)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                writer.Write(reader.GetValue(2)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                writer.Write(reader.GetValue(3)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                writer.Write(reader.GetValue(4)?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                writer.Write(reader.GetValue(5)?.ToString(), NpgsqlTypes.NpgsqlDbType.Text);
                writer.Write(reader.GetValue(6)?.ToString() ?? "NotSpecified", NpgsqlTypes.NpgsqlDbType.Text);
                rowCount++;
            }
            await writer.CompleteAsync();
            bulkInvite.TotalRows = rowCount;
        }

        await MigrateFromTempAsync(connection, tenantId, bulkInvite.Id);

        await ResolveManagerAsync(connection, tenantId);

        await context.SaveChangesAsync();
        return bulkInvite.ExternalId;
    }

    public async Task<BulkUserInvitedStatus> GetStatusAsync(Guid bulkUserInviteId)
    {
        var bulkUserInvite = await context.BulkUserInvites
            .FirstOrDefaultAsync(j => j.ExternalId == bulkUserInviteId)
            ?? throw new AppException(404, "Bulk upload job not found", "JOB_NOT_FOUND");

        return new BulkUserInvitedStatus(
            bulkUserInvite.TotalRows,
            bulkUserInvite.ProcessedRows,
            bulkUserInvite.SuccessCount,
            bulkUserInvite.FailureCount,
            bulkUserInvite.Status
        );
    }

    private static async Task CreateTempTableAsync(NpgsqlConnection connection)
    {
        var sql = @"CREATE TEMP TABLE temp_user_import(
            first_name TEXT, last_name TEXT, email TEXT, role_name TEXT, dept_name TEXT, manager_email TEXT, gender TEXT
        ) ON COMMIT DROP;";

        using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task MigrateFromTempAsync(NpgsqlConnection connection, int tenantId, int bulkInviteId)
    {
        var sql = $@"
            INSERT INTO ""Users"" (""TenantId"", ""FirstName"", ""LastName"", ""Email"", ""Status"", ""RoleId"", ""DepartmentId"", ""UnresolvedManagerEmail"", ""BulkUserInvitedId"", ""ExternalId"", ""CreatedAt"", ""UpdatedAt"", ""Gender"")
            SELECT 
                {tenantId}, t.first_name, t.last_name, t.email, 0, r.""Id"", d.""Id"", t.manager_email, {bulkInviteId}, gen_random_uuid(), NOW(), NOW(),
                CASE 
                    WHEN LOWER(t.gender) = 'male' THEN 1 
                    WHEN LOWER(t.gender) = 'female' THEN 2 
                    WHEN LOWER(t.gender) = 'others' OR LOWER(t.gender) = 'other' THEN 3 
                    ELSE 0 
                END
            FROM temp_user_import t 
            LEFT JOIN ""Roles"" r ON LOWER(r.""Name"") = LOWER(t.role_name) AND r.""TenantId"" = {tenantId}
            LEFT JOIN ""Departments"" d ON LOWER(d.""Name"") = LOWER(t.dept_name) AND d.""TenantId"" = {tenantId}
            WHERE t.email NOT IN (SELECT ""Email"" FROM ""Users"" WHERE ""TenantId"" = {tenantId});";

        using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
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
}
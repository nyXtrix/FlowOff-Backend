using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using LMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace LMS.Infrastructure.Services.Storage;

public class GoogleDriveStorageService : IStorageService
{
    private readonly DriveService _service;
    private readonly string _folderId;

    public GoogleDriveStorageService(IConfiguration configuration)
    {
        var clientId = configuration["Storage:GoogleClientId"];
        var clientSecret = configuration["Storage:GoogleClientSecret"];
        var refreshToken = configuration["Storage:GoogleRefreshToken"];
        var driveLink = configuration["Storage:GoogleDriveLink"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidOperationException("Google OAuth2 credentials (ClientId, ClientSecret, RefreshToken) are missing from configuration.");
        }

        if (string.IsNullOrEmpty(driveLink))
        {
            throw new InvalidOperationException("Google Drive Link is missing from configuration (Storage:GoogleDriveLink).");
        }

        _folderId = ExtractFolderId(driveLink);

        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            }
        };

        var flow = new GoogleAuthorizationCodeFlow(initializer);
        var tokenResponse = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
        {
            RefreshToken = refreshToken
        };

        var credential = new UserCredential(flow, "user", tokenResponse);

        _service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Leave Management System"
        });
    }

    private string ExtractFolderId(string link)
    {
        var match = Regex.Match(link, @"folders/([a-zA-Z0-9-_]+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return link;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "")
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = fileName,
            Parents = new List<string> { _folderId }
        };

        FilesResource.CreateMediaUpload request;
        request = _service.Files.Create(fileMetadata, fileStream, GetMimeType(fileName));
        request.Fields = "id";
        
        var progress = await request.UploadAsync();
        
        if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
        {
            throw new Exception($"Google Drive upload failed: {progress.Exception?.Message}", progress.Exception);
        }

        return request.ResponseBody.Id;
    }

    public async Task<Stream> DownloadFileAsync(string fileId)
    {
        var stream = new MemoryStream();
        await _service.Files.Get(fileId).DownloadAsync(stream);
        stream.Position = 0;
        return stream;
    }

    public async Task DeleteFileAsync(string fileId)
    {
        await _service.Files.Delete(fileId).ExecuteAsync();
    }

    public async Task<bool> FileExistsAsync(string fileId)
    {
        try
        {
            await _service.Files.Get(fileId).ExecuteAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}

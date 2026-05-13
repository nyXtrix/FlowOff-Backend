using LMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace LMS.Infrastructure.Services.Storage;

public class LocalStorageService(IWebHostEnvironment environment) : IStorageService
{
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "")
    {
        var uploadsRoot = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        var targetDir = Path.Combine(uploadsRoot, folder);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(targetDir, uniqueFileName);

        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
        }

        return Path.Combine("uploads", folder, uniqueFileName);
    }

    public Task<Stream> DownloadFileAsync(string relativePath)
    {
        var fullPath = Path.Combine(environment.ContentRootPath, "wwwroot", relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found on local storage", fullPath);
        }

        return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task DeleteFileAsync(string relativePath)
    {
        var fullPath = Path.Combine(environment.ContentRootPath, "wwwroot", relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string relativePath)
    {
        var fullPath = Path.Combine(environment.ContentRootPath, "wwwroot", relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }
}

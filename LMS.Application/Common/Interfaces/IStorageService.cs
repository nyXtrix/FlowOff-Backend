namespace LMS.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "");
    Task<Stream> DownloadFileAsync(string filePath);

    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}

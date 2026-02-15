namespace Cuintable.Server.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder);
    Task<bool> DeleteAsync(string fileUrl);
}

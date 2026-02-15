namespace Cuintable.Server.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        _basePath = Path.Combine(env.ContentRootPath, "uploads");
        _baseUrl = config["FileStorage:BaseUrl"] ?? "/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder)
    {
        var folderPath = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(folderPath);

        var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(folderPath, uniqueName);

        using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output);

        return $"{_baseUrl}/{folder}/{uniqueName}";
    }

    public Task<bool> DeleteAsync(string fileUrl)
    {
        var relativePath = fileUrl.Replace(_baseUrl, "").TrimStart('/');
        var fullPath = Path.Combine(_basePath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}

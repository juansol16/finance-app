using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace Cuintable.Server.Services;

public class GcsFileStorageService : IFileStorageService
{
    private const string BucketName = "cuintable-bucket";
    private readonly StorageClient _storageClient;

    public GcsFileStorageService()
    {
        var credentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_BASE64")
            ?? throw new InvalidOperationException("GOOGLE_CREDENTIALS_BASE64 environment variable is not set.");

        var credentialsJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(credentialsBase64));
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(credentialsJson));
        var credential = ServiceAccountCredential.FromServiceAccountData(stream).ToGoogleCredential();
        _storageClient = StorageClient.Create(credential);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder)
    {
        var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var objectName = $"{folder}/{uniqueName}";

        await _storageClient.UploadObjectAsync(BucketName, objectName, contentType, fileStream);

        return $"https://storage.googleapis.com/{BucketName}/{objectName}";
    }

    public async Task<bool> DeleteAsync(string fileUrl)
    {
        var prefix = $"https://storage.googleapis.com/{BucketName}/";
        if (!fileUrl.StartsWith(prefix))
            return false;

        var objectName = fileUrl[prefix.Length..];

        try
        {
            await _storageClient.DeleteObjectAsync(BucketName, objectName);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}

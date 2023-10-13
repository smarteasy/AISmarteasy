namespace AISmarteasy.Core.Connector;

public interface ICloudDriveConnector
{
    Task<string> CreateShareLinkAsync(string filePath, string type = "view", string scope = "anonymous", CancellationToken cancellationToken = default);

    Task<Stream> GetFileContentStreamAsync(string filePath, CancellationToken cancellationToken = default);

    Task UploadSmallFileAsync(string filePath, string destinationPath, CancellationToken cancellationToken = default);
}

namespace AISmarteasy.Core.Connector;

public interface IFileSystemConnector
{
    public Task<Stream> GetFileContentStreamAsync(string filePath, CancellationToken cancellationToken = default);

    public Task<Stream> GetWriteableFileStreamAsync(string filePath, CancellationToken cancellationToken = default);

    public Task<Stream> CreateFileAsync(string filePath, CancellationToken cancellationToken = default);

     public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
}

using System.ComponentModel;
using AISmarteasy.Core.Connecting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class DocumentSkill
{
    public static class Parameters
    {
        public const string FILE_PATH = "filePath";
    }

    private readonly IDocumentConnector _documentConnector;
    private readonly IFileSystemConnector _fileSystemConnector;
    private readonly ILogger _logger;

    public DocumentSkill()
    {

    }

    public DocumentSkill(IDocumentConnector documentConnector, IFileSystemConnector fileSystemConnector, ILoggerFactory? loggerFactory = null)
    {
        _documentConnector = documentConnector ?? throw new ArgumentNullException(nameof(documentConnector));
        _fileSystemConnector = fileSystemConnector ?? throw new ArgumentNullException(nameof(fileSystemConnector));
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(DocumentSkill)) : NullLogger.Instance;
    }

    [SKFunction, Description("Read all text from a document")]
    public async Task<string> ReadTextAsync(
        [Description("Path to the file to read")] string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading text from {0}", filePath);
        var stream = await _fileSystemConnector.GetFileContentStreamAsync(filePath, cancellationToken).ConfigureAwait(false);
        return _documentConnector.ReadText(stream);
    }

    [SKFunction, Description("Append text to a document. If the document doesn't exist, it will be created.")]
    public async Task AppendTextAsync(
        [Description("Text to append")] string text,
        [Description("Destination file path")] string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Variable was null or whitespace", nameof(filePath));
        }

        if (await _fileSystemConnector.FileExistsAsync(filePath, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Writing text to file {0}", filePath);
            Stream stream = await _fileSystemConnector.GetWriteableFileStreamAsync(filePath, cancellationToken).ConfigureAwait(false);
            _documentConnector.AppendText(stream, text);
        }
        else
        {
            _logger.LogDebug("File does not exist. Creating file at {0}", filePath);
            Stream stream = await _fileSystemConnector.CreateFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            _documentConnector.Initialize(stream);

            _logger.LogDebug("Writing text to {0}", filePath);
            _documentConnector.AppendText(stream, text);
        }
    }
}

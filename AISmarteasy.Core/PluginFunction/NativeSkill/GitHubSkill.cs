using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public class GitHubSkill
{
    private const int MAX_TOKENS = 1024;
    private const int MAX_FILE_SIZE = 2048;

    private readonly Function _summarizeCodeFunction;
    private readonly ILogger _logger;
    private static readonly char[] TrimChars = { ' ', '/' };

    internal const string SUMMARIZE_CODE_SNIPPET_DEFINITION =
        @"BEGIN CONTENT TO SUMMARIZE:
{{$INPUT}}
END CONTENT TO SUMMARIZE.

Summarize the content in 'CONTENT TO SUMMARIZE', identifying main points.
Do not incorporate other general knowledge.
Summary is in plain text, in complete sentences, with no markup or tags.

BEGIN SUMMARY:
";

    public GitHubSkill()
    {
    }
    public GitHubSkill(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger<GitHubSkill>() : NullLogger.Instance;

        //_summarizeCodeFunction = KernelProvider.Kernel.FindFunction(SummarizeCodeSnippetDefinition, nameof(GitHubSkill),
        //    description: "Given a snippet of code, summarize the part of the file.",
        //    maxTokens: MAX_TOKENS,
        //    temperature: 0.1,
        //    topP: 0.5);
    }

    [SKFunction, SKName("SummarizeRepository"), Description("Downloads a repository and summarizes the content")]
    public async Task<string?> SummarizeRepositoryAsync(
        [Description("URL of the GitHub repository to summarize")] string input,
        [Description("Name of the repository repositoryBranch which will be downloaded and summarized"), DefaultValue("main")] string repositoryBranch,
        [Description("The search string to match against the names of files in the repository"), DefaultValue("*.md")] string searchPattern,
        [Description("Personal access token for private repositories"), Optional] string? patToken,
        CancellationToken cancellationToken = default)
    {
        string tempPath = Path.GetTempPath();
        string directoryPath = Path.Combine(tempPath, $"SK-{Guid.NewGuid()}");
        string filePath = Path.Combine(tempPath, $"SK-{Guid.NewGuid()}.zip");

        try
        {
            var originalUri = input.Trim(TrimChars);
            var repositoryUri = Regex.Replace(originalUri, "github.com", "api.github.com/repos", RegexOptions.IgnoreCase);
            var repoBundle = $"{repositoryUri}/zipball/{repositoryBranch}";

            _logger.LogDebug("Downloading {RepoBundle}", repoBundle);

            var headers = new Dictionary<string, string>
            {
                { "X-GitHub-Api-Version", "2022-11-28" },
                { "Accept", "application/vnd.github+json" },
                { "User-Agent", "msft-semantic-kernel-sample" }
            };
            if (!string.IsNullOrEmpty(patToken))
            {
                _logger.LogDebug("Access token detected, adding authorization headers");
                headers.Add("Authorization", $"Bearer {patToken}");
            }

            await DownloadToFileAsync(repoBundle, headers, filePath, cancellationToken);

            ZipFile.ExtractToDirectory(filePath, directoryPath);

            await SummarizeCodeDirectoryAsync(directoryPath, searchPattern, originalUri, repositoryBranch, cancellationToken);

            return $"{originalUri}-{repositoryBranch}";
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }

    private async Task DownloadToFileAsync(string uri, IDictionary<string, string> headers, string filePath, CancellationToken cancellationToken = default)
    {
        using HttpClient client = new();

        using HttpRequestMessage request = new(HttpMethod.Get, uri);
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        FileStream fileStream = File.Create(filePath);
        await contentStream.CopyToAsync(fileStream, 81920, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Summarize a code file into an embedding
    /// </summary>
    private async Task SummarizeCodeFileAsync(string filePath, string repositoryUri, string repositoryBranch, string fileUri, CancellationToken cancellationToken = default)
    {
        string code = await File.ReadAllTextAsync(filePath, cancellationToken);

        if (code.Length > 0)
        {
            if (code.Length > MAX_FILE_SIZE)
            {
                var extension = new FileInfo(filePath).Extension;

                List<string> lines;
                List<string> paragraphs;

                switch (extension)
                {
                    case ".md":
                    {
                        lines = TextChunker.SplitMarkDownLines(code, MAX_TOKENS);
                        paragraphs = TextChunker.SplitMarkdownParagraphs(lines, MAX_TOKENS);

                        break;
                    }
                    default:
                    {
                        lines = TextChunker.SplitPlainTextLines(code, MAX_TOKENS);
                        paragraphs = TextChunker.SplitPlainTextParagraphs(lines, MAX_TOKENS);

                        break;
                    }
                }

                for (int i = 0; i < paragraphs.Count; i++)
                {
                    //await KernelProvider.Kernel.Memory.SaveInformationAsync(
                    //    $"{repositoryUri}-{repositoryBranch}",
                    //    text: $"{paragraphs[i]} File:{repositoryUri}/blob/{repositoryBranch}/{fileUri}",
                    //    id: $"{fileUri}_{i}",
                    //    cancellationToken: cancellationToken);
                }
            }
            //await KernelProvider.Kernel..Memory.SaveInformationAsync(
            //    $"{repositoryUri}-{repositoryBranch}",
            //    text: $"{code} File:{repositoryUri}/blob/{repositoryBranch}/{fileUri}",
            //    id: fileUri,
            //    cancellationToken: cancellationToken);
        }
    }

    private async Task SummarizeCodeDirectoryAsync(string directoryPath, string searchPattern, string repositoryUri, string repositoryBranch, CancellationToken cancellationToken = default)
    {
        string[] filePaths = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);

        if (filePaths != null && filePaths.Length > 0)
        {
            _logger.LogDebug("Found {0} files to summarize", filePaths.Length);

            foreach (string filePath in filePaths)
            {
                var fileUri = BuildFileUri(directoryPath, filePath, repositoryUri, repositoryBranch);
                await SummarizeCodeFileAsync(filePath, repositoryUri, repositoryBranch, fileUri, cancellationToken);
            }
        }
    }

    private string BuildFileUri(string directoryPath, string filePath, string repositoryUri, string repositoryBranch)
    {
        var repositoryBranchName = $"{repositoryUri.Trim('/').Substring(repositoryUri.LastIndexOf('/'))}-{repositoryBranch}";
        return filePath.Substring(directoryPath.Length + repositoryBranchName.Length + 1).Replace('\\', '/');
    }
}

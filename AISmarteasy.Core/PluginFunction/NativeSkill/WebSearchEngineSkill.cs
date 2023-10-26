using System.ComponentModel;
using System.Text.Json;
using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public class WebSearchEngineSkill
{
    public const string CountParam = "count";
    public const string OffsetParam = "offset";

    protected IWebSearchEngineConnector? Connector { get; set; }

    [SKFunction, Description("Perform a web search.")]
    public async Task<string> Search(
        [Description("Search query")] string query,
        [Description("Number of results")] int count = 10,
        [Description("Number of results to skip")] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (Connector == null)
            return string.Empty;

        var results = await Connector.SearchAsync(query, count, offset, cancellationToken).ConfigureAwait(false);
        var enumerable = results.ToList();
        if (!enumerable.Any())
        {
            throw new InvalidOperationException("Failed to get a response from the web search engine.");
        }

        return count == 1
            ? enumerable.FirstOrDefault() ?? string.Empty
            : JsonSerializer.Serialize(enumerable);
    }
}

using System.ComponentModel;
using System.Text.Json;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Memory;
using Microsoft.Extensions.Logging;

namespace Plugins.Native.Skills;


public sealed class TextMemorySkill
{
    public const string CollectionParam = "collection";

    public const string RelevanceParam = "relevance";

    public const string KeyParam = "key";

    public const string LimitParam = "limit";

    private const string DEFAULT_COLLECTION = "generic";
    private const double DEFAULT_RELEVANCE = 0.0;
    private const int DEFAULT_LIMIT = 1;

    private readonly ISemanticMemory? _memory;

    public TextMemorySkill()
    {
        _memory = null;
    }

    //public TextMemorySkill(ISemanticTextMemory memory)
    //{
    //    _memory = memory;
    //}

    [SKFunction, Description("Key-based lookup for a specific memory")]
    public async Task<string> RetrieveAsync(
        [SKName(CollectionParam), Description("Memories collection associated with the memory to retrieve"), DefaultValue(DEFAULT_COLLECTION)] string? collection,
        [SKName(KeyParam), Description("The key associated with the memory to retrieve")] string key,
        ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(collection);
        Verify.NotNullOrWhitespace(key);

        loggerFactory?.CreateLogger(typeof(TextMemorySkill)).LogDebug("Recalling memory with key '{0}' from collection '{1}'", key, collection);
        var memory = await this._memory.GetAsync(collection, key, cancellationToken: cancellationToken).ConfigureAwait(false);
        return memory?.Metadata.Text ?? string.Empty;
    }

     [SKFunction, Description("Semantic search and return up to N memories related to the input text")]
    public async Task<string> RecallAsync(
        [Description("The input text to find related memories for")] string input,
        [SKName(CollectionParam), Description("Memories collection to search"), DefaultValue(DEFAULT_COLLECTION)] string collection,
        [SKName(RelevanceParam), Description("The relevance score, from 0.0 to 1.0, where 1.0 means perfect match"), DefaultValue(DEFAULT_RELEVANCE)] double? relevance,
        [SKName(LimitParam), Description("The maximum number of relevant memories to recall"), DefaultValue(DEFAULT_LIMIT)] int? limit,
        ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(collection);
        relevance ??= DEFAULT_RELEVANCE;
        limit ??= DEFAULT_LIMIT;

        var logger = loggerFactory?.CreateLogger(typeof(TextMemorySkill));
        logger?.LogDebug("Searching memories in collection '{0}', relevance '{1}'", collection, relevance);

        var memories = _memory.SearchAsync(collection, input, limit.Value, relevance.Value,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var memoryList = new List<MemoryQueryResult>();
        await foreach (var memory in memories)
        {
            memoryList.Add(memory);
        }

        if (memoryList.Count == 0)
        {
            logger?.LogWarning("Memories not found in collection: {0}", collection);
            return string.Empty;
        }

        logger?.LogTrace("Done looking for memories in collection '{0}')", collection);
        return limit == 1 ? memoryList[0].Metadata.Text : JsonSerializer.Serialize(memoryList.Select(x => x.Metadata.Text));
    }

    [SKFunction, Description("Save information to semantic memory")]
    public async Task SaveAsync(
        [Description("The information to save")] string input,
        [SKName(CollectionParam), Description("Memories collection associated with the information to save"), DefaultValue(DEFAULT_COLLECTION)] string collection,
        [SKName(KeyParam), Description("The key associated with the information to save")] string key,
        ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(collection);
        Verify.NotNullOrWhitespace(key);

        loggerFactory?.CreateLogger(typeof(TextMemorySkill)).LogDebug("Saving memory to collection '{0}'", collection);

        await _memory.SaveAsync(collection, text: input, id: key, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    [SKFunction, Description("Remove specific memory")]
    public async Task RemoveAsync(
        [SKName(CollectionParam), Description("Memories collection associated with the information to save"), DefaultValue(DEFAULT_COLLECTION)] string collection,
        [SKName(KeyParam), Description("The key associated with the information to save")] string key,
        ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(collection);
        Verify.NotNullOrWhitespace(key);

        loggerFactory?.CreateLogger(typeof(TextMemorySkill)).LogDebug("Removing memory from collection '{0}'", collection);

        await _memory.RemoveAsync(collection, key, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

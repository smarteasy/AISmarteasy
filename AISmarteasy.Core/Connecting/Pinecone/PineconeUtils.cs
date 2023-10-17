using System.Collections;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connecting.Pinecone;

public static class PineconeUtils
{
    public const int MAX_METADATA_SIZE = 40 * 1024;

    public const int DEFAULT_DIMENSION = 1536;

    public const string DEFAULT_INDEX_NAME = "sk-index";

    public const IndexMetric DEFAULT_INDEX_METRIC = IndexMetric.Cosine;

    public const PodType DEFAULT_POD_TYPE = PodType.P1X1;

    internal static JsonSerializerOptions DefaultSerializerOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new PodTypeJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static async IAsyncEnumerable<PineconeDocument> EnsureValidMetadataAsync(
        IAsyncEnumerable<PineconeDocument> documents)
    {
        await foreach (PineconeDocument document in documents.ConfigureAwait(false))
        {
            if (document.Metadata == null || GetMetadataSize(document.Metadata) <= MAX_METADATA_SIZE)
            {
                yield return document;

                continue;
            }

            if (!document.Metadata.TryGetValue("text", out object? value))
            {
                yield return document;

                continue;
            }

            string text = value as string ?? string.Empty;
            int textSize = Encoding.UTF8.GetByteCount(text);
            document.Metadata.Remove("text");
            int remainingMetadataSize = GetMetadataSize(document.Metadata);

            int splitCounter = 0;
            int textIndex = 0;

            while (textSize > 0)
            {
                int availableSpace = MAX_METADATA_SIZE - remainingMetadataSize;
                int textSplitSize = Math.Min(textSize, availableSpace);

                while (textSplitSize > 0 && Encoding.UTF8.GetByteCount(text.ToCharArray(textIndex, textSplitSize)) > availableSpace)
                {
                    textSplitSize--;
                }

                string splitText = text.Substring(textIndex, textSplitSize);
                textIndex += textSplitSize;
                textSize -= Encoding.UTF8.GetByteCount(splitText);

                PineconeDocument splitDocument = PineconeDocument.Create($"{document.Id}_{splitCounter}", document.Values)
                    .WithMetadata(new Dictionary<string, object>(document.Metadata))
                    .WithSparseValues(document.SparseValues);
                splitDocument.Metadata!["text"] = splitText;

                yield return splitDocument;

                splitCounter++;
            }
        }
    }

    internal static async IAsyncEnumerable<UpsertRequest> GetUpsertBatchesAsync(
        IAsyncEnumerable<PineconeDocument> data,
        int batchSize)
    {
        List<PineconeDocument> currentBatch = new(batchSize);

        await foreach (PineconeDocument record in data.ConfigureAwait(false))
        {
            currentBatch.Add(record);

            if (currentBatch.Count != batchSize)
            {
                continue;
            }

            yield return UpsertRequest.UpsertVectors(currentBatch);

            currentBatch = new List<PineconeDocument>(batchSize);
        }

        if (currentBatch.Count <= 0)
        {
            yield break;
        }

        yield return UpsertRequest.UpsertVectors(currentBatch);
    }

    private static int GetMetadataSize(Dictionary<string, object> metadata)
    {
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);

        JsonSerializer.Serialize(writer, metadata);

        return (int)stream.Length;
    }

    private static int GetEntrySize(KeyValuePair<string, object> entry)
    {
        Dictionary<string, object> temp = new() { { entry.Key, entry.Value } };
        return GetMetadataSize(temp);
    }

    public static Dictionary<string, object> ConvertFilterToPineconeFilter(Dictionary<string, object> filter)
    {
        Dictionary<string, object> pineconeFilter = new();

        foreach (KeyValuePair<string, object> entry in filter)
        {
            pineconeFilter[entry.Key] = entry.Value switch
            {
                PineconeOperator op => op.ToDictionary(),
                IList list => new PineconeOperator("$in", list).ToDictionary(),

                DateTimeOffset dateTimeOffset => new PineconeOperator("$eq", dateTimeOffset.ToUnixTimeSeconds()).ToDictionary(),
                _ => new PineconeOperator("$eq", entry.Value).ToDictionary()
            };
        }

        return pineconeFilter;
    }

    public static string MetricTypeToString(IndexMetric indexMetric)
    {
        return indexMetric switch
        {
            IndexMetric.Cosine => "cosine",
            IndexMetric.Dotproduct => "dotProduct",
            IndexMetric.Euclidean => "euclidean",
            _ => string.Empty
        };
    }

    public static string PodTypeToString(PodType podType)
    {
        return podType switch
        {
            PodType.P1X1 => "p1x1",
            PodType.P1X2 => "p1x2",
            PodType.P1X4 => "p1x4",
            PodType.P1X8 => "p1x8",
            PodType.P2X1 => "p2x1",
            PodType.P2X2 => "p2x2",
            PodType.P2X4 => "p2x4",
            PodType.P2X8 => "p2x8",
            PodType.S1X1 => "s1x1",
            PodType.S1X2 => "s1x2",
            PodType.S1X4 => "s1x4",
            PodType.S1X8 => "s1x8",
            _ => string.Empty
        };
    }


    public sealed class PineconeOperator
    {
        public string Operator { get; }

        public object Value { get; }

        public PineconeOperator(string op, object value)
        {
            Operator = op;
            Value = value;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {
                    Operator, Value
                }
            };
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using static System.Net.Mime.MediaTypeNames;

namespace AISmarteasy.Core.Memory;

public class Embedding
{
    public static async Task<bool> SaveFromPdfDirectory(ISemanticMemory memory, string directory)
    {
        const int Max_Content_Item_Size = 2048;
        var memoryCollectionName = "smarteasy";

        var pdfFiles = Directory.GetFiles(directory, "*.pdf");

        foreach (var pdfFileName in pdfFiles)
        {
            var pdfDocument = PdfDocument.Open(pdfFileName);

            foreach (var pdfPage in pdfDocument.GetPages())
            {
                var pageText = ContentOrderTextExtractor.GetText(pdfPage);
                var paragraphs = new List<string>();

                if (pageText.Length > Max_Content_Item_Size)
                {
                    var lines = TextChunker.SplitPlainTextLines(pageText, Max_Content_Item_Size);
                    paragraphs = TextChunker.SplitPlainTextParagraphs(lines, Max_Content_Item_Size);
                }
                else
                {
                    paragraphs.Add(pageText);
                }

                foreach (var paragraph in paragraphs)
                {
                    var fileName = Path.GetFileName(pdfFileName);
                    var id = fileName + pdfPage.Number + paragraphs.IndexOf(paragraph);
                    var saveResult = await SaveAsync(memory, memoryCollectionName, paragraph, id).ConfigureAwait(false);
                }
            }
        }

        return true;
    }

    public static Task<IAsyncEnumerable<MemoryQueryResult>> SearchAsync(ISemanticMemory memory, string query,
        int limit = 1, double minRelevanceScore = 0.7, bool withEmbeddings = false, CancellationToken cancellationToken = default)
    {
        var memoryCollectionName = "smarteasy";
        var memoryResults = memory.SearchAsync(memoryCollectionName, query, limit, minRelevanceScore, withEmbeddings, cancellationToken);
        return Task.FromResult(memoryResults);
    }

    public static async Task<bool> SaveAsync(ISemanticMemory memory, Dictionary<string, string> textDictionary)
    {
        const int Max_Content_Item_Size = 2048;
        var memoryCollectionName = "smarteasy";

        foreach (var textData in textDictionary)
        {
            var id = textData.Key;

            if (textData.Value.Length > Max_Content_Item_Size)
            {
                var lines = TextChunker.SplitPlainTextLines(textData.Value, Max_Content_Item_Size);
                var texts = TextChunker.SplitPlainTextParagraphs(lines, Max_Content_Item_Size);
                foreach (var text in texts)
                {
                    var saveResult = await SaveAsync(memory, memoryCollectionName, text, id).ConfigureAwait(false);
                }
            }
            else
            {
                var saveResult = await SaveAsync(memory, memoryCollectionName, textData.Value, id)
                    .ConfigureAwait(false);
            }
        }

        return true;
    }

    private static async Task<string> SaveAsync(ISemanticMemory memory, string collection, string text, string id,
        string? description = null, string? additionalMetadata = null)

    {
        return await memory.SaveAsync(collection, text, id).ConfigureAwait(false);
    }
}
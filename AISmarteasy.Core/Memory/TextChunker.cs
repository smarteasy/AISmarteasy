using System.Diagnostics;
using System.Text;
using AISmarteasy.Core.Handling;

namespace AISmarteasy.Core.Memory;

public static class TextChunker
{
    public delegate int TokenCounter(string input);

    private static readonly char[] SpaceChar = { ' ' };
    private static readonly string?[] PlaintextSplitOptions = { "\n\r", ".", "?!", ";", ":", ",", ")]}", " ", "-", null };
    private static readonly string?[] MarkdownSplitOptions = { ".", "?!", ";", ":", ",", ")]}", " ", "-", "\n\r", null };

    public static List<string> SplitPlainTextLines(string text, int maxTokensPerLine, TokenCounter? tokenCounter = null)
    {
        tokenCounter ??= DefaultTokenCounter;

        return InternalSplitLines(text, maxTokensPerLine, trim: true, PlaintextSplitOptions, tokenCounter);
    }

    public static List<string> SplitMarkDownLines(string text, int maxTokensPerLine, TokenCounter? tokenCounter = null)
    {
        tokenCounter ??= DefaultTokenCounter;

        return InternalSplitLines(text, maxTokensPerLine, trim: true, MarkdownSplitOptions, tokenCounter);
    }

    public static List<string> SplitPlainTextParagraphs(List<string> lines, int maxTokensPerParagraph, int overlapTokens = 0,
        string? chunkHeader = null, TokenCounter? tokenCounter = null)
    {
        tokenCounter ??= DefaultTokenCounter;

        return InternalSplitTextParagraphs(lines, maxTokensPerParagraph, overlapTokens, chunkHeader,
            (text, maxTokens) => InternalSplitLines(text, maxTokens, trim: false, PlaintextSplitOptions, tokenCounter), tokenCounter);
    }

    public static List<string> SplitMarkdownParagraphs(List<string> lines, int maxTokensPerParagraph,
        int overlapTokens = 0, string? chunkHeader = null, TokenCounter? tokenCounter = null)
    {
        tokenCounter ??= DefaultTokenCounter;

        return InternalSplitTextParagraphs(lines, maxTokensPerParagraph, overlapTokens, chunkHeader, (text, maxTokens) => InternalSplitLines(text, maxTokens, trim: false, MarkdownSplitOptions, tokenCounter), tokenCounter);
    }

    private static List<string> InternalSplitTextParagraphs(List<string> lines, int maxTokensPerParagraph,
        int overlapTokens, string? chunkHeader, Func<string, int, List<string>> longLinesSplitter, TokenCounter tokenCounter)
    {
        if (maxTokensPerParagraph <= 0)
        {
            throw new ArgumentException("maxTokensPerParagraph should be a positive number");
        }

        if (maxTokensPerParagraph <= overlapTokens)
        {
            throw new ArgumentException("overlapTokens cannot be larger than maxTokensPerParagraph");
        }

        if (lines.Count == 0)
        {
            return new List<string>();
        }

        var chunkHeaderTokens = chunkHeader is { Length: > 0 } ? tokenCounter(chunkHeader) : 0;
        var adjustedMaxTokensPerParagraph = maxTokensPerParagraph - overlapTokens - chunkHeaderTokens;

        IEnumerable<string> truncatedLines = lines.SelectMany(line => longLinesSplitter(line, adjustedMaxTokensPerParagraph));

        var paragraphs = BuildParagraph(truncatedLines, adjustedMaxTokensPerParagraph, longLinesSplitter, tokenCounter);
        var processedParagraphs = ProcessParagraphs(paragraphs, adjustedMaxTokensPerParagraph, overlapTokens, chunkHeader, longLinesSplitter, tokenCounter);

        return processedParagraphs;
    }

    private static List<string> BuildParagraph(IEnumerable<string> truncatedLines, int maxTokensPerParagraph,
        Func<string, int, List<string>> longLinesSplitter, TokenCounter tokenCounter)
    {
        StringBuilder paragraphBuilder = new();
        List<string> paragraphs = new();

        foreach (string line in truncatedLines)
        {
            if (paragraphBuilder.Length > 0 && tokenCounter(paragraphBuilder.ToString()) + tokenCounter(line) + 1 >= maxTokensPerParagraph)
            {
                paragraphs.Add(paragraphBuilder.ToString().Trim());
                paragraphBuilder.Clear();
            }

            paragraphBuilder.AppendLine(line);
        }

        if (paragraphBuilder.Length > 0)
        {
            paragraphs.Add(paragraphBuilder.ToString().Trim());
        }

        return paragraphs;
    }

    private static List<string> ProcessParagraphs(List<string> paragraphs, int adjustedMaxTokensPerParagraph, int overlapTokens, string? chunkHeader, Func<string, int, List<string>> longLinesSplitter, TokenCounter tokenCounter)
    {
        var processedParagraphs = new List<string>();
        var paragraphStringBuilder = new StringBuilder();

        if (paragraphs.Count > 1)
        {
            var lastParagraph = paragraphs[paragraphs.Count - 1];
            var secondLastParagraph = paragraphs[paragraphs.Count - 2];

            if (tokenCounter(lastParagraph) < adjustedMaxTokensPerParagraph / 4)
            {
                var lastParagraphTokens = lastParagraph.Split(SpaceChar, StringSplitOptions.RemoveEmptyEntries);
                var secondLastParagraphTokens = secondLastParagraph.Split(SpaceChar, StringSplitOptions.RemoveEmptyEntries);

                var lastParagraphTokensCount = lastParagraphTokens.Length;
                var secondLastParagraphTokensCount = secondLastParagraphTokens.Length;

                if (lastParagraphTokensCount + secondLastParagraphTokensCount <= adjustedMaxTokensPerParagraph)
                {
                    var newSecondLastParagraph = string.Join(" ", secondLastParagraphTokens);
                    var newLastParagraph = string.Join(" ", lastParagraphTokens);

                    paragraphs[paragraphs.Count - 2] = $"{newSecondLastParagraph} {newLastParagraph}";
                    paragraphs.RemoveAt(paragraphs.Count - 1);
                }
            }
        }

        for (int i = 0; i < paragraphs.Count; i++)
        {
            paragraphStringBuilder.Clear();

            if (chunkHeader is not null)
            {
                paragraphStringBuilder.Append(chunkHeader);
            }

            var paragraph = paragraphs[i];

            if (overlapTokens > 0 && i < paragraphs.Count - 1)
            {
                var nextParagraph = paragraphs[i + 1];
                var split = longLinesSplitter(nextParagraph, overlapTokens);

                paragraphStringBuilder.Append(paragraph);

                if (split.FirstOrDefault() is { } overlap)
                {
                    paragraphStringBuilder.Append(' ');
                    paragraphStringBuilder.Append(overlap);
                }
            }
            else
            {
                paragraphStringBuilder.Append(paragraph);
            }

            processedParagraphs.Add(paragraphStringBuilder.ToString());
        }

        return processedParagraphs;
    }

    private static List<string> InternalSplitLines(string text, int maxTokensPerLine, bool trim, string?[] splitOptions, TokenCounter tokenCounter)
    {
        var result = new List<string>();

        text = text.NormalizeLineEndings();
        result.Add(text);
        for (int i = 0; i < splitOptions.Length; i++)
        {
            int count = result.Count;
            var (splits2, inputWasSplit2) = Split(result, maxTokensPerLine, splitOptions[i].AsSpan(), trim, tokenCounter);
            result.AddRange(splits2);
            result.RemoveRange(0, count);
            if (!inputWasSplit2)
            {
                break;
            }
        }
        return result;
    }

    private static (List<string>, bool) Split(List<string> input, int maxTokens, ReadOnlySpan<char> separators, bool trim, TokenCounter tokenCounter)
    {
        bool inputWasSplit = false;
        List<string> result = new();
        int count = input.Count;
        for (int i = 0; i < count; i++)
        {
            var (splits, split) = Split(input[i].AsSpan(), input[i], maxTokens, separators, trim, tokenCounter);
            result.AddRange(splits);
            inputWasSplit |= split;
        }
        return (result, inputWasSplit);
    }

    private static (List<string>, bool) Split(ReadOnlySpan<char> input, string? inputString, int maxTokens, ReadOnlySpan<char> separators, bool trim, TokenCounter tokenCounter)
    {
        Debug.Assert(inputString is null || input.SequenceEqual(inputString.AsSpan()));
        List<string> result = new();
        var inputWasSplit = false;
        if (tokenCounter(input.ToString()) > maxTokens)
        {
            inputWasSplit = true;

            int half = input.Length / 2;
            int cutPoint = -1;

            if (separators.IsEmpty)
            {
                cutPoint = half;
            }
            else if (input.Length > 2)
            {
                int pos = 0;
                while (true)
                {
                    int index = input.Slice(pos, input.Length - 1 - pos).IndexOfAny(separators);
                    if (index < 0)
                    {
                        break;
                    }

                    index += pos;

                    if (Math.Abs(half - index) < Math.Abs(half - cutPoint))
                    {
                        cutPoint = index + 1;
                    }

                    pos = index + 1;
                }
            }

            if (cutPoint > 0)
            {
                var firstHalf = input.Slice(0, cutPoint);
                var secondHalf = input.Slice(cutPoint);
                if (trim)
                {
                    firstHalf = firstHalf.Trim();
                    secondHalf = secondHalf.Trim();
                }

                // Recursion
                var (splits1, split1) = Split(firstHalf, null, maxTokens, separators, trim, tokenCounter);
                result.AddRange(splits1);
                var (splits2, split2) = Split(secondHalf, null, maxTokens, separators, trim, tokenCounter);
                result.AddRange(splits2);

                inputWasSplit = split1 || split2;
                return (result, inputWasSplit);
            }
        }

        result.Add((inputString is not null, trim) switch
        {
            (true, true) => inputString!.Trim(),
            (true, false) => inputString!,
            (false, true) => input.Trim().ToString(),
            (false, false) => input.ToString(),
        });

        return (result, inputWasSplit);
    }

    private static int DefaultTokenCounter(string input)
    {
        return input.Length / 4;
    }
}

using AISmarteasy.Core.Prompt.Blocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Prompt;

public sealed class TemplateTokenizer
{
    public TemplateTokenizer()
        : this(new NullLoggerFactory())
    {
    }

    public TemplateTokenizer(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _codeTokenizer = new CodeTokenizer(_loggerFactory);
    }


    public IList<Block> Tokenize(string text)
    {
        const int Empty_CodeBlock_Length = 4;
        const int Min_CodeBlock_Length = Empty_CodeBlock_Length + 1;

        if (string.IsNullOrEmpty(text))
        {
            return new List<Block> { new TextBlock(string.Empty, _loggerFactory) };
        }

        if (text.Length < Min_CodeBlock_Length)
        {
            return new List<Block> { new TextBlock(text, _loggerFactory) };
        }

        var blocks = new List<Block>();

        var endOfLastBlock = 0;

        var blockStartPos = 0;
        var blockStartFound = false;

        var insideTextValue = false;
        var textValueDelimiter = '\0';

        var skipNextChar = false;
        var nextChar = text[0];

        for (var nextCharCursor = 1; nextCharCursor < text.Length; nextCharCursor++)
        {
            var currentCharPos = nextCharCursor - 1;
            var currentChar = nextChar;
            nextChar = text[nextCharCursor];

            if (skipNextChar)
            {
                skipNextChar = false;
                continue;
            }

            if (!insideTextValue && currentChar == Symbols.BlockStarter && nextChar == Symbols.BlockStarter)
            {
                blockStartPos = currentCharPos;
                blockStartFound = true;
            }

            if (blockStartFound)
            {
                if (insideTextValue)
                {
                    if (currentChar == Symbols.EscapeChar && CanBeEscaped(nextChar))
                    {
                        skipNextChar = true;
                        continue;
                    }

                    if (currentChar == textValueDelimiter)
                    {
                        insideTextValue = false;
                    }
                }
                else
                {
                    if (IsQuote(currentChar))
                    {
                        insideTextValue = true;
                        textValueDelimiter = currentChar;
                    }
                    else if (currentChar == Symbols.BlockEnder && nextChar == Symbols.BlockEnder)
                    {
                        if (blockStartPos > endOfLastBlock)
                        {
                            blocks.Add(new TextBlock(text, endOfLastBlock, blockStartPos, _loggerFactory));
                        }
 
                        var contentWithDelimiters = SubStr(text, blockStartPos, nextCharCursor + 1);

                        var contentWithoutDelimiters = contentWithDelimiters
                            .Substring(2, contentWithDelimiters.Length - Empty_CodeBlock_Length)
                            .Trim();

                        if (contentWithoutDelimiters.Length == 0)
                        {
                            blocks.Add(new TextBlock(contentWithDelimiters, _loggerFactory));
                        }
                        else
                        {
                            var codeBlocks = _codeTokenizer.Tokenize(contentWithoutDelimiters);

                            switch (codeBlocks[0].Type)
                            {
                                case BlockTypeKind.Variable:
                                    if (codeBlocks.Count > 1)
                                    {
                                        throw new SKException($"Invalid token detected after the variable: {contentWithoutDelimiters}");
                                    }

                                    blocks.Add(codeBlocks[0]);
                                    break;

                                case BlockTypeKind.Value:
                                    if (codeBlocks.Count > 1)
                                    {
                                        throw new SKException($"Invalid token detected after the value: {contentWithoutDelimiters}");
                                    }

                                    blocks.Add(codeBlocks[0]);
                                    break;

                                case BlockTypeKind.FunctionId:
                                    blocks.Add(new CodeBlock(codeBlocks, contentWithoutDelimiters, _loggerFactory));
                                    break;

                                case BlockTypeKind.Code:
                                case BlockTypeKind.Text:
                                case BlockTypeKind.Undefined:
                                case BlockTypeKind.NamedArg:
                                default:
                                    throw new SKException($"Code tokenizer returned an incorrect first token type {codeBlocks[0].Type:G}");
                            }
                        }

                        endOfLastBlock = nextCharCursor + 1;
                        blockStartFound = false;
                    }
                }
            }
        }

        if (endOfLastBlock < text.Length)
        {
            blocks.Add(new TextBlock(text, endOfLastBlock, text.Length, _loggerFactory));
        }

        return blocks;
    }

    private readonly ILoggerFactory _loggerFactory;
    private readonly CodeTokenizer _codeTokenizer;

    private static string SubStr(string text, int startIndex, int stopIndex)
    {
        return text.Substring(startIndex, stopIndex - startIndex);
    }

    private static bool IsQuote(char c)
    {
        return c is Symbols.DblQuote or Symbols.SglQuote;
    }

    private static bool CanBeEscaped(char c)
    {
        return c is Symbols.DblQuote or Symbols.SglQuote or Symbols.EscapeChar;
    }
}

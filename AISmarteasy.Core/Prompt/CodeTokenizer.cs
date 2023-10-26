using System.Diagnostics.CodeAnalysis;
using System.Text;
using AISmarteasy.Core.Prompt.Blocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Prompt;

internal sealed class CodeTokenizer
{
    private readonly ILoggerFactory _loggerFactory;

    public CodeTokenizer(ILoggerFactory? loggerFactory = null)
    {
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public List<Block> Tokenize(string? text)
    {
        text = text?.Trim();

        if (string.IsNullOrEmpty(text)) { return new List<Block>(); }

        TokenTypeKind currentTokenType = TokenTypeKind.None;

        var currentTokenContent = new StringBuilder();

        char textValueDelimiter = '\0';

        var blocks = new List<Block>();
        char nextChar = text[0];

        bool spaceSeparatorFound = false;

        bool namedArgSeparatorFound = false;
        char namedArgValuePrefix = '\0';


        if (text.Length == 1)
        {
            switch (nextChar)
            {
                case Symbols.VAR_PREFIX:
                    blocks.Add(new VariableBlock(text, this._loggerFactory));
                    break;

                case Symbols.DBL_QUOTE:
                case Symbols.SGL_QUOTE:
                    blocks.Add(new ValueBlock(text, this._loggerFactory));
                    break;

                default:
                    blocks.Add(new FunctionIdBlock(text, this._loggerFactory));
                    break;
            }

            return blocks;
        }

        bool skipNextChar = false;
        for (int nextCharCursor = 1; nextCharCursor < text.Length; nextCharCursor++)
        {
            char currentChar = nextChar;
            nextChar = text[nextCharCursor];

            if (skipNextChar)
            {
                skipNextChar = false;
                continue;
            }

            if (nextCharCursor == 1)
            {
                if (IsVarPrefix(currentChar))
                {
                    currentTokenType = TokenTypeKind.Variable;
                }
                else if (IsQuote(currentChar))
                {
                    currentTokenType = TokenTypeKind.Value;
                    textValueDelimiter = currentChar;
                }
                else
                {
                    currentTokenType = TokenTypeKind.FunctionId;
                }

                currentTokenContent.Append(currentChar);
                continue;
            }

            if (currentTokenType == TokenTypeKind.Value || (currentTokenType == TokenTypeKind.NamedArg && IsQuote(namedArgValuePrefix)))
            {
                if (currentChar == Symbols.ESCAPE_CHAR && CanBeEscaped(nextChar))
                {
                    currentTokenContent.Append(nextChar);
                    skipNextChar = true;
                    continue;
                }

                currentTokenContent.Append(currentChar);

                if (currentChar == textValueDelimiter && currentTokenType == TokenTypeKind.Value)
                {
                    blocks.Add(new ValueBlock(currentTokenContent.ToString(), this._loggerFactory));
                    currentTokenContent.Clear();
                    currentTokenType = TokenTypeKind.None;
                    spaceSeparatorFound = false;
                }
                else if (currentChar == namedArgValuePrefix && currentTokenType == TokenTypeKind.NamedArg)
                {
                    blocks.Add(new NamedArgBlock(currentTokenContent.ToString(), this._loggerFactory));
                    currentTokenContent.Clear();
                    currentTokenType = TokenTypeKind.None;
                    spaceSeparatorFound = false;
                    namedArgSeparatorFound = false;
                    namedArgValuePrefix = '\0';
                }

                continue;
            }

            if (IsBlankSpace(currentChar))
            {
                if (currentTokenType == TokenTypeKind.Variable)
                {
                    blocks.Add(new VariableBlock(currentTokenContent.ToString(), this._loggerFactory));
                    currentTokenContent.Clear();
                    currentTokenType = TokenTypeKind.None;
                }
                else if (currentTokenType == TokenTypeKind.FunctionId)
                {
                    var tokenContent = currentTokenContent.ToString();

                    if (CodeTokenizer.IsValidNamedArg(tokenContent))
                    {
                        blocks.Add(new NamedArgBlock(tokenContent, this._loggerFactory));
                    }
                    else
                    {
                        blocks.Add(new FunctionIdBlock(tokenContent, this._loggerFactory));
                    }
                    currentTokenContent.Clear();
                    currentTokenType = TokenTypeKind.None;
                }
                else if (currentTokenType == TokenTypeKind.NamedArg && namedArgSeparatorFound && namedArgValuePrefix != 0)
                {
                    blocks.Add(new NamedArgBlock(currentTokenContent.ToString(), this._loggerFactory));
                    currentTokenContent.Clear();
                    namedArgSeparatorFound = false;
                    namedArgValuePrefix = '\0';
                    currentTokenType = TokenTypeKind.None;
                }

                spaceSeparatorFound = true;

                continue;
            }

            if (currentTokenType == TokenTypeKind.NamedArg && (!namedArgSeparatorFound || namedArgValuePrefix == 0))
            {
                if (!namedArgSeparatorFound)
                {
                    if (currentChar == Symbols.NAMED_ARG_BLOCK_SEPARATOR)
                    {
                        namedArgSeparatorFound = true;
                    }
                }
                else
                {
                    namedArgValuePrefix = currentChar;
                    if (!IsQuote(namedArgValuePrefix) && namedArgValuePrefix != Symbols.VAR_PREFIX)
                    {
                        throw new SKException($"Named argument values need to be prefixed with a quote or {Symbols.VAR_PREFIX}.");
                    }
                }
                currentTokenContent.Append(currentChar);
                continue;
            }

            currentTokenContent.Append(currentChar);

            if (currentTokenType == TokenTypeKind.None)
            {
                if (!spaceSeparatorFound)
                {
                    throw new SKException("Tokens must be separated by one space least");
                }

                if (IsQuote(currentChar))
                {
                    currentTokenType = TokenTypeKind.Value;
                    textValueDelimiter = currentChar;
                }
                else if (IsVarPrefix(currentChar))
                {
                    currentTokenType = TokenTypeKind.Variable;
                }
                else if (blocks.Count == 0)
                {
                    currentTokenType = TokenTypeKind.FunctionId;
                }
                else
                {
                    currentTokenType = TokenTypeKind.NamedArg;
                }
            }
        }

        currentTokenContent.Append(nextChar);
        switch (currentTokenType)
        {
            case TokenTypeKind.Value:
                blocks.Add(new ValueBlock(currentTokenContent.ToString(), _loggerFactory));
                break;

            case TokenTypeKind.Variable:
                blocks.Add(new VariableBlock(currentTokenContent.ToString(), _loggerFactory));
                break;

            case TokenTypeKind.FunctionId:
                var tokenContent = currentTokenContent.ToString();

                if (CodeTokenizer.IsValidNamedArg(tokenContent))
                {
                    blocks.Add(new NamedArgBlock(tokenContent, _loggerFactory));
                }
                else
                {
                    blocks.Add(new FunctionIdBlock(currentTokenContent.ToString(), _loggerFactory));
                }
                break;

            case TokenTypeKind.NamedArg:
                blocks.Add(new NamedArgBlock(currentTokenContent.ToString(), _loggerFactory));
                break;

            case TokenTypeKind.None:
                throw new SKException("Tokens must be separated by one space least");
        }

        return blocks;
    }

    private static bool IsVarPrefix(char c)
    {
        return (c == Symbols.VAR_PREFIX);
    }

    private static bool IsBlankSpace(char c)
    {
        return c is Symbols.SPACE or Symbols.NEW_LINE or Symbols.CARRIAGE_RETURN or Symbols.TAB;
    }

    private static bool IsQuote(char c)
    {
        return c is Symbols.DBL_QUOTE or Symbols.SGL_QUOTE;
    }

    private static bool CanBeEscaped(char c)
    {
        return c is Symbols.DBL_QUOTE or Symbols.SGL_QUOTE or Symbols.ESCAPE_CHAR;
    }

    [SuppressMessage("Design", "CA1031:Modify to catch a more specific allowed exception type, or rethrow exception",
    Justification = "Does not throw an exception by design.")]
    private static bool IsValidNamedArg(string tokenContent)
    {
        try
        {
            var tokenContentAsNamedArg = new NamedArgBlock(tokenContent);
            return tokenContentAsNamedArg.IsValid(out _);
        }
        catch
        {
            return false;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Exception;
using SemanticKernel.Prompt.Blocks;

namespace SemanticKernel.Prompt;

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
        char nextChar = text![0];

        bool spaceSeparatorFound = false;

        bool namedArgSeparatorFound = false;
        char namedArgValuePrefix = '\0';


        if (text.Length == 1)
        {
            switch (nextChar)
            {
                case Symbols.VarPrefix:
                    blocks.Add(new VarBlock(text, this._loggerFactory));
                    break;

                case Symbols.DblQuote:
                case Symbols.SglQuote:
                    blocks.Add(new ValBlock(text, this._loggerFactory));
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
                if (currentChar == Symbols.EscapeChar && CanBeEscaped(nextChar))
                {
                    currentTokenContent.Append(nextChar);
                    skipNextChar = true;
                    continue;
                }

                currentTokenContent.Append(currentChar);

                if (currentChar == textValueDelimiter && currentTokenType == TokenTypeKind.Value)
                {
                    blocks.Add(new ValBlock(currentTokenContent.ToString(), this._loggerFactory));
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
                    blocks.Add(new VarBlock(currentTokenContent.ToString(), this._loggerFactory));
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
                    if (currentChar == Symbols.NamedArgBlockSeparator)
                    {
                        namedArgSeparatorFound = true;
                    }
                }
                else
                {
                    namedArgValuePrefix = currentChar;
                    if (!IsQuote((char)namedArgValuePrefix) && namedArgValuePrefix != Symbols.VarPrefix)
                    {
                        throw new SKException($"Named argument values need to be prefixed with a quote or {Symbols.VarPrefix}.");
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
                blocks.Add(new ValBlock(currentTokenContent.ToString(), this._loggerFactory));
                break;

            case TokenTypeKind.Variable:
                blocks.Add(new VarBlock(currentTokenContent.ToString(), this._loggerFactory));
                break;

            case TokenTypeKind.FunctionId:
                var tokenContent = currentTokenContent.ToString();

                if (CodeTokenizer.IsValidNamedArg(tokenContent))
                {
                    blocks.Add(new NamedArgBlock(tokenContent, this._loggerFactory));
                }
                else
                {
                    blocks.Add(new FunctionIdBlock(currentTokenContent.ToString(), this._loggerFactory));
                }
                break;

            case TokenTypeKind.NamedArg:
                blocks.Add(new NamedArgBlock(currentTokenContent.ToString(), this._loggerFactory));
                break;

            case TokenTypeKind.None:
                throw new SKException("Tokens must be separated by one space least");
        }

        return blocks;
    }

    private static bool IsVarPrefix(char c)
    {
        return (c == Symbols.VarPrefix);
    }

    private static bool IsBlankSpace(char c)
    {
        return c is Symbols.Space or Symbols.NewLine or Symbols.CarriageReturn or Symbols.Tab;
    }

    private static bool IsQuote(char c)
    {
        return c is Symbols.DblQuote or Symbols.SglQuote;
    }

    private static bool CanBeEscaped(char c)
    {
        return c is Symbols.DblQuote or Symbols.SglQuote or Symbols.EscapeChar;
    }

    [SuppressMessage("Design", "CA1031:Modify to catch a more specific allowed exception type, or rethrow exception",
    Justification = "Does not throw an exception by design.")]
    private static bool IsValidNamedArg(string tokenContent)
    {
        try
        {
            var tokenContentAsNamedArg = new NamedArgBlock(tokenContent);
            return tokenContentAsNamedArg.IsValid(out var error);
        }
        catch
        {
            return false;
        }
    }
}

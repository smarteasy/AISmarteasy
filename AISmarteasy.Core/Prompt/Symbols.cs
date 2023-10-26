namespace AISmarteasy.Core.Prompt;

internal static class Symbols
{
    internal const char BLOCK_STARTER = '{';
    internal const char BLOCK_ENDER = '}';

    internal const char VAR_PREFIX = '$';
    internal const char NAMED_ARG_BLOCK_SEPARATOR = '=';

    internal const char DBL_QUOTE = '"';
    internal const char SGL_QUOTE = '\'';
    internal const char ESCAPE_CHAR = '\\';

    internal const char SPACE = ' ';
    internal const char TAB = '\t';
    internal const char NEW_LINE = '\n';
    internal const char CARRIAGE_RETURN = '\r';
}

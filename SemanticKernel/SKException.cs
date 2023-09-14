namespace SemanticKernel;

/// <summary>
/// Represents the base exception from which all Semantic Kernel exceptions derive.
/// </summary>
public class SKException : Exception
{
    public SKException()
    {
    }

    public SKException(string? message) : base(message)
    {
    }

    public SKException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

namespace SemanticKernel.Exception;


public class SKException : System.Exception
{
    public SKException()
    {
    }

    public SKException(string? message) : base(message)
    {
    }

    public SKException(string? message, System.Exception? innerException) : base(message, innerException)
    {
    }
}

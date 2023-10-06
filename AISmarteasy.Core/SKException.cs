namespace AISmarteasy.Core;


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

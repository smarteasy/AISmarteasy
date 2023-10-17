namespace AISmarteasy.Core.Handling;

internal static class ExceptionExtensions
{
    internal static bool IsCriticalException(this Exception ex)
        => ex is ThreadAbortException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException
            or CannotUnloadAppDomainException
            or InvalidProgramException;
}

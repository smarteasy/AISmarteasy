using System.Threading;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using NS of Exception
namespace SemanticKernel.Util;
#pragma warning restore IDE0130

internal static class ExceptionExtensions
{
    internal static bool IsCriticalException(this System.Exception ex)
        => ex is ThreadAbortException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException
            or CannotUnloadAppDomainException
            or InvalidProgramException;
}

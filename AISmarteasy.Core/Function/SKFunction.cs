using System.Reflection;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Function;

public static class SKFunction
{
    public static ISKFunction FromNativeMethod(
        MethodInfo method,
        object? target = null,
        string? pluginName = null,
        ILoggerFactory? loggerFactory = null)
            => NativeFunction.FromNativeMethod(method, target, pluginName, loggerFactory);

    public static ISKFunction FromNativeFunction(
        Delegate nativeFunction,
        string? skillName = null,
        string? functionName = null,
        string? description = null,
        IEnumerable<ParameterView>? parameters = null,
        ILoggerFactory? loggerFactory = null)
            => NativeFunction.FromNativeFunction(nativeFunction, skillName, functionName, description, parameters, loggerFactory);

    public static ISKFunction FromSemanticConfig(
        string skillName,
        string functionName,
        SemanticFunctionConfig functionConfig,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
            => SemanticFunction.FromSemanticConfig(skillName, functionName, functionConfig, loggerFactory, cancellationToken);
}

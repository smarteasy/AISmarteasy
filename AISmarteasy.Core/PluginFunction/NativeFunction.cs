using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Handling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class NativeFunction : Function
{
    internal NativeFunction(string pluginName, string name, string description, IList<ParameterView> parameters,
        Func<AIRequestSettings?, CancellationToken, Task> delegateFunction, ILogger logger)
    : base(pluginName, name, description, false, parameters)
    {
        _logger = logger;
        _function = delegateFunction;
    }

    public static Function FromNativeMethod(
        MethodInfo method,
        object? target = null,
        string? pluginName = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (!method.IsStatic && target is null)
        {
            throw new ArgumentNullException(nameof(target), "Argument cannot be null for non-static methods");
        }

        var logger = loggerFactory?.CreateLogger(method.DeclaringType ?? typeof(Function)) ?? NullLogger.Instance;
        var methodDetails = GetMethodDetails(method, target, logger);

        return new NativeFunction(pluginName!, methodDetails.Name, methodDetails.Description, methodDetails.Parameters, methodDetails.Function, logger);
    }

    public override Task RunAsync(AIRequestSettings requestSettings, CancellationToken cancellationToken = default)
    {
        try
        {
            return _function(requestSettings, cancellationToken);
        }
        catch (Exception e) when (!e.IsCriticalException())
        {
            _logger.LogError(e, "Native function {Plugin}.{Name} execution failed with error {Error}", PluginName, Name, e.Message);
            throw;
        }
    }

    private readonly Func<AIRequestSettings?, CancellationToken, Task> _function;
    private readonly ILogger _logger;

    private struct MethodDetails
    {
        public List<ParameterView> Parameters { get; set; }
        public string Name { get; init; }
        public string Description { get; init; }
        public Func<AIRequestSettings?, CancellationToken, Task> Function { get; set; }
    }

    private static MethodDetails GetMethodDetails(MethodInfo method, object? target, ILogger? logger = null)
    {
        Verify.NotNull(method);

        string? functionName = method.GetCustomAttribute<SKNameAttribute>(inherit: true)?.Name.Trim();
        if (string.IsNullOrEmpty(functionName))
        {
            functionName = SanitizeMetadataName(method.Name);
            Verify.ValidFunctionName(functionName);

            if (IsAsyncMethod(method) &&
                functionName.EndsWith("Async", StringComparison.Ordinal) &&
                functionName.Length > "Async".Length)
            {
                functionName = functionName.Substring(0, functionName.Length - "Async".Length);
            }
        }

        method.GetCustomAttribute<SKFunctionAttribute>(inherit: true);

        string? description = method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;

        var result = new MethodDetails
        {
            Name = functionName,
            Description = description ?? string.Empty,
        };

        (result.Function, result.Parameters) = GetDelegateInfo(target, method);

        logger?.LogTrace("Method '{0}' found", result.Name);

        return result;
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        Type t = method.ReturnType;

        if (t == typeof(Task) || t == typeof(ValueTask))
        {
            return true;
        }

        if (t.IsGenericType)
        {
            t = t.GetGenericTypeDefinition();
            if (t == typeof(Task<>) || t == typeof(ValueTask<>))
            {
                return true;
            }
        }

        return false;
    }

    private static (Func<AIRequestSettings?, CancellationToken, Task> function, List<ParameterView>) GetDelegateInfo(object? instance, MethodInfo method)
    {
        ThrowForInvalidSignatureIf(method.IsGenericMethodDefinition, method, "Generic methods are not supported");

        var stringParameterViews = new List<ParameterView>();
        var parameters = method.GetParameters();

        var parameterFuncs = new Func<SKContext, CancellationToken, object?>[parameters.Length];
        bool sawFirstParameter = false, hasSKContextParam = false, hasCancellationTokenParam = false, hasLoggerParam = false, hasCultureParam = false;
        for (int i = 0; i < parameters.Length; i++)
        {
            (parameterFuncs[i], ParameterView? parameterView) = GetParameterMarshalerDelegate(
                method, parameters[i],
                ref sawFirstParameter, ref hasSKContextParam, ref hasCancellationTokenParam, ref hasLoggerParam, ref hasCultureParam);
            if (parameterView is not null)
            {
                stringParameterViews.Add(parameterView);
            }
        }

        Func<object?, Task> returnFunc = GetReturnValueMarshalerDelegate(method);

        Task Function(AIRequestSettings? _, CancellationToken cancellationToken)
        {
            object?[] args = parameterFuncs.Length != 0 ? new object?[parameterFuncs.Length] : Array.Empty<object?>();
            var context = KernelProvider.Kernel!.Context;
            
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = parameterFuncs[i](context, cancellationToken);
            }

            object? result = method.Invoke(instance, args);

            return returnFunc(result);
        }

        stringParameterViews.AddRange(method
            .GetCustomAttributes<SKParameterAttribute>(inherit: true)
            .Select(x => new ParameterView(x.Name, x.Description, x.DefaultValue ?? string.Empty)));

        Verify.ParametersUniqueness(stringParameterViews);

        return (Function, stringParameterViews);
    }

    private static (Func<SKContext, CancellationToken, object?>, ParameterView?) GetParameterMarshalerDelegate(
        MethodInfo method, ParameterInfo parameter,
        ref bool sawFirstParameter, ref bool hasSKContextParam, ref bool hasCancellationTokenParam, ref bool hasLoggerParam, ref bool hasCultureParam)
    {
        Type type = parameter.ParameterType;


        if (type == typeof(SKContext))
        {
            TrackUniqueParameterType(ref hasSKContextParam, method, $"At most one {nameof(SKContext)} parameter is permitted.");
            return (static (context, _) => context, null);
        }

        if (type == typeof(ILogger) || type == typeof(ILoggerFactory))
        {
            TrackUniqueParameterType(ref hasLoggerParam, method, $"At most one {nameof(ILogger)}/{nameof(ILoggerFactory)} parameter is permitted.");
            return type == typeof(ILogger) ?
                ((context, _) => context.LoggerFactory.CreateLogger(method.DeclaringType ?? typeof(Function)), null) :
                ((context, _) => context.LoggerFactory, null);
        }

        if (type == typeof(CultureInfo) || type == typeof(IFormatProvider))
        {
            TrackUniqueParameterType(ref hasCultureParam, method, $"At most one {nameof(CultureInfo)}/{nameof(IFormatProvider)} parameter is permitted.");
            return (static (context, _) => context.Culture, null);
        }

        if (type == typeof(CancellationToken))
        {
            TrackUniqueParameterType(ref hasCancellationTokenParam, method, $"At most one {nameof(CancellationToken)} parameter is permitted.");
            return (static (_, cancellationToken) => cancellationToken, null);
        }

        if (!type.IsByRef && GetParser(type) is { } parser)
        {
            SKNameAttribute? nameAttr = parameter.GetCustomAttribute<SKNameAttribute>(inherit: true);
            string name = nameAttr?.Name.Trim() ?? SanitizeMetadataName(parameter.Name);
            bool nameIsInput = name.Equals("input", StringComparison.OrdinalIgnoreCase);
            ThrowForInvalidSignatureIf(name.Length == 0, method, $"Parameter {parameter.Name}'s context attribute defines an invalid name.");
            ThrowForInvalidSignatureIf(sawFirstParameter && nameIsInput, method, "Only the first parameter may be named 'input'");

            DefaultValueAttribute? defaultValueAttribute = parameter.GetCustomAttribute<DefaultValueAttribute>(inherit: true);
            bool hasDefaultValue = defaultValueAttribute is not null;
            object? defaultValue = defaultValueAttribute?.Value;
            if (!hasDefaultValue && parameter.HasDefaultValue)
            {
                hasDefaultValue = true;
                defaultValue = parameter.DefaultValue;
            }

            if (hasDefaultValue)
            {
                if (defaultValue is string defaultStringValue && defaultValue.GetType() != typeof(string))
                {
                    defaultValue = parser(defaultStringValue, CultureInfo.InvariantCulture);
                }
                else
                {
                    ThrowForInvalidSignatureIf(
                        defaultValue is null && type.IsValueType && Nullable.GetUnderlyingType(type) is null,
                        method,
                        $"Type {type} is a non-nullable value type but a null default value was specified.");
                    ThrowForInvalidSignatureIf(
                        defaultValue is not null && !type.IsInstanceOfType(defaultValue),
                        method,
                        $"Default value {defaultValue} for parameter {name} is not assignable to type {type}.");
                }
            }

            bool fallBackToInput = !sawFirstParameter && !nameIsInput;

            object? ParameterFunc(SKContext context, CancellationToken _)
            {
                if (context.Variables.TryGetValue(name, out string? value))
                {
                    return Process(value);
                }

                if (hasDefaultValue)
                {
                    return defaultValue;
                }

                if (fallBackToInput)
                {
                    return Process(context.Variables.Input);
                }

                throw new SKException($"Missing value for parameter '{name}'");

                object? Process(string str)
                {
                    if (type == typeof(string))
                    {
                        return str;
                    }

                    try
                    {
                        return parser(str, context.Culture);
                    }
                    catch (Exception e) when (!e.IsCriticalException())
                    {
                        throw new ArgumentOutOfRangeException(name, str, e.Message);
                    }
                }
            }

            sawFirstParameter = true;

            var parameterView = new ParameterView(
                name,
                parameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty,
                defaultValue?.ToString() ?? string.Empty);

            return (ParameterFunc, parameterView);
        }

        throw GetExceptionForInvalidSignature(method, $"Unknown parameter type {parameter.ParameterType}");
    }

    private static Func<object?, Task> GetReturnValueMarshalerDelegate(MethodInfo method)
    {
        Verify.NotNull(KernelProvider.Kernel);

        Type returnType = method.ReturnType;

        if (returnType == typeof(Task))
        {
            return async static (result) =>
            {
                await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
            };
        }

        if (returnType == typeof(ValueTask))
        {
            return async static (result) =>
            {
                await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(false);
            };
        }

        if (returnType == typeof(string))
        {
            return (result) =>
            {
                var context = KernelProvider.Kernel.Context;
                context.Variables.Update((string?)result);
                return Task.FromResult(context);
            };
        }

        if (returnType == typeof(Task<string>))
        {
            return async static (result) =>
            {
                var context = KernelProvider.Kernel.Context;
                context.Variables.Update(await ((Task<string>)ThrowIfNullResult(result)).ConfigureAwait(false));
            };
        }

        if (returnType == typeof(ValueTask<string>))
        {
            return async static (result) =>
            {
                var context = KernelProvider.Kernel.Context;
                context.Variables.Update(await ((ValueTask<string>)ThrowIfNullResult(result)).ConfigureAwait(false));
            };
        }

        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var formatter = GetFormatter(returnType) as Func<object?, CultureInfo, string>;
            if (formatter == null)
            {
                throw GetExceptionForInvalidSignature(method, $"Unknown return type {returnType}");
            }

            return (result) =>
            {
                var context = KernelProvider.Kernel.Context;
                context.Variables.Update(formatter(result, context.Culture));
                return Task.FromResult(context);
            };
        }

        if (returnType.GetGenericTypeDefinition() is { } genericTask &&
            genericTask == typeof(Task<>) &&
            returnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod() is { } taskResultGetter &&
            GetFormatter(taskResultGetter.ReturnType) is { } taskResultFormatter)
        {
            return async (result) =>
            {
                var context = KernelProvider.Kernel.Context;
                await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                context.Variables.Update(taskResultFormatter(taskResultGetter.Invoke(result!, Array.Empty<object>()), context.Culture));
            };
        }

        if (returnType.GetGenericTypeDefinition() is { } genericValueTask &&
            genericValueTask == typeof(ValueTask<>) &&
            returnType.GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance) is { } valueTaskAsTask &&
            valueTaskAsTask.ReturnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod() is { } asTaskResultGetter &&
            GetFormatter(asTaskResultGetter.ReturnType) is { } asTaskResultFormatter)
        {
            return async (result) =>
            {
                var context = KernelProvider.Kernel.Context;
                var task = (Task)valueTaskAsTask.Invoke(ThrowIfNullResult(result), Array.Empty<object>())!;
                await task.ConfigureAwait(false);
                context.Variables.Update(asTaskResultFormatter(asTaskResultGetter.Invoke(task, Array.Empty<object>()), context.Culture));
            };
        }

        throw GetExceptionForInvalidSignature(method, $"Unknown return type {returnType}");

        static object ThrowIfNullResult(object? result) =>
            result ??
            throw new SKException("Function returned null unexpectedly.");
    }

    [DoesNotReturn]
    private static Exception GetExceptionForInvalidSignature(MethodInfo method, string reason) =>
        throw new SKException($"Function '{method.Name}' is not supported by the kernel. {reason}");

    private static void ThrowForInvalidSignatureIf([DoesNotReturnIf(true)] bool condition, MethodInfo method, string reason)
    {
        if (condition)
        {
            throw GetExceptionForInvalidSignature(method, reason);
        }
    }

    private static void TrackUniqueParameterType(ref bool hasParameterType, MethodInfo method, string failureMessage)
    {
        ThrowForInvalidSignatureIf(hasParameterType, method, failureMessage);
        hasParameterType = true;
    }

    private static Func<string, CultureInfo, object?>? GetParser(Type targetType) =>
        Parsers.GetOrAdd(targetType, static targetType =>
        {
            if (targetType == typeof(string))
            {
                return (input, _) => input;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType) ?? throw new InvalidOperationException();
            }

            if (targetType.IsEnum)
            {
                return (input, _) => Enum.Parse(targetType, input, ignoreCase: true);
            }
            if (GetTypeConverter(targetType) is { } converter && converter.CanConvertFrom(typeof(string)))
            {
                return (input, cultureInfo) =>
                {
                    try
                    {
                        return converter.ConvertFromString(context: null, cultureInfo, input) ?? throw new InvalidOperationException();
                    }
                    catch (Exception e) when (!e.IsCriticalException() && !Equals(cultureInfo, CultureInfo.InvariantCulture))
                    {
                        return converter.ConvertFromInvariantString(input) ?? throw new InvalidOperationException();
                    }
                };
            }
            return null;
        });

    private static Func<object?, CultureInfo, string?>? GetFormatter(Type targetType) =>
        Formatters.GetOrAdd(targetType, static targetType =>
        {
            bool wasNullable = false;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                wasNullable = true;
                targetType = Nullable.GetUnderlyingType(targetType) ?? throw new InvalidOperationException();
            }

            if (targetType.IsEnum)
            {
                return (input, _) => input?.ToString()!;
            }

            if (targetType == typeof(string))
            {
                return (input, _) => (string)input!;
            }

            if (GetTypeConverter(targetType) is { } converter && converter.CanConvertTo(typeof(string)))
            {
                return (input, cultureInfo) =>
                {
                    if (wasNullable && input is null)
                    {
                        return null!;
                    }

                    return converter.ConvertToString(context: null, cultureInfo, input);
                };
            }

            return null;
        });

    private static TypeConverter? GetTypeConverter(Type targetType)
    {
        if (targetType == typeof(byte)) { return new ByteConverter(); }
        if (targetType == typeof(sbyte)) { return new SByteConverter(); }
        if (targetType == typeof(bool)) { return new BooleanConverter(); }
        if (targetType == typeof(ushort)) { return new UInt16Converter(); }
        if (targetType == typeof(short)) { return new Int16Converter(); }
        if (targetType == typeof(char)) { return new CharConverter(); }
        if (targetType == typeof(uint)) { return new UInt32Converter(); }
        if (targetType == typeof(int)) { return new Int32Converter(); }
        if (targetType == typeof(ulong)) { return new UInt64Converter(); }
        if (targetType == typeof(long)) { return new Int64Converter(); }
        if (targetType == typeof(float)) { return new SingleConverter(); }
        if (targetType == typeof(double)) { return new DoubleConverter(); }
        if (targetType == typeof(decimal)) { return new DecimalConverter(); }
        if (targetType == typeof(TimeSpan)) { return new TimeSpanConverter(); }
        if (targetType == typeof(DateTime)) { return new DateTimeConverter(); }
        if (targetType == typeof(DateTimeOffset)) { return new DateTimeOffsetConverter(); }
        if (targetType == typeof(Uri)) { return new UriTypeConverter(); }
        if (targetType == typeof(Guid)) { return new GuidConverter(); }

        if (targetType.GetCustomAttribute<TypeConverterAttribute>() is { } tca &&
            Type.GetType(tca.ConverterTypeName, throwOnError: false) is { } converterType &&
            Activator.CreateInstance(converterType) is TypeConverter converter)
        {
            return converter;
        }

        return null;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.Name} ({this.Description})";

    private static string SanitizeMetadataName(string? methodName) =>
        InvalidNameCharsRegex.Replace(methodName!, "_");

    private static readonly Regex InvalidNameCharsRegex = new("[^0-9A-Za-z_]");

    private static readonly ConcurrentDictionary<Type, Func<string, CultureInfo, object>?> Parsers = new();

    private static readonly ConcurrentDictionary<Type, Func<object?, CultureInfo, string?>?> Formatters = new();
}

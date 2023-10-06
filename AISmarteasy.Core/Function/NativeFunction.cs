﻿using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion;
using AISmarteasy.Core.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Util;

namespace AISmarteasy.Core.Function;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal sealed class NativeFunction : ISKFunction, IDisposable
{
    public string Name { get; }

    public string PluginName { get; }

    public string Description { get; }

    public bool IsSemantic { get; } = false;

    public AIRequestSettings RequestSettings { get; } = new();

    public IList<ParameterView> Parameters { get; }

    public static ISKFunction FromNativeMethod(
        MethodInfo method,
        object? target = null,
        string? pluginName = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (!method.IsStatic && target is null)
        {
            throw new ArgumentNullException(nameof(target), "Argument cannot be null for non-static methods");
        }

        var logger = loggerFactory?.CreateLogger(method.DeclaringType ?? typeof(SKFunction)) ?? NullLogger.Instance;
        var methodDetails = GetMethodDetails(method, target, logger);

        return new NativeFunction(
            delegateFunction: methodDetails.Function,
            parameters: methodDetails.Parameters,
            pluginName: pluginName!,
            functionName: methodDetails.Name,
            description: methodDetails.Description,
            logger: logger);
    }

    public static ISKFunction FromNativeFunction(
        Delegate nativeFunction,
        string? pluginName = null,
        string? functionName = null,
        string? description = null,
        IEnumerable<ParameterView>? parameters = null,
        ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(ISKFunction)) : NullLogger.Instance;
        var methodDetails = GetMethodDetails(nativeFunction.Method, nativeFunction.Target, logger);

        functionName ??= methodDetails.Name;
        parameters ??= methodDetails.Parameters;
        description ??= methodDetails.Description;

        return new NativeFunction(
            delegateFunction: methodDetails.Function,
            parameters: parameters?.ToList() ?? (IList<ParameterView>)Array.Empty<ParameterView>(),
            description: description,
            pluginName: pluginName!,
            functionName: functionName,
            logger: logger);
    }

    public FunctionView Describe()
    {
        return new FunctionView
        {
            Name = Name,
            PluginName = PluginName,
            Description = Description,
            Parameters = Parameters,
        };
    }

    public async Task<SKContext> InvokeAsync(SKContext context, AIRequestSettings? settings = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _function(settings, context, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Exception e) when (!e.IsCriticalException())
        {
            _logger.LogError(e, "Native function {Plugin}.{Name} execution failed with error {Error}", PluginName, Name, e.Message);
            throw;
        }
    }

    public ISKFunction SetDefaultPluginCollection(IPlugin plugins)
    {
        return this;
    }

    public ISKFunction SetAIConfiguration(AIRequestSettings settings)
    {
        return this;
    }


    public void Dispose()
    {
    }

    public override string ToString()
        => this.ToString(false);

    public string ToString(bool writeIndented)
        => JsonSerializer.Serialize(this, options: writeIndented ? ToStringIndentedSerialization : ToStringStandardSerialization);

    private static readonly JsonSerializerOptions ToStringStandardSerialization = new();
    private static readonly JsonSerializerOptions ToStringIndentedSerialization = new() { WriteIndented = true };
    private readonly Func<AIRequestSettings?, SKContext, CancellationToken, Task<SKContext>> _function;
    private readonly ILogger _logger;

    private struct MethodDetails
    {
        public Func<AIRequestSettings?, SKContext, CancellationToken, Task<SKContext>> Function { get; set; }
        public List<ParameterView> Parameters { get; set; }
        public string Name { get; init; }
        public string Description { get; init; }
    }

    private static async Task<string> GetCompletionsResultContentAsync(IReadOnlyList<ITextResult> completions, CancellationToken cancellationToken = default)
    {
       return await completions[0].GetCompletionAsync(cancellationToken).ConfigureAwait(false);
    }

    internal NativeFunction(
        Func<AIRequestSettings?, SKContext, CancellationToken, Task<SKContext>> delegateFunction,
        IList<ParameterView> parameters,
        string pluginName,
        string functionName,
        string description,
        ILogger logger)
    {
        Verify.NotNull(delegateFunction);
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(functionName);
        Verify.ParametersUniqueness(parameters);

        _logger = logger;

        _function = delegateFunction;
        Parameters = parameters;

        Name = functionName;
        PluginName = pluginName;
        Description = description;
    }

    [DoesNotReturn]
    private void ThrowNotSemantic()
    {
        this._logger.LogError("The function is not semantic");
        throw new SKException("Invalid operation, the method requires a semantic function");
    }

    private static MethodDetails GetMethodDetails(
        MethodInfo method,
        object? target,
        ILogger? logger = null)
    {
        Verify.NotNull(method);

        string? functionName = method.GetCustomAttribute<SKNameAttribute>(inherit: true)?.Name?.Trim();
        if (string.IsNullOrEmpty(functionName))
        {
            functionName = SanitizeMetadataName(method.Name!);
            Verify.ValidFunctionName(functionName);

            if (IsAsyncMethod(method) &&
                functionName.EndsWith("Async", StringComparison.Ordinal) &&
                functionName.Length > "Async".Length)
            {
                functionName = functionName.Substring(0, functionName.Length - "Async".Length);
            }
        }

        SKFunctionAttribute? functionAttribute = method.GetCustomAttribute<SKFunctionAttribute>(inherit: true);

        string? description = method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;

        var result = new MethodDetails
        {
            Name = functionName!,
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

    private static (Func<AIRequestSettings?, SKContext, CancellationToken, Task<SKContext>> function, List<ParameterView>) GetDelegateInfo(object? instance, MethodInfo method)
    {
        ThrowForInvalidSignatureIf(method.IsGenericMethodDefinition, method, "Generic methods are not supported");

        var stringParameterViews = new List<ParameterView>();
        var parameters = method.GetParameters();

        var parameterFuncs = new Func<SKContext, CancellationToken, object?>[parameters.Length];
        bool sawFirstParameter = false, hasSKContextParam = false, hasCancellationTokenParam = false, hasLoggerParam = false, hasMemoryParam = false, hasCultureParam = false;
        for (int i = 0; i < parameters.Length; i++)
        {
            (parameterFuncs[i], ParameterView? parameterView) = GetParameterMarshalerDelegate(
                method, parameters[i],
                ref sawFirstParameter, ref hasSKContextParam, ref hasCancellationTokenParam, ref hasLoggerParam, ref hasMemoryParam, ref hasCultureParam);
            if (parameterView is not null)
            {
                stringParameterViews.Add(parameterView);
            }
        }

        Func<object?, SKContext, Task<SKContext>> returnFunc = GetReturnValueMarshalerDelegate(method);

        Task<SKContext> Function(AIRequestSettings? _, SKContext context, CancellationToken cancellationToken)
        {
            object?[] args = parameterFuncs.Length != 0 ? new object?[parameterFuncs.Length] : Array.Empty<object?>();
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = parameterFuncs[i](context, cancellationToken);
            }

            object? result = method.Invoke(instance, args);

            return returnFunc(result, context);
        }

        stringParameterViews.AddRange(method
            .GetCustomAttributes<SKParameterAttribute>(inherit: true)
            .Select(x => new ParameterView(x.Name ?? string.Empty, x.Description ?? string.Empty, x.DefaultValue ?? string.Empty)));

        Verify.ParametersUniqueness(stringParameterViews);

        return (Function, stringParameterViews);
    }

    private static (Func<SKContext, CancellationToken, object?>, ParameterView?) GetParameterMarshalerDelegate(
        MethodInfo method, ParameterInfo parameter,
        ref bool sawFirstParameter, ref bool hasSKContextParam, ref bool hasCancellationTokenParam, ref bool hasLoggerParam, ref bool hasMemoryParam, ref bool hasCultureParam)
    {
        Type type = parameter.ParameterType;


        if (type == typeof(SKContext))
        {
            TrackUniqueParameterType(ref hasSKContextParam, method, $"At most one {nameof(SKContext)} parameter is permitted.");
            return (static (SKContext context, CancellationToken _) => context, null);
        }

        if (type == typeof(ILogger) || type == typeof(ILoggerFactory))
        {
            TrackUniqueParameterType(ref hasLoggerParam, method, $"At most one {nameof(ILogger)}/{nameof(ILoggerFactory)} parameter is permitted.");
            return type == typeof(ILogger) ?
                ((SKContext context, CancellationToken _) => context.LoggerFactory.CreateLogger(method?.DeclaringType ?? typeof(SKFunction)), null) :
                ((SKContext context, CancellationToken _) => context.LoggerFactory, null);
        }

        if (type == typeof(CultureInfo) || type == typeof(IFormatProvider))
        {
            TrackUniqueParameterType(ref hasCultureParam, method, $"At most one {nameof(CultureInfo)}/{nameof(IFormatProvider)} parameter is permitted.");
            return (static (SKContext context, CancellationToken _) => context.Culture, null);
        }

        if (type == typeof(CancellationToken))
        {
            TrackUniqueParameterType(ref hasCancellationTokenParam, method, $"At most one {nameof(CancellationToken)} parameter is permitted.");
            return (static (SKContext _, CancellationToken cancellationToken) => cancellationToken, null);
        }

        if (!type.IsByRef && GetParser(type) is { } parser)
        {
            SKNameAttribute? nameAttr = parameter.GetCustomAttribute<SKNameAttribute>(inherit: true);
            string name = nameAttr?.Name?.Trim() ?? SanitizeMetadataName(parameter.Name);
            bool nameIsInput = name.Equals("input", StringComparison.OrdinalIgnoreCase);
            ThrowForInvalidSignatureIf(name.Length == 0, method, $"Parameter {parameter.Name}'s context attribute defines an invalid name.");
            ThrowForInvalidSignatureIf(sawFirstParameter && nameIsInput, method, "Only the first parameter may be named 'input'");

            DefaultValueAttribute defaultValueAttribute = parameter.GetCustomAttribute<DefaultValueAttribute>(inherit: true);
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
                        defaultValue is not null && !type.IsAssignableFrom(defaultValue.GetType()),
                        method,
                        $"Default value {defaultValue} for parameter {name} is not assignable to type {type}.");
                }
            }

            bool fallBackToInput = !sawFirstParameter && !nameIsInput;
            Func<SKContext, CancellationToken, object?> parameterFunc = (SKContext context, CancellationToken _) =>
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

                object? Process(string value)
                {
                    if (type == typeof(string))
                    {
                        return value;
                    }

                    try
                    {
                        return parser(value, context.Culture);
                    }
                    catch (System.Exception e) when (!e.IsCriticalException())
                    {
                        throw new ArgumentOutOfRangeException(name, value, e.Message);
                    }
                }
            };

            sawFirstParameter = true;

            var parameterView = new ParameterView(
                name,
                parameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty,
                defaultValue?.ToString() ?? string.Empty);

            return (parameterFunc, parameterView);
        }

        throw GetExceptionForInvalidSignature(method, $"Unknown parameter type {parameter.ParameterType}");
    }

    private static Func<object?, SKContext, Task<SKContext>> GetReturnValueMarshalerDelegate(MethodInfo method)
    {
        Type returnType = method.ReturnType;

        if (returnType == typeof(void))
        {
            return static (result, context) => Task.FromResult(context);
        }

        if (returnType == typeof(Task))
        {
            return async static (result, context) =>
            {
                await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                return context;
            };
        }

        if (returnType == typeof(ValueTask))
        {
            return async static (result, context) =>
            {
                await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(false);
                return context;
            };
        }

        if (returnType == typeof(SKContext))
        {
            return static (result, _) => Task.FromResult((SKContext)ThrowIfNullResult(result));
        }

        if (returnType == typeof(Task<SKContext>))
        {
            return static (result, _) => (Task<SKContext>)ThrowIfNullResult(result);
        }

        if (returnType == typeof(ValueTask<SKContext>))
        {
            return static (result, context) => ((ValueTask<SKContext>)ThrowIfNullResult(result)).AsTask();
        }

        if (returnType == typeof(string))
        {
            return static (result, context) =>
            {
                context.Variables.Update((string?)result);
                return Task.FromResult(context);
            };
        }

        if (returnType == typeof(Task<string>))
        {
            return async static (result, context) =>
            {
                context.Variables.Update(await ((Task<string>)ThrowIfNullResult(result)).ConfigureAwait(false));
                return context;
            };
        }

        if (returnType == typeof(ValueTask<string>))
        {
            return async static (result, context) =>
            {
                context.Variables.Update(await ((ValueTask<string>)ThrowIfNullResult(result)).ConfigureAwait(false));
                return context;
            };
        }

        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (GetFormatter(returnType) is not Func<object?, CultureInfo, string> formatter)
            {
                throw GetExceptionForInvalidSignature(method, $"Unknown return type {returnType}");
            }

            return (result, context) =>
            {
                context.Variables.Update(formatter(result, context.Culture));
                return Task.FromResult(context);
            };
        }

        if (returnType.GetGenericTypeDefinition() is Type genericTask &&
            genericTask == typeof(Task<>) &&
            returnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod() is MethodInfo taskResultGetter &&
            GetFormatter(taskResultGetter.ReturnType) is Func<object?, CultureInfo, string> taskResultFormatter)
        {
            return async (result, context) =>
            {
                await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                context.Variables.Update(taskResultFormatter(taskResultGetter.Invoke(result!, Array.Empty<object>()), context.Culture));
                return context;
            };
        }

        if (returnType.GetGenericTypeDefinition() is Type genericValueTask &&
            genericValueTask == typeof(ValueTask<>) &&
            returnType.GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance) is MethodInfo valueTaskAsTask &&
            valueTaskAsTask.ReturnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod() is MethodInfo asTaskResultGetter &&
            GetFormatter(asTaskResultGetter.ReturnType) is { } asTaskResultFormatter)
        {
            return async (result, context) =>
            {
                Task task = (Task)valueTaskAsTask.Invoke(ThrowIfNullResult(result), Array.Empty<object>());
                await task.ConfigureAwait(false);
                context.Variables.Update(asTaskResultFormatter(asTaskResultGetter.Invoke(task!, Array.Empty<object>()), context.Culture));
                return context;
            };
        }

        throw GetExceptionForInvalidSignature(method, $"Unknown return type {returnType}");

        static object ThrowIfNullResult(object? result) =>
            result ??
            throw new SKException("Function returned null unexpectedly.");
    }

    [DoesNotReturn]
    private static System.Exception GetExceptionForInvalidSignature(MethodInfo method, string reason) =>
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
                return (input, cultureInfo) => input;
            }

            bool wasNullable = false;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                wasNullable = true;
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            if (targetType.IsEnum)
            {
                return (input, cultureInfo) =>
                {
                    if (wasNullable && input is null)
                    {
                        return null!;
                    }

                    return Enum.Parse(targetType, input, ignoreCase: true);
                };
            }
            if (GetTypeConverter(targetType) is { } converter && converter.CanConvertFrom(typeof(string)))
            {
                return (input, cultureInfo) =>
                {
                    if (wasNullable && input is null)
                    {
                        return null!;
                    }
                    try
                    {
                        return converter.ConvertFromString(context: null, cultureInfo, input);
                    }
                    catch (System.Exception e) when (!e.IsCriticalException() && cultureInfo != CultureInfo.InvariantCulture)
                    {
                        return converter.ConvertFromInvariantString(input);
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
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            if (targetType.IsEnum)
            {
                return (input, cultureInfo) => input?.ToString()!;
            }

            if (targetType == typeof(string))
            {
                return (input, cultureInfo) => (string)input!;
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

        if (targetType.GetCustomAttribute<TypeConverterAttribute>() is TypeConverterAttribute tca &&
            Type.GetType(tca.ConverterTypeName, throwOnError: false) is Type converterType &&
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

    private static readonly ConcurrentDictionary<Type, Func<object?, CultureInfo, string>?> Formatters = new();
}
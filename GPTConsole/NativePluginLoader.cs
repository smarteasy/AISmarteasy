using System.Reflection;
using Microsoft.Extensions.Logging;
using SemanticKernel;
using SemanticKernel.Function;

namespace GPTConsole;

public class NativePluginLoader
{
    private readonly Kernel _kernel;

    public NativePluginLoader()
    {
        _kernel = KernelProvider.Kernel;
    }
    
    public void Load()
    {
        var nativeDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Plugins", "Native");
        var pluginNamespace = "Plugins.Native.Skills";

        LoadPlugin(nativeDirectory, pluginNamespace);
    }

    private void LoadPlugin(string nativeDirectory, string pluginNamespace)
    {
        var logger = _kernel.LoggerFactory.CreateLogger("NativeFunction");

        foreach (var file in Directory.GetFiles(nativeDirectory))
        {
            var fileNameWithExt = Path.GetFileName(file);
            var pluginName  = fileNameWithExt.Substring(0, fileNameWithExt.Length - 3);
            var typeName = pluginNamespace + "." + pluginName;

            var type = Type.GetType(typeName);

            var instance = Activator.CreateInstance(type!);
            MethodInfo[] methods = type!.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            Dictionary<string, ISKFunction> functions = new(StringComparer.OrdinalIgnoreCase);

            logger.LogTrace("Importing plugin name: {0}.", typeName);

            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<SKFunctionAttribute>() is not null)
                {
                    ISKFunction function = SKFunction.FromNativeMethod(method, instance, pluginName);
                    if (functions.ContainsKey(function.Name))
                    {
                        throw new SKException("Function overloads are not supported, please differentiate function names");
                    }

                    functions.Add(function.Name, function);

                    _kernel.RegisterNativeFunction(function);

                    logger.LogTrace("Methods imported {0}", functions.Count);
                }
            }
        }
    }
}


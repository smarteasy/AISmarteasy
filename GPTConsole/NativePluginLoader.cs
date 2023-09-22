using System.Reflection;
using Microsoft.Extensions.Logging;
using SemanticKernel;
using SemanticKernel.Function;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GPTConsole;

public class NativePluginLoader
{
    private readonly IKernel _kernel;

    public NativePluginLoader()
    {
        _kernel = KernelProvider.Kernel;
    }
    
    public void Load()
    {
        var nativeDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "native");
        var pluginPath = "Plugins";

        Load(pluginPath, Directory.GetDirectories(nativeDirectory));
    }

    private void Load(string pluginPath, string[] subDirectories)
    {
        foreach (var subDirectory in subDirectories)
        {
            var fileNames = new List<string>();
            foreach (var file in Directory.GetFiles(subDirectory))
            {
                var fileNameWithExt = Path.GetFileName(file);
                fileNames.Add(fileNameWithExt.Substring(0, fileNameWithExt.Length - 3));
            }

            var directoryName = Path.GetFileName(subDirectory);

            if (fileNames.Count == 0)
            {
                pluginPath += "." + directoryName;
                Load(pluginPath, Directory.GetDirectories(subDirectory));
            }
            else
            {
                LoadFunction(directoryName, pluginPath, fileNames.ToArray());
            }
        }
    }

    private void LoadFunction(string plugin, string pluginPath, string[] fileNames)
    {
        pluginPath += "." + plugin;
        Dictionary<string, ISKFunction> functions = new(StringComparer.OrdinalIgnoreCase);
        var logger = _kernel.LoggerFactory.CreateLogger("NativeFunction");

        foreach (var fileName in fileNames)
        {
            string typeName = pluginPath + "." + fileName;
            var type = Type.GetType(typeName);
            var instance = Activator.CreateInstance(type!);
            MethodInfo[] methods = type!.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            logger.LogTrace("Importing plugin name: {0}. Potential methods found: {1}", type.Name, methods.Length);

            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<SKFunctionAttribute>() is not null)
                {
                    ISKFunction function = SKFunction.FromNativeMethod(method, instance, plugin);
                    if (functions.ContainsKey(function.Name))
                    {
                        throw new SKException("Function overloads are not supported, please differentiate function names");
                    }

                    functions.Add(function.Name, function);

                    _kernel.RegisterNativeFunction(function);
                }
            }

        }

        logger.LogTrace("Methods imported {0}", functions.Count);
    }
}


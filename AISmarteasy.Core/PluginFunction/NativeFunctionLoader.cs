using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.PluginFunction;

public static class NativeFunctionLoader
{
    public static Dictionary<string, Function> Load()
    {
        var nativeDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Plugins", "Native");
        var pluginNamespace = "AISmarteasy.Core.PluginFunction.NativeSkill";
        var typeNamePostfix = "Skill";
        var plugins = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace == pluginNamespace && type.Name.Contains(typeNamePostfix)) 
            .ToList();

        return LoadPlugin(plugins);
    }

    private static Dictionary<string, Function> LoadPlugin(List<Type> plugins, ILoggerFactory? loggerFactory = null)
    {
        Dictionary<string, Function> functions = new(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in plugins)
        {
            var instance = Activator.CreateInstance(plugin);
            MethodInfo[] methods = plugin.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<SKFunctionAttribute>() is not null)
                {
                    Function function = NativeFunction.FromNativeMethod(method, instance, plugin.Name, loggerFactory);
                    var functionKey = function.PluginName + "." + function.Name;
                    if (functions.ContainsKey(functionKey))
                    {
                        throw new SKException("Function overloads are not supported, please differentiate function names");
                    }

                    functions.Add(functionKey, function);
                }
            }
        }

        return functions;
    }
}


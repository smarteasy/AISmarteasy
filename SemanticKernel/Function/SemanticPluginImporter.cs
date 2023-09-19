using Microsoft.Extensions.Logging;
using SemanticKernel;
using SemanticKernel.Function;
using SemanticKernel.Prompt;

namespace SemanticKernel.Function;

//TODO - 커널이 빌드된 후, 이 역할 필요 없이 plugins 폴더 하위의 모든 플러그인 다 임포트하도록 한다. 커널 private 함수가 된다. 
public static class SemanticPluginImporter
{
    public static IDictionary<string, ISKFunction> ImportFromDirectory(
        IKernel kernel, params string[] pluginDirectoryNames)
    {
        const string ConfigFile = "config.json";
        const string PromptFile = "skprompt.txt";

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins");
        var plugin = new Dictionary<string, ISKFunction>();

        ILogger? logger = null;
        foreach (var pluginDirectoryName in pluginDirectoryNames)
        {
            Verify.ValidSkillName(pluginDirectoryName);
            var pluginDirectory = Path.Combine(pluginsDirectory, pluginDirectoryName);
            Verify.DirectoryExists(pluginDirectory);

            string[] directories = Directory.GetDirectories(pluginDirectory);
            foreach (string directory in directories)
            {
                var functionName = Path.GetFileName(directory);

                var promptPath = Path.Combine(directory, PromptFile);
                if (!File.Exists(promptPath)) { continue; }

                var config = new PromptTemplateConfig();
                var configPath = Path.Combine(directory, ConfigFile);
                if (File.Exists(configPath))
                {
                    config = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));
                }

                logger ??= kernel.LoggerFactory.CreateLogger(typeof(IKernel));
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Config {0}: {1}", functionName, config.ToJson());
                }

                var template = new PromptTemplate(File.ReadAllText(promptPath), config, kernel.PromptTemplateEngine);
                var functionConfig = new SemanticFunctionConfig(config, template);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registering function {0}.{1} loaded from {2}", pluginDirectoryName, functionName, directory);
                }

                plugin[functionName] = kernel.RegisterSemanticFunction(pluginDirectoryName, functionName, functionConfig);
            }
        }

        kernel.CreateNewContext();

        return plugin;
    }
}

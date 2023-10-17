namespace AISmarteasy.Core.Connecting.OpenAI;

public class PromptTemplateConfigBuilder
{
    public static PromptTemplateConfig Build(double temperature = 0.7, int maxTokens = 256)
    {
        var result = new PromptTemplateConfig()
        {
            Completion =
            {
                Temperature = temperature,
                MaxTokens = maxTokens

            }
        };

        return result;
    }
}

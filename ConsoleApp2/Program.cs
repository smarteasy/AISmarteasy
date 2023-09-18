using System;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Service;
using SemanticKernel;

namespace GPTConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var kernel = KernelBuilder.BuildCompletionService(AIServiceTypeKind.OpenAITextCompletion, "api key");
            var prompt = "ChatGPT?";
            var answer = await kernel.RunCompletion(prompt);
            Console.WriteLine(answer.Text);

            Console.ReadLine();
        }
    }
}
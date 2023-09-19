using System;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Service;
using SemanticKernel;
using SemanticKernel.Function;
using Azure;

namespace GPTConsole
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            await RunSemanticFunction();
            Console.ReadLine();
        }


        public static async void RunTextCompletionSimply()
        {
            var kernel = KernelBuilder.BuildCompletionService(AIServiceTypeKind.OpenAITextCompletion, "api key");
            var prompt = "ChatGPT?";
            var answer = await kernel.RunCompletion(prompt);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunSemanticFunction()
        {
            var kernel = KernelBuilder.BuildCompletionService(AIServiceTypeKind.OpenAITextCompletion, "api key");
            var plugin = SemanticPluginImporter.ImportFromDirectory(kernel, "Fun");
            var parameters = new Dictionary<string, string> { { "input", "time travel to dinosaur age" } };

            var answer = await kernel.RunSemanticFunction(kernel, plugin["Joke"], parameters);
            Console.WriteLine(answer.Text);
        }
    }
}
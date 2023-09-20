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
            await RunTextCompletionSimply();
            Console.ReadLine();
        }


        public static async Task RunTextCompletionSimply()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, "api key")
                .Build();

            var prompt = "ChatGPT?";
            var answer = await kernel.RunCompletion(prompt);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunSemanticFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, "api key")
                .Build();

            var function = kernel.Plugins.GetFunction("Fun", "Joke");
            var parameters = new Dictionary<string, string> { { "input", "time travel to dinosaur age" } };

            var answer = await kernel.RunSemanticFunction(kernel, function, parameters);
            Console.WriteLine(answer.Text);
        }
    }
}
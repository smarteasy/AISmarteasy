using System;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Service;
using SemanticKernel;
using SemanticKernel.Function;
using Azure;
using SemanticKernel.Context;

namespace GPTConsole
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            await RunNativeFunction();
            Console.ReadLine();
        }


        public static async Task RunTextCompletionSimply()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, "")
                .Build();

            var prompt = "ChatGPT?";
            var answer = await kernel.RunCompletion(prompt);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunSemanticFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, "")
                .Build();

            var function = kernel.Plugins.GetFunction("Fun", "Joke");
            var parameters = new Dictionary<string, string> { { "input", "time travel to dinosaur age" } };

            var answer = await kernel.RunFunction(kernel, function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunNativeFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, "")
                .Build();

            var loader = new NativePluginLoader(kernel);
            loader.Load();

            var function = kernel.Plugins.GetFunction("MathSkill", "Sqrt");
            var parameters = new Dictionary<string, string> { { "input", "12" } };

            var answer = await kernel.RunFunction(kernel, function, parameters);
            Console.WriteLine(answer.Text);
            
            parameters = new Dictionary<string, string>
            {
                { "first", "12.34" },
                { "second", "56.78" }
            };

            function = kernel.Plugins.GetFunction("MathSkill", "Multiply");
            answer = await kernel.RunFunction(kernel, function, parameters);
            Console.WriteLine(answer.Text);
        }
    }
}
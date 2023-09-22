using System;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Service;
using SemanticKernel;
using SemanticKernel.Function;
using Azure;
using SemanticKernel.Context;
using System.Collections.Generic;

namespace GPTConsole
{
    internal class Program
    {
        private const string API_KEY = "...";

        public static async Task Main(string[] args)
        {
            await RunOrchestratorFunction();
            Console.ReadLine();
        }


        public static async Task RunTextCompletionSimply()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, API_KEY)
                .Build();

            var prompt = "ChatGPT?";
            var answer = await kernel.RunCompletion(prompt);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunSemanticFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, API_KEY)
                .Build();

            var function = kernel.Plugins.GetFunction("Fun", "Joke");
            var parameters = new Dictionary<string, string> { { "input", "time travel to dinosaur age" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunNativeFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, API_KEY)
                .Build();

            var loader = new NativePluginLoader();
            loader.Load();

            var function = kernel.Plugins.GetFunction("MathSkill", "Sqrt");
            var parameters = new Dictionary<string, string> { { "input", "12" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
            
            parameters = new Dictionary<string, string>
            {
                { "first", "12.34" },
                { "second", "56.78" }
            };

            function = kernel.Plugins.GetFunction("MathSkill", "Multiply");
            answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunGetIntentFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, API_KEY)
                .Build();

            var function = kernel.Plugins.GetFunction("OrchestratorSkill", "GetIntent");

            var parameters = new Dictionary<string, string>
            {
                ["input"] = "Yes",
                ["history"] = @"Bot: How can I help you?
User: What's the weather like today?
Bot: Where are you located?
User: I'm in Seattle.
Bot: It's 70 degrees and sunny in Seattle today.
User: Thanks! I'll wear shorts.
Bot: You're welcome.
User: Could you remind me what I have on my calendar today?
Bot: You have a meeting with your team at 2:00 PM.
User: Oh right! My team just hit a major milestone; I should send them an email to congratulate them.
Bot: Would you like to write one for you?",
                ["options"] = "SendEmail, ReadEmail, SendMeeting, RsvpToMeeting, SendChat"
            };
            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunOrchestratorFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, API_KEY)
                .Build();

            var loader = new NativePluginLoader();
            loader.Load();

            var function = kernel.Plugins.GetFunction("OrchestratorSkill", "RouteRequest");
            var parameters = new Dictionary<string, string> { { "input", "What is the square root of 634?" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }
    }
}
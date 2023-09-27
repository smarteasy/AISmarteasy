using System.Text.Json;
using SemanticKernel.Service;
using SemanticKernel;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

namespace GPTConsole
{
    internal class Program
    {
        private const string API_KEY = "";

        public static async Task Main(string[] args)
        {
            await RunChatCompletion();
            Console.ReadLine();
        }


        public static async Task RunTextCompletion()
        {
            AIServiceConfig config = new AIServiceConfig
            {
                Service = AIServiceKind.TextCompletion,
                Vendor = AIServiceVendorKind.OpenAI,
                ServiceFeature = AIServiceFeatureKind.Normal,
                APIKey = API_KEY
            };

            var kernel = new KernelBuilder().Build(config);

            var prompt = "ChatGPT?";
            var answer = await kernel.RunCompletion(prompt);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunChatCompletion()
        {
            AIServiceConfig config = new AIServiceConfig
            {
                Service = AIServiceKind.ChatCompletion,
                Vendor = AIServiceVendorKind.OpenAI,
                ServiceFeature = AIServiceFeatureKind.Normal,
                APIKey = API_KEY
            };

            var kernel = new KernelBuilder().Build(config);

            var history = new ChatHistory();
            history.AddUserMessage("Hi, I'm looking for book suggestions");
            history = await kernel.RunChatCompletion(history);
            Console.WriteLine(history.LastContent);

            history.AddUserMessage("I would like a non-fiction book suggestion about Greece history. Please only list one book.");
            history = await kernel.RunChatCompletion(history);
            Console.WriteLine(history.LastContent);

            history.AddUserMessage("that sounds interesting, what are some of the topics I will learn about?");
            history = await kernel.RunChatCompletion(history);
            Console.WriteLine(history.LastContent);

            history.AddUserMessage("Which topic from the ones you listed do you think most people find interesting?");
            history = await kernel.RunChatCompletion(history);
            Console.WriteLine(history.LastContent);

            history.AddUserMessage("could you list some more books I could read about the topic(s) you mentioned?");
            history = await kernel.RunChatCompletion(history);
            Console.WriteLine(history.LastContent);
        }

        public static async Task RunSemanticFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceKind.TextCompletion, API_KEY)
                .Build();

            var function = kernel.Plugins.GetFunction("Fun", "Joke");
            var parameters = new Dictionary<string, string> { { "input", "time travel to dinosaur age" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunNativeFunction()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceKind.TextCompletion, API_KEY)
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
                .WithCompletionService(AIServiceKind.TextCompletion, API_KEY)
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
                .WithCompletionService(AIServiceKind.TextCompletion, API_KEY)
                .Build();

            var loader = new NativePluginLoader();
            loader.Load();

            var function = kernel.Plugins.GetFunction("OrchestratorSkill", "RouteRequest");
            var parameters = new Dictionary<string, string> { { "input", "What is the square root of 634?" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunPipeline()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceKind.TextCompletion, API_KEY)
                .Build();


            var jokeFunction = kernel.Plugins.GetFunction("TempSkill", "Joke");
            var poemFunction = kernel.Plugins.GetFunction("TempSkill", "Poem");
            var menuFunction = kernel.Plugins.GetFunction("TempSkill", "Menu");

            kernel.Context.Variables["input"] = "Charlie Brown";


            var answer = await kernel.RunPipeline(jokeFunction, poemFunction, menuFunction);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunPlanner()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceKind.ChatCompletion, API_KEY)
                .Build();

            var loader = new NativePluginLoader();
            loader.Load();


         
            var goal = "If my investment of 2130.23 dollars increased by 23%, how much would I have after I spent $5 on a latte?";
            var plan = await kernel.RunPlan(goal);


            Console.WriteLine("Plan:\n");
            Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

            Console.WriteLine("\nPlan results:");
            Console.WriteLine(JsonSerializer.Serialize(plan.State, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}




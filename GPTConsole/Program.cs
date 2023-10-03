using System.Text.Json;
using SemanticKernel.Service;
using SemanticKernel;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;
using SemanticKernel.Connector.Memory;
using SemanticKernel.Connector.Memory.Pinecone;
using SemanticKernel.Context;
using System.Globalization;
using SemanticKernel.Function;
using SemanticKernel.Prompt;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace GPTConsole
{
    internal class Program
    {
        private const string API_KEY = "";
        private const string PINECONE_ENVIRONMENT = "gcp-starter";
        private const string PINECONE_API_KEY = "";

        public static async Task Main(string[] args)
        {
            //await RunChatCompletion();
            //await RunNativeFunction();
            //await RunExample1(); 
            //await RunExample2();
            //await RunExample3();
            //await RunExample4();
            //await RunExample5();
            //await TimeSkillNow();
            //await RunExample6();
            //await RunExample10();
            //await RunPineconeIndexRelated();

            //await GoogleSearch();

            await RunExample7();
 
            Console.ReadLine();
        }

        public static Task RunExample10()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            foreach (var plugin in kernel.Plugins.Values)
            {
                foreach (var functionView in plugin.BuildPluginView().FunctionViews.Values)
                {
                    PrintFunction(functionView);
                }
            }

            return Task.CompletedTask;
        }

        private static void PrintFunction(FunctionView func)
        {
            Console.WriteLine($"   {func.Name}: {func.Description}");

            if (func.Parameters.Count > 0)
            {
                Console.WriteLine("      Params:");
                foreach (var p in func.Parameters)
                {
                    Console.WriteLine($"      - {p.Name}: {p.Description}");
                    Console.WriteLine($"        default: '{p.DefaultValue}'");
                }
            }

            Console.WriteLine();
        }

        public static async Task RunExample1()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            var function = kernel.FindFunction("TextSkill", "Uppercase");
            var parameters = new Dictionary<string, string> { { "input", "ciao" } };
            var answer = await kernel.RunFunction(function, parameters);

            Console.WriteLine(answer.Text);
        }

        public static async Task RunExample2()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            var trimStart = kernel.FindFunction("TextSkill", "TrimStart");
            var trimEnd = kernel.FindFunction("TextSkill", "TrimEnd");
            var uppercase = kernel.FindFunction("TextSkill", "Uppercase");

            kernel.Context.Variables["input"] = "    i n f i n i t e     s p a c e     ";

            var answer = await kernel.RunPipeline(trimStart, trimEnd, uppercase);
            Console.WriteLine(answer.Text);
        }

        private static async Task RunExample3()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            var function1 = kernel.FindFunction("TextSkill", "AppendDay");
            var function2 = kernel.FindFunction("TextSkill", "Uppercase");

            var variables = new ContextVariables("Today is: ");
            variables.Set("day", DateTimeOffset.Now.ToString("dddd", CultureInfo.CurrentCulture));

            kernel.Context = new SKContext(variables);
            var answer = await kernel.RunPipeline(function1, function2);
            Console.WriteLine(answer.Text);
        }

        private static async Task RunExample4()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            var function1 = kernel.FindFunction("GoogleSkill", "Search");

            var parameters1 = new Dictionary<string, string> { { "input", "What's the tallest building in South America" } };

            var result1 = await kernel.RunFunction(function1, parameters1);

            Console.WriteLine(result1.Text + "\n");


            var function2 = kernel.FindFunction("SummarizeSkill", "Summarize");
            var parameters2 = new Dictionary<string, string>();

            var result2 = await kernel.RunFunction(function2, parameters2);
            Console.WriteLine(result2.Text);
        }

        private static async Task RunExample5()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, API_KEY));

            string promptTemplate = @"
Generate a creative reason or excuse for the given event.
Be creative and be funny. Let your imagination run wild.

Event: I am running late.
Excuse: I was being held ransom by giraffe gangsters.

Event: I haven't been to the gym for a year
Excuse: I've been too busy training my pet dragon.

Event: {{$input}}
";

            string configText = @"
{
    ""schema"": 1,
    ""type"": ""completion"",
    ""description"": ""Generate a creative reason or excuse for the given even"",
    ""completion"": {
        ""max_tokens"": 1024,
        ""temperature"": 0.4,
        ""top_p"": 1
    }
}
";
            var config = PromptTemplateConfig.FromJson(configText);
            var template = new PromptTemplate(promptTemplate, config);
            var functionConfig = new SemanticFunctionConfig(config, template);

            var function = kernel.CreateSemanticFunction("EventSkill", "GenerateReasonOrExcuse", functionConfig);

            var parameters = new Dictionary<string, string> { { "input", "I missed the F1 final race" } };

            var result = await kernel.RunFunction(function, parameters);
            Console.WriteLine(result.Text);

            parameters = new Dictionary<string, string> { { "input", "sorry I forgot your birthday" } };

            result = await kernel.RunFunction(function, parameters);
            Console.WriteLine(result.Text);
        }

        private static async Task RunExample6()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            string promptTemplate = @"
Today is: {{TimeSkill.Date}}
Current time is: {{TimeSkill.Time}}

Answer to the following questions using JSON syntax, including the data used.
Is it morning, afternoon, evening, or night (morning/afternoon/evening/night)?
Is it weekend time (weekend/not weekend)?
";

            string configText = @"
{
    ""schema"": 1,
    ""type"": ""completion"",
    ""description"": ""Calling native function."",
    ""completion"": {
        ""max_tokens"": 1024,
        ""temperature"": 0.9
    }
}
";
            var config = PromptTemplateConfig.FromJson(configText);
            var template = new PromptTemplate(promptTemplate, config);
            var functionConfig = new SemanticFunctionConfig(config, template);

            var function = kernel.CreateSemanticFunction("TemplateSkill", "CallNativeFunction", functionConfig);

            var parameters = new Dictionary<string, string>();

            var result = await kernel.RunFunction(function, parameters);
            Console.WriteLine(result.Text);
        }

        public static async Task RunExample7()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            var questions = "What's the exchange rate EUR:USD?";
            Console.WriteLine(questions);

            var function = kernel.FindFunction("RAGSkill", "Search");
            var parameters = new Dictionary<string, string> { ["input"] = questions };
            var answer = await kernel.RunFunction(function, parameters);

            if (answer.Text.Contains("google.search", StringComparison.OrdinalIgnoreCase))
            {
                var searchFunction = kernel.FindFunction("GoogleSkill", "Search");
                var searchParameters = new Dictionary<string, string> { { "input", questions } };

                var searchAnswer = await kernel.RunFunction(searchFunction, searchParameters);
                string searchResult = searchAnswer.Text;

                Console.WriteLine("---- Fetching information from Google");
                Console.WriteLine(searchResult);

                //var summarizeFunction = kernel.FindFunction("SummarizeSkill", "Summarize");
                //var summarizeParameters = new Dictionary<string, string>
                //{
                //    ["input"] = searchResult
                //};
                //var searchResultSummary = await kernel.RunFunction(summarizeFunction, summarizeParameters);
                //searchResult = searchResultSummary.Text;

                parameters = new Dictionary<string, string>
                {
                    ["input"] = questions,
                    ["externalInformation"] = searchResult
                };

                answer = await kernel.RunFunction(function, parameters);
                Console.WriteLine($"Answer: {answer.Text}");
            }
            else
            {
                Console.WriteLine("AI had all the information, no need to query Google.");
                Console.WriteLine($"Answer: {answer.Text}");
            }
        }

        public static async Task RunPineconeIndexRelated()
        {
            var pinecone = new PineconeClient(PINECONE_ENVIRONMENT, PINECONE_API_KEY);

            var creatingIndexName = "smarteasy";

            await foreach(var index in pinecone.ListIndexesAsync())
            {
                var existedIndexName = index;
                if (existedIndexName == creatingIndexName)
                {
                    creatingIndexName = "test";
                }
                var describeIndex = await pinecone.DescribeIndexAsync(existedIndexName!);
                Console.WriteLine(describeIndex?.Configuration);
    
                await pinecone.DeleteIndexAsync(existedIndexName!);
            }

            var indexDefinition = new IndexDefinition(creatingIndexName);
            await pinecone.CreateIndexAsync(indexDefinition);
            Console.WriteLine($"{creatingIndexName} is created." );
        }

        public static async Task GeneratePineconeEmbeddings()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            string[] data = { "A", "B" };

            var embeddings = await kernel.AIService.GenerateEmbeddings(data);

            Console.WriteLine("GenerateEmbeddings");

            var vectors = new List<PineconeDocument>();
            foreach (var embedding in embeddings)
            {
                var vector = new PineconeDocument(embedding);
                vectors.Add(vector);
            }

            var indexName = "smarteasy";
            var pinecone = new PineconeClient(PINECONE_ENVIRONMENT, PINECONE_API_KEY);

            await pinecone.UpsertAsync(indexName, vectors);

            var describeIndexStats = await pinecone.DescribeIndexStatsAsync(indexName);
            Console.WriteLine(JsonSerializer.Serialize(describeIndexStats));
        }

        public static async Task RunTextCompletion()
        {
            AIServiceConfig config = new AIServiceConfig
            {
                ServiceType = AIServiceTypeKind.TextCompletion,
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
                ServiceType = AIServiceTypeKind.ChatCompletion,
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
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            var function = kernel.FindFunction("Fun", "Joke");
            var parameters = new Dictionary<string, string> { { "input", "time travel to dinosaur age" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunNativeFunction()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            var function = kernel.FindFunction("MathSkill", "Sqrt");
            var parameters = new Dictionary<string, string> { { "input", "12" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);

            parameters = new Dictionary<string, string>
            {
                { "input", "12.34" },
                { "number", "56.78" }
            };

            function = kernel.FindFunction("MathSkill", "Multiply");
            answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task TimeSkillNow()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            var loader = new NativePluginLoader();
            loader.Load();

            var function = kernel.FindFunction("TimeSkill", "Now");

            var parameters = new Dictionary<string, string> { };
            var answer = await kernel.RunFunction(function, parameters);

            Console.WriteLine(answer.Text);
        }

        public static async Task RunGetIntentFunction()
        {
            var kernel = new KernelBuilder()
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

            var function = kernel.FindFunction("OrchestratorSkill", "GetIntent");

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
                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY)); 

            var loader = new NativePluginLoader();
            loader.Load();

            var function = kernel.FindFunction("OrchestratorSkill", "RouteRequest");
            var parameters = new Dictionary<string, string> { { "input", "What is the square root of 634?" } };

            var answer = await kernel.RunFunction(function, parameters);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunPipeline()
        {
            var kernel = new KernelBuilder()
                    .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));


            var jokeFunction = kernel.FindFunction("TempSkill", "Joke");
            var poemFunction = kernel.FindFunction("TempSkill", "Poem");
            var menuFunction = kernel.FindFunction("TempSkill", "Menu");

            kernel.Context.Variables["input"] = "Charlie Brown";


            var answer = await kernel.RunPipeline(jokeFunction, poemFunction, menuFunction);
            Console.WriteLine(answer.Text);
        }

        public static async Task RunPlanner()
        {
            var kernel = new KernelBuilder()
                    .Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, API_KEY));

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





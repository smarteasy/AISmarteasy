using System.Globalization;
using AISmarteasy.Core;
using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Memory;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;

namespace GPTConsole
{
    internal class Program
    {
        private static readonly string OpenaiAPIKey = Env.OpenaiAPIKey;
        private static readonly string PineconeEnvironment = Env.PineconeEnvironment;
        private static readonly string PineconeAPIKey = Env.PineconeAPIKey;

        public static void Main(string[] args)
        {
            //Run_Example01_NativeFunctions();
            //Run_Example02_Pipeline();
            //Run_Example03_Variables();
            //Run_Example04_01_CombineLLMPromptsAndNativeCode();
            //Run_Example04_02_CombineLLMPromptsAndNativeCode();
            //Run_Example05_InlineFunctionDefinition();
            //Run_Example06_TemplateLanguage();
            //RunExample10_DescribeAllPluginsAndFunctions();
            //Run_Example12_SequentialPlanner();
            //Run_Example13_01_ConversationSummaryPlugin();
            //Run_Example13_02_ConversationSummaryPlugin();
            // Run_Example13_03_ConversationSummaryPlugin();
            //Run_Example14_01_SemanticMemory();
            //Run_Example14_02_SemanticMemory();
            //Run_Example17_ChatGPT();
            //Run_Example18_01_DallE();
            //Run_Example18_02_DallE();
            //Run_Example32_StreamingCompletion();
            //Run_Example33_StreamingChat();

            Run_LanguageCalculatorPlugin();

            Console.ReadLine();
        }

        public static async void RunGetIntentFunction()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
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

            var config = new FunctionRunConfig("OrchestratorSkill", "GetIntent", parameters);
            await kernel.RunFunctionAsync(config);
            Console.WriteLine(kernel.Context.Variables.Input);
        }

        public static async void Run_Example01_NativeFunctions()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
            loader.Load();

            var config = new FunctionRunConfig("TextSkill", "Uppercase");
            config.UpdateInput("ciao");
            await kernel.RunFunctionAsync(config);

            Console.WriteLine(KernelProvider.Kernel.ContextVariablesInput);
        }

        public static async void Run_Example02_Pipeline()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
            loader.Load();

            var config = new PipelineRunConfig();

            config.AddPluginFunctionName("TextSkill", "TrimStart");
            config.AddPluginFunctionName("TextSkill", "TrimEnd");
            config.AddPluginFunctionName("TextSkill", "Uppercase");

            config.UpdateInput("    i n f i n i t e     s p a c e     ");

            var answer = await kernel.RunPipelineAsync(config);
            Console.WriteLine(answer.Text);
        }

        private static async void Run_Example03_Variables()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;

            var loader = new NativeFunctionLoader();
            loader.Load();

            var variables = new ContextVariables("Today is: ");
            variables.Set("day", DateTimeOffset.Now.ToString("dddd", CultureInfo.CurrentCulture));

            kernel.Context = new SKContext(variables);

            var config = new PipelineRunConfig();
            config.AddPluginFunctionName("TextSkill", "AppendDay");
            config.AddPluginFunctionName("TextSkill", "Uppercase");

            var answer = await kernel.RunPipelineAsync(config);
            Console.WriteLine(answer.Text);
        }

        private static async void Run_Example04_01_CombineLLMPromptsAndNativeCode()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
            loader.Load();

            var config = new PipelineRunConfig();
            config.AddPluginFunctionName("GoogleSkill", "Search");
            config.AddPluginFunctionName("SummarizeSkill", "Summarize");
            config.UpdateInput("What's the tallest building in South America");

            var result2 = await kernel.RunPipelineAsync(config);
            Console.WriteLine(result2.Text);
        }

        private static async void Run_Example04_02_CombineLLMPromptsAndNativeCode()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
            loader.Load();

            var config = new PipelineRunConfig();
            config.AddPluginFunctionName("GoogleSkill", "Search");
            config.AddPluginFunctionName("SummarizeSkill", "Notegen");
            config.UpdateInput("What's the tallest building in South America");

            Console.WriteLine("======== Notegen ========");
            var result1 = await kernel.RunPipelineAsync(config);
            Console.WriteLine(result1.Text);
        }

        private static async void Run_Example05_InlineFunctionDefinition()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
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
            ""description"": ""Generate a creative reason or excuse for the given event."",
            ""completion"": {
                ""max_tokens"": 1024,
                ""temperature"": 0.4,
                ""top_p"": 1
            }
        }
        ";
            var config = PromptTemplateConfig.FromJson(configText);
            var template = new PromptTemplate(promptTemplate, config);
            var functionConfig = new SemanticFunctionConfig("EventSkill", "GenerateReasonOrExcuse", config, template);
            var inlineFunction = kernel.RegisterSemanticFunction(functionConfig);

            await kernel.RunFunctionAsync(inlineFunction, "I missed the F1 final race");
            Console.WriteLine(KernelProvider.Kernel.ContextVariablesInput);

            await kernel.RunFunctionAsync(inlineFunction, "sorry I forgot your birthday");
            Console.WriteLine(KernelProvider.Kernel.ContextVariablesInput);
        }

        private static async void Run_Example06_TemplateLanguage()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
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
            ""description"": ""Generate day and time information."",
            ""completion"": {
                ""max_tokens"": 1024,
                ""temperature"": 0.4,
                ""top_p"": 1
            }
        }
        ";
            var config = PromptTemplateConfig.FromJson(configText);
            var template = new PromptTemplate(promptTemplate, config);
            var functionConfig =
                new SemanticFunctionConfig("TimeSkill", "GenerateDayTimeInformation", config, template);
            var function = kernel?.RegisterSemanticFunction(functionConfig);

            await kernel!.RunFunctionAsync(function!, string.Empty);
            Console.WriteLine(KernelProvider.Kernel?.ContextVariablesInput);
        }

        public static Task RunExample10_DescribeAllPluginsAndFunctions()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
            loader.Load();

            foreach (var plugin in kernel!.Plugins.Values)
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

        private static async void Run_Example12_SequentialPlanner()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
            loader.Load();

            var goal = "Write a poem about John Doe, then translate it into Korean.";

            var plan = await kernel.RunPlanAsync(goal);
            Console.WriteLine("Plan:\n");
            Console.WriteLine(plan.Content);
            Console.WriteLine("Answer:\n");
            Console.WriteLine(plan.Answer);
        }

        private static async void Run_Example13_01_ConversationSummaryPlugin()
        {
            Console.WriteLine("======== SamplePlugins - Conversation Summary Plugin - Summarize ========");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var config = new FunctionRunConfig("ConversationSummarySkill", "SummarizeConversation");
            config.UpdateInput(ProviderChatTranscript.EXAMPLE13);
            await kernel!.RunFunctionAsync(config);

            Console.WriteLine("Generated Summary:");
            Console.WriteLine(kernel.Context.Variables.Input);
        }

        private static async void Run_Example13_02_ConversationSummaryPlugin()
        {
            Console.WriteLine("======== SamplePlugins - Conversation Summary Plugin - Action Items ========");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletionWithGpt35, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var config = new FunctionRunConfig("ConversationSummarySkill", "GenerateActionItems");
            config.UpdateInput(ProviderChatTranscript.EXAMPLE13);
            await kernel!.RunFunctionAsync(config);

            Console.WriteLine("Generated Action Items:");
            Console.WriteLine(kernel.Result);
        }

        private static async void Run_Example13_03_ConversationSummaryPlugin()
        {
            Console.WriteLine("======== SamplePlugins - Conversation Summary Plugin - Topics ========");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var config = new FunctionRunConfig("ConversationSummarySkill", "GenerateTopics");
            config.UpdateInput(ProviderChatTranscript.EXAMPLE13);
            await kernel!.RunFunctionAsync(config);

            Console.WriteLine("Generated Topics::");
            Console.WriteLine(kernel.Result);
        }

        private static async void Run_Example14_01_SemanticMemory()
        {
            Console.WriteLine("Adding some GitHub file URLs and their descriptions to the semantic memory.");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey, ImageGenerationTypeKind.None,
                    MemoryTypeKind.PineCone, PineconeAPIKey, PineconeEnvironment));
            var kernel = KernelProvider.Kernel;
            var githubFiles = SampleData();
            await kernel!.SaveMemoryAsync(githubFiles);
        }

        private static Dictionary<string, string> SampleData()
        {
            return new Dictionary<string, string>
            {
                ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
                    = "README: Installation, getting started, and how to contribute",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
                    = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks//00-getting-started.ipynb"]
                    = "Jupyter notebook describing how to get started with the Semantic Kernel",
                ["https://github.com/microsoft/semantic-kernel/tree/main/samples/plugins/ChatPlugin/ChatGPT"]
                    = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel/Memory/VolatileMemoryStore.cs"]
                    = "C# class that defines a volatile embedding store",
                ["https://github.com/microsoft/semantic-kernel/blob/main/samples/dotnet/KernelHttpServer/README.md"]
                    = "README: How to set up a Semantic Kernel Service API using Azure Function Runtime v4",
                ["https://github.com/microsoft/semantic-kernel/blob/main/samples/apps/chat-summary-webapp-react/README.md"]
                    = "README: README associated with a sample chat summary react-based webapp",
            };
        }

        private static async void Run_Example14_02_SemanticMemory()
        {
            var query = "Can I build a chat with SK?";

            Console.WriteLine("\nQuery: " + query + "\n");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey, ImageGenerationTypeKind.None,
                    MemoryTypeKind.PineCone, PineconeAPIKey, PineconeEnvironment));
            var kernel = KernelProvider.Kernel;
            var memoryResults = await kernel!.SearchMemoryAsync(query);

            if (memoryResults != null)
            {
                var i = 0;
                await foreach (MemoryQueryResult memoryResult in memoryResults)
                {
                    Console.WriteLine($"Result {++i}:");
                    Console.WriteLine("  URL:     : " + memoryResult.Metadata.Id);
                    Console.WriteLine("  Title    : " + memoryResult.Metadata.Text);
                    Console.WriteLine("  Relevance: " + memoryResult.Relevance);
                    Console.WriteLine();
                }
            }
        }

        private static async void Run_Example17_ChatGPT()
        {
            Console.WriteLine("======== Open AI - ChatGPT ========");

             KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
             var kernel = KernelProvider.Kernel;
            Console.WriteLine("Chat content:");
            Console.WriteLine("------------------------");

            var systemMessage = "You are a librarian, expert about books";
            var chatHistory = await kernel!.RunChatCompletionAsync(systemMessage);
            Console.WriteLine(systemMessage);
            await MessageOutputAsync(chatHistory);
            Console.WriteLine("------------------------");

            var userMessage = "Hi, I'm looking for book suggestions";
            Console.WriteLine(userMessage);
            chatHistory.AddUserMessage(userMessage);
            chatHistory = await kernel.RunChatCompletionAsync(chatHistory);
            await MessageOutputAsync(chatHistory);
            Console.WriteLine("------------------------");

            userMessage = "I love history and philosophy, I'd like to learn something new about Greece, any suggestion";
            Console.WriteLine(userMessage);
            chatHistory.AddUserMessage(userMessage);
            chatHistory = await kernel.RunChatCompletionAsync(chatHistory);
            await MessageOutputAsync(chatHistory);
        }

        private static Task MessageOutputAsync(ChatHistory chatHistory)
        {
            var message = chatHistory.Messages.Last();

            Console.WriteLine($"{message.Role}: {message.Content}");
            Console.WriteLine("------------------------");

            return Task.CompletedTask;
        }

        private static async void Run_Example18_01_DallE()
        {
            Console.WriteLine("======== OpenAI Dall-E 2 Image Generation ========");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey, ImageGenerationTypeKind.DallE));
            var kernel = KernelProvider.Kernel;
            var imageDescription = "A cute baby sea otter";
            var image = await kernel!.RunImageGenerationAsync(imageDescription, 256, 256);

            Console.WriteLine(imageDescription);
            Console.WriteLine("Image URL: " + image);
        }

        private static async void Run_Example18_02_DallE()
        {
            Console.WriteLine("======== Chat with images ========");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey, ImageGenerationTypeKind.DallE));
            var kernel = KernelProvider.Kernel;

            var systemMessage = @"You're chatting with a user. Instead of replying directly to the user
 provide the description of an image that expresses what you want to say.
 The user won't see your message, they will see only the image. The system
 generates an image using your description, so it's important you describe the image with details.";

            var chatHistory = await kernel!.RunChatCompletionAsync(systemMessage);

            var msg = "Hi, I'm from Tokyo, where are you from?";
            chatHistory.AddUserMessage(msg);
            Console.WriteLine("User: " + msg);

            chatHistory = await kernel.RunChatCompletionAsync(chatHistory);
            await MessageOutputAsync(chatHistory);

            var botMessageBase = chatHistory.Messages.Last();
            var image = await kernel.RunImageGenerationAsync(botMessageBase.Content, 1024, 1024);
            Console.WriteLine("Bot: " + image);
            Console.WriteLine("Img description: " + botMessageBase.Content);

            msg = "Oh, wow. Not sure where that is, could you provide more details?";
            chatHistory.AddUserMessage(msg);
            Console.WriteLine("User: " + msg);

            chatHistory = await kernel.RunChatCompletionAsync(chatHistory);
            await MessageOutputAsync(chatHistory);
        }
        
        public static async void Run_Example32_StreamingCompletion()
        {
            Console.WriteLine("======== Azure OpenAI - Text Completion - Raw Streaming ========");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var prompt = "Write one paragraph why AI is awesome";

            Console.WriteLine("Prompt: " + prompt);
            await foreach (var answer in kernel!.RunTextStreamingCompletionAsync(prompt))
            {
                await foreach (var stream in answer.GetCompletionStreamingAsync())
                {
                    Console.Write(stream);
                }
            }

            Console.WriteLine();
        }

        public static async void Run_Example33_StreamingChat()
        {
            Console.WriteLine("======== Open AI - ChatGPT Streaming ========");

            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            Console.WriteLine("Chat content:");
            Console.WriteLine("------------------------");

            var systemMessage = "You are a librarian, expert about books";
            var chatHistory = await kernel!.RunChatCompletionAsync(systemMessage);
            await MessageOutputAsync(chatHistory);

            chatHistory.AddUserMessage("Hi, I'm looking for book suggestions");
            await MessageOutputAsync(chatHistory);

            Console.Write($"{AuthorRole.Assistant}: ");

            string fullMessage = string.Empty;

            await foreach (var answer in kernel.RunChatStreamingAsync(chatHistory))
            {
                await foreach (var stream in answer.GetStreamingChatMessageAsync())
                {
                    Console.Write(stream.Content);
                }
            }
        }

        public static async void Run_LanguageCalculatorPlugin()
        {
            KernelBuilder.Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, OpenaiAPIKey));
            var kernel = KernelProvider.Kernel;
            var loader = new NativeFunctionLoader();
            loader.Load();

            var config = new PipelineRunConfig();

            config.AddPluginFunctionName("LanguageMathProblemSkill", "TranslateMathProblem");
            config.AddPluginFunctionName("LanguageCalculatorSkill", "Calculate");

            config.UpdateInput("what is the square root of 625?");

            var answer = await kernel!.RunPipelineAsync(config);
            Console.WriteLine(answer.Text);
        }


        //public static async Task RunPdf()
        //{
        //    var kernel = new KernelBuilder()
        //        .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY,
        //            MemoryTypeKind.PineCone, PINECONE_API_KEY, PINECONE_ENVIRONMENT));

        //    var directory = Directory.GetCurrentDirectory();
        //    var paragraphs = await kernel.SaveEmbeddingsFromDirectoryPdfFiles(directory);

        //    foreach (var paragraph in paragraphs)
        //    {
        //        Console.WriteLine(paragraph);
        //    }
        //}

        //public static async Task RunExample7()
        //{
        //    var kernel = new KernelBuilder()
        //        .Build(new AIServiceConfig(AIServiceTypeKind.ChatCompletion, API_KEY));

        //    var loader = new NativePluginLoader();
        //    loader.Load();

        //    var questions = "What's the exchange rate EUR:USD?";
        //    Console.WriteLine(questions);

        //    var function = kernel.FindFunction("RAGSkill", "Search");
        //    var parameters = new Dictionary<string, string> { ["input"] = questions };
        //    var answer = await kernel.RunFunction(function, parameters);

        //    if (answer.Text.Contains("google.search", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var searchFunction = kernel.FindFunction("GoogleSkill", "Search");
        //        var searchParameters = new Dictionary<string, string> { { "input", questions } };

        //        var searchAnswer = await kernel.RunFunction(searchFunction, searchParameters);
        //        string searchResult = searchAnswer.Text;

        //        Console.WriteLine("---- Fetching information from Google");
        //        Console.WriteLine(searchResult);

        //        parameters = new Dictionary<string, string>
        //        {
        //            ["input"] = questions,
        //            ["externalInformation"] = searchResult
        //        };

        //        answer = await kernel.RunFunction(function, parameters);
        //        Console.WriteLine($"Answer: {answer.Text}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("AI had all the information, no need to query Google.");
        //        Console.WriteLine($"Answer: {answer.Text}");
        //    }
        //}

        //        public static async Task RunPineconeIndexRelated()
        //        {
        //            var pinecone = new PineconeClient(PINECONE_ENVIRONMENT, PINECONE_API_KEY);

        //            var creatingIndexName = "smarteasy";

        //            await foreach(var index in pinecone.ListIndexesAsync())
        //            {
        //                var existedIndexName = index;
        //                if (existedIndexName == creatingIndexName)
        //                {
        //                    creatingIndexName = "test";
        //                }
        //                var describeIndex = await pinecone.DescribeIndexAsync(existedIndexName!);
        //                Console.WriteLine(describeIndex?.Configuration);

        //                await pinecone.DeleteIndexAsync(existedIndexName!);
        //            }

        //            var indexDefinition = new IndexDefinition(creatingIndexName);
        //            await pinecone.CreateIndexAsync(indexDefinition);
        //            Console.WriteLine($"{creatingIndexName} is created." );
        //        }

        //        public static async Task GeneratePineconeEmbeddings()
        //        {
        //            var kernel = new KernelBuilder()
        //                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY, 
        //                    MemoryTypeKind.PineCone, PINECONE_API_KEY, PINECONE_ENVIRONMENT));

        //            string[] data = { "A", "B" };

        //            var embeddings = await kernel.AIService.GenerateEmbeddingsAsync(data);

        //            Console.WriteLine("GenerateEmbeddings");

        //            var vectors = new List<PineconeDocument>();
        //            foreach (var embedding in embeddings)
        //            {
        //                var vector = new PineconeDocument(embedding);
        //                vectors.Add(vector);
        //            }

        //            var indexName = "smarteasy";
        //            var pinecone = new PineconeClient(PINECONE_ENVIRONMENT, PINECONE_API_KEY);

        //            await pinecone.UpsertAsync(indexName, vectors);

        //            var describeIndexStats = await pinecone.DescribeIndexStatsAsync(indexName);
        //            Console.WriteLine(JsonSerializer.Serialize(describeIndexStats));
        //        }

        //        public static async Task RunChatCompletion()
        //        {
        //            AIServiceConfig config = new AIServiceConfig
        //            {
        //                ServiceType = AIServiceTypeKind.ChatCompletion,
        //                Vendor = AIServiceVendorKind.OpenAI,
        //                ServiceFeature = AIServiceFeatureKind.Normal,
        //                APIKey = API_KEY
        //            };

        //            var kernel = new KernelBuilder().Build(config);

        //            var history = new ChatHistory();
        //            history.AddUserMessage("Hi, I'm looking for book suggestions");
        //            history = await kernel.RunChatCompletion(history);
        //            Console.WriteLine(history.LastContent);

        //            history.AddUserMessage("I would like a non-fiction book suggestion about Greece history. Please only list one book.");
        //            history = await kernel.RunChatCompletion(history);
        //            Console.WriteLine(history.LastContent);

        //            history.AddUserMessage("that sounds interesting, what are some of the topics I will learn about?");
        //            history = await kernel.RunChatCompletion(history);
        //            Console.WriteLine(history.LastContent);

        //            history.AddUserMessage("Which topic from the ones you listed do you think most people find interesting?");
        //            history = await kernel.RunChatCompletion(history);
        //            Console.WriteLine(history.LastContent);

        //            history.AddUserMessage("could you list some more books I could read about the topic(s) you mentioned?");
        //            history = await kernel.RunChatCompletion(history);
        //            Console.WriteLine(history.LastContent);
        //        }

        //        public static async Task RunSemanticFunction()
        //        {
        //            var kernel = new KernelBuilder()
        //                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

        //            var function = kernel.FindFunction("Fun", "Joke");
        //            var parameters = new Dictionary<string, string> { { "input", "time travel to dinosaur age" } };

        //            var answer = await kernel.RunFunction(function, parameters);
        //            Console.WriteLine(answer.Text);
        //        }

        //        public static async Task RunNativeFunction()
        //        {
        //            var kernel = new KernelBuilder()
        //                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

        //            var loader = new NativePluginLoader();
        //            loader.Load();

        //            var function = kernel.FindFunction("MathSkill", "Sqrt");
        //            var parameters = new Dictionary<string, string> { { "input", "12" } };

        //            var answer = await kernel.RunFunction(function, parameters);
        //            Console.WriteLine(answer.Text);

        //            parameters = new Dictionary<string, string>
        //            {
        //                { "input", "12.34" },
        //                { "number", "56.78" }
        //            };

        //            function = kernel.FindFunction("MathSkill", "Multiply");
        //            answer = await kernel.RunFunction(function, parameters);
        //            Console.WriteLine(answer.Text);
        //        }

        //        public static async Task TimeSkillNow()
        //        {
        //            var kernel = new KernelBuilder()
        //                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));

        //            var loader = new NativePluginLoader();
        //            loader.Load();

        //            var function = kernel.FindFunction("TimeSkill", "Now");

        //            var parameters = new Dictionary<string, string> { };
        //            var answer = await kernel.RunFunction(function, parameters);

        //            Console.WriteLine(answer.Text);
        //        }



        //        public static async Task RunOrchestratorFunction()
        //        {
        //            var kernel = new KernelBuilder()
        //                .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY)); 

        //            var loader = new NativePluginLoader();
        //            loader.Load();

        //            var function = kernel.FindFunction("OrchestratorSkill", "RouteRequest");
        //            var parameters = new Dictionary<string, string> { { "input", "What is the square root of 634?" } };

        //            var answer = await kernel.RunFunction(function, parameters);
        //            Console.WriteLine(answer.Text);
        //        }

        //        public static async Task RunPipeline()
        //        {
        //            var kernel = new KernelBuilder()
        //                    .Build(new AIServiceConfig(AIServiceTypeKind.TextCompletion, API_KEY));


        //            var jokeFunction = kernel.FindFunction("TempSkill", "Joke");
        //            var poemFunction = kernel.FindFunction("TempSkill", "Poem");
        //            var menuFunction = kernel.FindFunction("TempSkill", "Menu");

        //            kernel.Context.Variables["input"] = "Charlie Brown";


        //            var answer = await kernel.RunPipeline(jokeFunction, poemFunction, menuFunction);
        //            Console.WriteLine(answer.Text);
        //        }
    }
}





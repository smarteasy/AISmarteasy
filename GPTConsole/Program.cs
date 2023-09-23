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
        private const string API_KEY = "";

        public static async Task Main(string[] args)
        {
            await RunPipeline();
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

        public static async Task RunPipeline()
        {
            var kernel = new KernelBuilder()
                .WithCompletionService(AIServiceTypeKind.OpenAITextCompletion, API_KEY)
                .Build();


            var jokeFunction = kernel.Plugins.GetFunction("TempSkill", "Joke");
            var poemFunction = kernel.Plugins.GetFunction("TempSkill", "Poem");
            var menuFunction = kernel.Plugins.GetFunction("TempSkill", "Menu");
 
            kernel.Context.Variables["input"] = "Charlie Brown";


            var answer = await kernel.RunPipeline(jokeFunction, poemFunction, menuFunction);
            Console.WriteLine(answer.Text);
        }
    }
}




//const string ThePromptTemplate = $@"
//오늘 날짜와 시간을 물어보려면, timeskill을 사용해.

//Today is: {{time.Date}}
//Current time is: {{time.Time}}

//Answer to the following questions using JSON syntax, including the data used.
//Is it morning, afternoon, evening, or night (morning/afternoon/evening/night)?
//Is it weekend time (weekend/not weekend)?";


//var pipeline = new Pipeline(kernel);
//var functions = new List<NativeFunctionInfo>
//{
//    new NativeFunctionInfo("MathPlugin", "Add")
//};
//pipeline.AddNativeFunctions(functions.ToArray());
//var myOutput = await pipeline.RunAsync("3 plus 5?");
//Console.WriteLine(myOutput);




//var plugin = new ChatPluigin(kernel, @"C:\Users\kimhn\source\repos\Solution1\ConsoleApp2\Plugins");
//var result = await plugin.Chat("I would like a non-fiction book suggestion about Greece history. Please only list one book.");
//Console.WriteLine(result);
//result = await plugin.Chat("that sounds interesting, what are some of the topics I will learn about?");
//Console.WriteLine(result);
//result = await plugin.Chat("Which topic from the ones you listed do you think most people find interesting?");
//Console.WriteLine(result);
//result = await plugin.Chat("could you list some more books I could read about the topic(s) you mentioned?");
//Console.WriteLine(result);

//var semanticPluigin = new SemanticPluigin(kernel, @"C:\Users\kimhn\source\repos\Solution1\ConsoleApp2\Plugins");


//semanticPluigin.Import("FunPlugin");

//var result = await semanticPluigin.RunFunction("Joke", "time travel to dinosaur age");
//Console.WriteLine(result);



//IKernel kernel = KernelBuilderEx.BuildOpenAICompletionService(AIServiceKind.OpenAIChatCompletion, apiKey);
// Return the result to the Notebook

////// Import the Math Plugin
//kernel.ImportSkill(new Plugins.MathPlugin.Math(), "MathPlugin");

////// Create planner
//var planner = new SequentialPlanner(kernel);

////// Create a plan for the ask
////var ask = "If my investment of 2130.23 dollars increased by 23%, how much would I have after I spent $5 on a latte?";
//var ask = "3 plus 5?";
//var plan = await PlanBuilder.Build(kernel, PlanTypeKind.Sequential, ask);

////// Execute the plan

//Console.WriteLine("Plan:\n");
//Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

//var result = (await kernel.RunAsync(plan)).Result;

//Console.WriteLine("Plan results:");
//Console.WriteLine(result.Trim());

//var pluginsDirectory = Path.Combine(@"C:\Users\kimhn\source\repos\Solution1\ConsoleApp2", "plugins");

// Import the semantic functions
//kernel.ImportSemanticSkillFromDirectory(pluginsDirectory, "OrchestratorPlugin");




// Make a request that runs the Sqrt function
//var result = await kernel.RunAsync("12", mathPlugin["Sqrt"]);



// ... instantiate your kernel

// Add the math plugin
//var mathPlugin = kernel.ImportSkill(new MathPlugin(), "MathPlugin");



// Make a request that runs the Sqrt function
//var result1 = await kernel.RunAsync("What is the square root of 634?", orchestratorPlugin["RouteRequest"]);
//Console.WriteLine(result1);

// Make a request that runs the Multiply function
//var result2 = await kernel.RunAsync("What is 12.34 times 56.78?", orchestratorPlugin["RouteRequest"]);
//Console.WriteLine(result2);

//var contextVariables = new ContextVariables
//{
//    ["first"] = "12.34",
//    ["second"] = "56.78"
//};

//// Make a request that runs the Multiply function
//var result = await kernel.RunAsync(contextVariables, mathPlugin["Multiply"]);
//Console.WriteLine(result);

//            var pluginsDirectory = Path.Combine(@"C:\Users\kimhn\source\repos\Solution1\ConsoleApp2", "Plugins");

//            // Import the OrchestratorPlugin from the plugins directory.
//            var orchestratorPlugin = kernel.ImportSemanticSkillFromDirectory(pluginsDirectory, "OrchestratorPlugin");

//            var conversationSummaryPlugin = kernel.ImportSkill(new ConversationSummarySkill(kernel), "ConversationSummarySkill");

//            // Create a new context and set the input, history, and options variables.
//            var variables = new ContextVariables
//            {
//                ["input"] = "Yes",
//                ["history"] = @"Bot: How can I help you?
//User: What's the weather like today?
//Bot: Where are you located?
//User: I'm in Seattle.
//Bot: It's 70 degrees and sunny in Seattle today.
//User: Thanks! I'll wear shorts.
//Bot: You're welcome.
//User: Could you remind me what I have on my calendar today?
//Bot: You have a meeting with your team at 2:00 PM.
//User: Oh right! My team just hit a major milestone; I should send them an email to congratulate them.
//Bot: Would you like to write one for you?",
//                ["options"] = "SendEmail, ReadEmail, SendMeeting, RsvpToMeeting, SendChat"
//            };


//            kernel.ImportSkill(new ConversationSummarySkill(kernel), "ConversationSummarySkill");

//var result = (await kernel.RunAsync(variables, orchestratorPlugin["GetIntent"]));
//Console.WriteLine(result.Result);
//Console.WriteLine(result);
//User: {{$input}}

//---------------------------------------------

//The intent of the user in 5 words or less: ";

//            var promptConfig = new PromptTemplateConfig
//            {
//                Schema = 1,
//                Type = "completion",
//                Description = "Gets the intent of the user.",
//                Completion =
//                {
//                    MaxTokens = 500,
//                    Temperature = 0.0,
//                    TopP = 0.0,
//                    PresencePenalty = 0.0,
//                    FrequencyPenalty = 0.0
//                },
//                Input =
//                {
//                    Parameters = new List<InputParameter>
//                    {
//                        new PromptTemplateConfig.InputParameter
//                        {
//                            Name = "input",
//                            Description = "The user's request.",
//                            DefaultValue = ""
//                        }
//                    }
//                }
//            };

//            var model = "text-davinci-003";
//            var apiKey = "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";

//            IKernel kernel = new KernelBuilder()
//                .WithOpenAITextCompletionService(model, apiKey)
//                .Build();

//            // Create the SemanticFunctionConfig object
//            var promptTemplate = new PromptTemplate(
//                prompt,
//                promptConfig,
//                kernel
//            );
//            var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);

//            // Register the GetIntent function with the Kernel
//            var getIntentFunction = kernel.RegisterSemanticFunction("OrchestratorPlugin", "GetIntent", functionConfig);

//            var result = await kernel.RunAsync(
//                "I want to send an email to the marketing team celebrating their recent milestone.",
//                getIntentFunction
//            );

//            Console.WriteLine(result);

// Configure AI backend used by the kernel
//var (useAzureOpenAI, model, azureEndpoint, apiKey, orgId) = Settings.LoadFromFile();
//if (useAzureOpenAI)
//    builder.WithAzureChatCompletionService(model, azureEndpoint, apiKey);
//else

//            var model = "gpt-4"; 
//            var apiKey = "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";
//            builder.WithOpenAIChatCompletionService(model, apiKey);

//            IKernel kernel = builder.Build();


//            const string skPrompt = @"
//ChatBot can have a conversation with you about any topic.
//It can give explicit instructions or say 'I don't know' if it does not have an answer.

//{{$history}}
//User: {{$userInput}}
//ChatBot:";

//            var promptConfig = new PromptTemplateConfig
//            {
//                Completion =
//                {
//                    MaxTokens = 2000,
//                    Temperature = 0.7,
//                    TopP = 0.5,
//                }
//            };

//            var promptTemplate = new PromptTemplate(skPrompt, promptConfig, kernel);
//            var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);
//            var chatFunction = kernel.RegisterSemanticFunction("ChatBot", "Chat", functionConfig);

//            var context = kernel.CreateNewContext();

//            var history = "";
//            context.Variables["history"] = history;

//            Func<string, Task> Chat = async (string input) =>
//            {
//                // Save new message in the context variables
//                context.Variables["userInput"] = input;

//                // Process the user message and get an answer
//                var answer = await chatFunction.InvokeAsync(context);

//                // Append the new interaction to the chat history
//                history += $"\nUser: {input}\nMelody: {answer}\n";
//                context.Variables["history"] = history;

//                // Show the response
//                Console.WriteLine(context);
//            };

//            await Chat("I would like a non-fiction book suggestion about Greece history. Please only list one book.");
//            await Chat("that sounds interesting, what are some of the topics I will learn about?");
//            await Chat("Which topic from the ones you listed do you think most people find interesting?");
//            await Chat("could you list some more books I could read about the topic(s) you mentioned?");
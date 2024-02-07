using HandlebarsDotNet;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using MicrosoftPlannerPlugin;

namespace PlannerPluginDemo;

public class PlannerPluginCheck
{
    static string MicrosoftPlannerPlanId = "nRjAlPs9B0OAxFnFDgb5O2UAFw2T";

    public async Task ExecuteAsync()
    {
        Console.WriteLine("Test the planner!!");
        var modelDeploymentName = "Gpt4v32k"; // "gpt4"; - was 0314 (no function calling)
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI_Endpoint", EnvironmentVariableTarget.User);
        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AzureOpenAI_ApiKey", EnvironmentVariableTarget.User);

        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            modelDeploymentName,
            azureOpenAIEndpoint,
            azureOpenAIApiKey,
            modelId: "gpt-4-32k"
        );
        var kernel = builder.Build();

        // Creating the graph Plugin
        string msGraphTenantId = Environment.GetEnvironmentVariable("msGraphTenantId", EnvironmentVariableTarget.User); 
        string msGraphClientId = Environment.GetEnvironmentVariable("msGraphClientId", EnvironmentVariableTarget.User);  // app id
        string msGraphClientSecret = Environment.GetEnvironmentVariable("msGraphClientSecret", EnvironmentVariableTarget.User);  

        // Nope, type does not work here: kernel.Plugins.AddFromType<msGraphPlannerPlugin>();
        // CUstom plugin for web search test
        string microsoftPlannerGraphPluginName = "MicrosoftPlannerGraphPlugin";
        var msGraphPlannerPlugin = new MicrosoftPlannerGraphPlugin(msGraphTenantId, msGraphClientId, msGraphClientSecret);
        kernel.ImportPluginFromObject(msGraphPlannerPlugin, microsoftPlannerGraphPluginName);

        // And we try it
        string userPromptGenerateTasks = @"I want YOU to plan the tasks needed to accomplish the following goal and tasks to achieve it:" +
            "---" +
            "{{ $input }}" +
            "---" +
            "For this goal and approximate tasks, can you prepare:" +
            " 1. A list of tasks in order how they should be done? each task should be short text with a number in front showcasing its order." +
            " 2. A name to group all the tasks which is descriptive, clear and short." +
            "With this prepare a JSON with should contain this information, containing:" +
            " 1. a JSON element, named/with key bucketname that contains the name for grouping the tasks. " +
            " 2. a JSON array, named/with key tasklist that contains an array with all the tasks. Each task should have the key task." +
            "Ensure the JSON is valid and well formed." +
            "Ensure ONLY THE JSON IS OUTPUTTED." +
            "Do not output anything else but the JSON.";

        string inputTask = "The task is the following:" +
            "Plan an announcement of the technical company events for this year 2024, for this I want to annotate" +
            "the ideas, brainstorm. Then sync with my team lead and next with the architecture and cloud departments. Afterwards I would " +
            "like to present it to the CTO and get approval - and budget! Also after all is ok-ed I want to announce it in the wiki, Teams " +
            "and some events just to make our people aware.";

        var jsonWithTasks = await kernel.InvokePromptAsync(
            userPromptGenerateTasks, 
                new KernelArguments() { { "input", inputTask } });
        Console.WriteLine($"The elaborated tasks: {jsonWithTasks}");

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////
        /// this is the function calling part which is trying to get the model to execute the function calling part
        /// does not work or does weird things - hallucinates and creates code or something else -but not what we want
        //string userPrompForFunctionCalling = "You are a Microsoft Planner expert,your task is to process a set of tasks provided in JSON format." +
        //    "This JSON include the bucket name and a task list, Here is the JSON to process:" +
        //    "---" +
        //    "{{ $JsonWithBucketAndTasks }}" +
        //    "---" +
        //    "I want you to do the following: " +
        //    "1. First and foremost, create a bucket in Microsoft Planner for a given Microsoft Planner Plan id: {{ $MicrosoftPlannerPlanId }} " +
        //    "   and with the 'bucketname' in the JSON. Please use the MicrosoftPlannerGraphPlugin for this and the CreateBucketAsync function." +
        //    "2. Second, retrieve the PlannerBucket returned, as we will need its PlannerBucket id on the next steps." +
        //    "3. For each task provided in the JSON I would like to create the tasks in Microsoft Planner using the MicrosoftPlannerGraphPlugin " +
        //    "   and the CreateTaskAsync function. For this you need to provide it the Plan Id and the PlannerBucket id." +
        //    "" +
        //    "Please try to process the JSON and invoke the plugin/tools stated directly as you should have access to them." +
        //    "If something does not work, please remember and state it at the end with all the details.";

        //var functionCallingFunction = kernel.CreateFunctionFromPrompt(
        //    userPrompForFunctionCalling,
        //    new OpenAIPromptExecutionSettings()
        //    {
        //        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        //        MaxTokens = 650,
        //        Temperature = 0,
        //        TopP = 1
        //    });

        //var response =
        //    await kernel.InvokeAsync(
        //        functionCallingFunction,
        //        new KernelArguments()
        //        {
        //            { "$JsonWithBucketAndTasks", jsonWithTasks },
        //            { "$MicrosoftPlannerPlanId", "nRjAlPs9B0OAxFnFDgb5O2UAFw2T" }
        //        });

        //Console.WriteLine($"Result: {response}");
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// this is the handlebars plan part which is trying to get the model to generate a handlebars plan
        /// The handlebars CreatePlanAsync does not accept arguments so we put them into the prompt and call the function

        string userPromptCreateInPlanner = "You are a Microsoft Planner expert,your task is to process a set of tasks provided in JSON format." +
            "This JSON include the bucket name and a task list, Here is an example of the JSON to process:" +
            "---" +
            "{\r\n  \"bucketname\": \"2024 Technical Events Announcement Plan\",\r\n  \"tasklist\": [\r\n    {\r\n      \"task\": \"1. Annotate initial ideas for the announcement of the technical events\"\r\n    },\r\n    {\r\n      \"task\": \"2. Brainstorm additional ideas and refine initial thoughts\"\r\n    }\r\n  ]\r\n}" +
            "---" +
            "I want you to do the following: " +
            "1. First and foremost, create a bucket in Microsoft Planner for a given Microsoft Planner Plan id: " + MicrosoftPlannerPlanId +
            "   and with the 'bucketname' in the JSON. Please use the MicrosoftPlannerGraphPlugin for this and the CreateBucketAsync function." +
            "2. Second, retrieve the PlannerBucket returned, as we will need its PlannerBucket id on the next steps." +
            "3. For each task provided in the JSON I would like to create the tasks in Microsoft Planner using the MicrosoftPlannerGraphPlugin " +
            "   and the CreateTaskAsync function. For this you need to provide it the Plan Id and the PlannerBucket id." +
            "" +
            "Please generate me an amazing handlebarsplan including calling the functions mentioned to create the Microsoft Planner Bucket and tasks.";

        var planner = new HandlebarsPlanner(
            new HandlebarsPlannerOptions() { AllowLoops = true });


        var plan = await planner.CreatePlanAsync(kernel, userPromptCreateInPlanner);

        Console.WriteLine($"Plan: {plan}");

        //var result = await plan.InvokeAsync(kernel);
        //Console.WriteLine($"\nResult:\n{result}\n");

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //// After 3-6 times it generated a suitable plan to adapt, shown next:
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Plan: {{!-- Step 1: Read the input JSON and set the Plan Id --}}
        //{{set "planId" "nRjAlPs9B0OAxFnFDgb5O2UAFw2T"}}
        //{{set "inputJson" "YOUR INPUT JSON HERE"}}
        //{{set "bucketName" inputJson.bucketname}}
        //{{set "taskList" inputJson.tasklist}}

        //{{!-- Step 2: Call the CreateBucket helper and get the new bucket --}}
        //{{set "newBucket" (MicrosoftPlannerGraphPlugin-CreateBucket planId=bucketName)}}
        //{{set "newBucketId" newBucket.id}}

        //{{!-- Step 3: Loop through the tasks in the taskList --}}
        //{{#each taskList as |task|}}
        //  {{!-- Step 4: Call the CreateTask helper for each task in the list --}}
        //  {{set "newTask" (MicrosoftPlannerGraphPlugin-CreateTask planId=planId bucketId=newBucketId taskTitle=task.task)}}
        //  {{json newTask}}
        //{{/each}}        


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Next is the Handlebars prompt template execution, based on the previous plan generate (a valid one)
        /// 

        var handlebarsTemplate =
          @"{{!-- Step 1: Read the input JSON and set the Plan Id --}}
            {{set ""planId"" MicrosoftPlannerPlanId}}
            {{set ""inputJson"" JsonWithBucketAndTasks}}
            {{set ""bucketName"" inputJson.bucketname}}
            {{set ""taskList"" inputJson.tasklist}}

            {{!-- Step 2: Call the CreateBucket helper and get the new bucket --}}
            {{set ""newBucket"" (MicrosoftPlannerGraphPlugin-CreateBucket planId=bucketName)}}
            {{set ""newBucketId"" newBucket.id}}

            {{!-- Step 3: Loop through the tasks in the taskList --}}
            {{#each taskList}}
              {{!-- Step 4: Call the CreateTask helper for each task in the list --}}
              {{set ""newTask"" (MicrosoftPlannerGraphPlugin-CreateTask planId=planId bucketId=newBucketId taskTitle=this.task)}}
              {{json newTask}}
            {{/each}}";

        var HandlebarsSPromptFunction = kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = handlebarsTemplate,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        // Invoke prompt
        var customHandlebarsPromptResult = 
            await kernel.InvokeAsync(
                HandlebarsSPromptFunction,
                new() {
                    { "JsonWithBucketAndTasks", jsonWithTasks },
                    { "MicrosoftPlannerPlanId", MicrosoftPlannerPlanId }
                }
            );

        Console.WriteLine($"Result:  {customHandlebarsPromptResult}");

        Console.WriteLine();

    }
}

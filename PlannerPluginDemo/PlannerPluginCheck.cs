using HandlebarsDotNet;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using MicrosoftPlannerPlugin;
using System.Text.Json;

namespace PlannerPluginDemo;

public class PlannerPluginCheck
{
    static string MicrosoftPlannerPlanId = "nRjAlPs9B0OAxFnFDgb5O2UAFw2T";

    public async Task ExecuteAsync()
    {
        Console.WriteLine("Test the planner!!");
        var modelDeploymentName = "Gpt4v32k"; 
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

        /// Ask the user for input or use the default one
        Console.WriteLine($"The default goal to generate tasks is: {inputTask}");
        Console.WriteLine($"\n \n Would you like to have a diferent goal?\n");
        Console.WriteLine($"If is ok, press enter. Otherwise write the goal for which you'd like to generate a set of tasks in Planner \n");

        var newGoal = Console.ReadLine();
        if (!string.IsNullOrEmpty(newGoal))
        {
            inputTask = newGoal;
        }

        Console.WriteLine($"\n --- \n And that's it, now I will be generating the tasks for this glorious goal you have set me to do, thank you user!!!" +
            $"\n The final goal to generate tasks is: {inputTask}");
        Console.WriteLine($"\n ==========================================\n Generating Tasks \n ==========================================\n ");

        var jsonWithTasks = await kernel.InvokePromptAsync(
            userPromptGenerateTasks, 
                new KernelArguments() { { "input", inputTask } });
        Console.WriteLine($"\n ==========================================\n Tasks Generated!! \n ==========================================\n ");
        Console.WriteLine($"The elaborated tasks: {jsonWithTasks}");

        var resultJsonTasks = jsonWithTasks.GetValue<string>();
        EventPlan eventPlan = DeserializeJsonTasks(resultJsonTasks);
        eventPlan.Tasklist.Reverse(); // reverse the order of the tasks so we can add them in the correct order in Planner.

        var handlebarsTemplate =
          @"
            {{set ""newBucket"" (MicrosoftPlannerGraphPlugin-CreateBucket planId=MicrosoftPlannerPlanId name=eventPlan.Bucketname )}}
            {{set ""newBucketId"" newBucket.id}}

            {{#each eventPlan.Tasklist}}
              {{set ""newTask"" (MicrosoftPlannerGraphPlugin-CreateTask planId=MicrosoftPlannerPlanId bucketId=newBucketId taskTitle=this.Task)}}
            {{/each}}
            
            Tell the user that you have created the following Bucket in Microsoft Planner: {{json eventPlan.Bucketname}}
            As well as the following tasks:
            {{#each eventPlan.Tasklist}}
              - {{json this.Task}}
            {{/each}}

            Let the user know that the tasks have been created in the Microsoft Planner plan and now they need to be done in a funny way.
           ";

        var HandlebarsSPromptFunction = kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = handlebarsTemplate,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        Console.WriteLine($"\n ==========================================\n Creating Tasks (and bucket)  in Microsoft Planner \n ==========================================\n ");

        // Invoke prompt
        var customHandlebarsPromptResult = 
            await kernel.InvokeAsync(
                HandlebarsSPromptFunction,
                new() {
                    { "eventPlan", eventPlan },
                    { "MicrosoftPlannerPlanId", MicrosoftPlannerPlanId }
                }
            );
        Console.WriteLine($"\n ==========================================\n Tasks (and bucket) Created in Planner \n ==========================================\n ");

        Console.WriteLine($"\n\n Result:  {customHandlebarsPromptResult}");

        Console.WriteLine();
    }

    private EventPlan DeserializeJsonTasks(string jsonWithTasks)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        EventPlan eventPlan = JsonSerializer.Deserialize<EventPlan>(jsonWithTasks, options);

        Console.WriteLine($"Bucket Name: {eventPlan.Bucketname}");
        foreach (var task in eventPlan.Tasklist)
        {
            Console.WriteLine(task.Task);
        }

        return eventPlan;
    }
}

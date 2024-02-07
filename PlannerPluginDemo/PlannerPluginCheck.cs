using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MicrosoftPlannerPlugin;

namespace PlannerPluginDemo;

public class PlannerPluginCheck
{
    public async Task ExecuteAsync()
    {
        Console.WriteLine("Test the planner!!");
        var modelDeploymentName = "gpt4v613"; // "gpt4"; - was 0314 (no function calling)
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI_Endpoint", EnvironmentVariableTarget.User);
        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AzureOpenAI_ApiKey", EnvironmentVariableTarget.User);

        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(
            modelDeploymentName,
            azureOpenAIEndpoint,
            azureOpenAIApiKey,
            modelId: "gpt-4"
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
        string userPrompt = @"I want YOU to plan the tasks needed to accomplish the following task:" +
            "---" +
            "{{ $input }}" +
            "---" +
            "For this task, can you prepare a list of tasks in order how they should be done? " +
            "Once you have this, I would like to create the tasks in Microsoft Planner using the Graph API plugin tool." +
            "Note that you need to do this in the following order:" +
            "1. Create a Microsoft Graph Group and remember the group id. Note the group name must not exist, so add a random 4 digit number to the end." +
            "2. Get the user id from the admin user provided his user principal name: {{ $adminUser }}." +
            "3. Add this retrieved user to the graph group by using the admin user id provided and the group id" +
            "4. Create a plan by using the group id and an original name that does not exist (add a random 4 digit number to the end). Remember the Plan id." +
            "5. Create a bucket in the plan by using the plan id and a suitable name" +
            "6. Create the tasks in the bucket, using the plan id and the bucket id. Preceed the tasks with 'Task 1: ' and follow with the title of the task and so on (update the numbering)" +
            "At the end summarize what you just did";

        string inputTask = "The task is the following:" +
            "Plan an announcement of the technical company events for this year, for this I want to annotate" +
            "the ideas, brainstorm. Then sync with my team lead and next with the architecture and cloud departments. Afterwards I would " +
            "like to present it to the CTO and get approval - and budget! Also after all is ok-ed I want to announce it in the wiki, Teams " +
            "and some events just to make our people aware.";
        string adminUser = "admin@M365x27369537.onmicrosoft.com";

        var plannerFunction = kernel.CreateFunctionFromPrompt(
            userPrompt, 
            new OpenAIPromptExecutionSettings() {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                MaxTokens = 650, 
                Temperature = 0, 
                TopP = 1 });

        var response = 
            await kernel.InvokeAsync(
                plannerFunction, 
                new KernelArguments()
                {
                    { "input", inputTask },
                    { "adminUser", adminUser }
                });

        Console.WriteLine($"Result: {response}");
        Console.WriteLine();

    }
}

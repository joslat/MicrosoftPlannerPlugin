using Microsoft.Graph.Models;

namespace MicrosoftPlannerPlugin;

public class PluginCheck
{
    public MicrosoftPlannerGraphPlugin msGraphPlugin { get; private set; }

    public PluginCheck()
    {
        // Go Graph Gooo
        string msGraphTenantId = Environment.GetEnvironmentVariable("msGraphTenantId", EnvironmentVariableTarget.User);
        string msGraphClientId = Environment.GetEnvironmentVariable("msGraphClientId", EnvironmentVariableTarget.User);  // app id
        string msGraphClientSecret = Environment.GetEnvironmentVariable("msGraphClientSecret", EnvironmentVariableTarget.User);

        msGraphPlugin = new MicrosoftPlannerGraphPlugin(msGraphTenantId, msGraphClientId, msGraphClientSecret);
    }

    public async Task ExecuteAsync()
    {
        Console.WriteLine("Test the planner!!");
        var userAdmin = "admin@M365x27369537.onmicrosoft.com";
        await RunPlannerTestAsync(msGraphPlugin, userAdmin);
    }

    private async Task RunPlannerTestAsync(
        MicrosoftPlannerGraphPlugin plannerGraphPlugin,
        string user)
    {
        var groupPlanID = "008";
        var adminUser = await msGraphPlugin.GetUser("admin@M365x27369537.onmicrosoft.com");

        var userY = await msGraphPlugin.GetUser("AdeleV@M365x27369537.OnMicrosoft.com");
        //var userX = await plannerGraphPlugin.GetUserAsync();

        List<Group> groups = await plannerGraphPlugin.GetAllGroupsAsync();
        List<PlannerPlan> plans = await plannerGraphPlugin.GetAllPlansAsync(groups);

        var group = await plannerGraphPlugin.CreateGroupAsync(
            $"the AI Agent Group {groupPlanID}",
            $"The AI Agent Group for this thing...{groupPlanID}");

        await plannerGraphPlugin.AddUserToGroupAsync(
            adminUser.Id.ToString(),
            group.Id.ToString());

        // await plannerGraphPlugin.AddCurrentUserToGroupAsync(group.Id.ToString());

        String thePlanName = $"The AI Agent Plan {groupPlanID}";

        // Step 1: Create a Plan
        // var plan = await plannerGraphPlugin.CreatePlanAsync(thePlanName);
        var plan = await plannerGraphPlugin.CreatePlanAsync(thePlanName, group.Id.ToString());



        // Step 2: Create a Bucket
        var bucket = await plannerGraphPlugin.CreateBucketAsync(
            plan.Id,
            $"My New Bucket {groupPlanID}");

        // Step 3: Add Tasks
        var task1 = await plannerGraphPlugin.CreateTaskAsync(plan.Id, bucket.Id, "Task 1");
        var task2 = await plannerGraphPlugin.CreateTaskAsync(plan.Id, bucket.Id, "Task 2");
        var task3 = await plannerGraphPlugin.CreateTaskAsync(plan.Id, bucket.Id, "Task 3");


        // Assume more tasks are added and we want to mark task1 as done for demonstration
        //await plannerGraphPlugin.UpdateTaskToDoneAsync(task1);
        //await plannerGraphPlugin.UpdateTaskToDoneAsync(task3);

        // Optionally, check if all tasks in the bucket are completed
        await plannerGraphPlugin.AreAllTasksDoneInBucketAsync(bucket.Id);

        // update last task
        //await plannerGraphPlugin.UpdateTaskToDoneAsync(task2);

        // Check again
        //await plannerGraphPlugin.AreAllTasksDoneInBucketAsync(bucket.Id);


    }

}

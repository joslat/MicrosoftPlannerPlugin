using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace MicrosoftPlannerPlugin;

public sealed class MicrosoftPlannerGraphPlugin : MicrosoftGraphPluginBase
{
    public MicrosoftPlannerGraphPlugin(
        string tenantId,
        string clientId,
        string clientSecret)
        : base(tenantId, clientId, clientSecret)
    {
    }
    /// <summary>
    /// Gets the Microsoft Graph User from a user principal name
    /// </summary>
    /// <param name="userPrincipalName"></param>
    /// <returns></returns>
    [KernelFunction, Description("Returns the Microsoft Graph User from a user principal name")]
    public async Task<User?> GetUser(
         [Description("User Principal Name")] string userPrincipalName)
    {
        return await GraphClient.Users[userPrincipalName].GetAsync();
    }

    public async Task<(IEnumerable<Site>?, IEnumerable<Site>?)> GetSharepointSites()
    {
        var sites = (await GraphClient.Sites.GetAllSites.GetAsync())?.Value;
        if (sites == null)
        {
            return (null, null);
        }

        sites.RemoveAll(x => string.IsNullOrEmpty(x.DisplayName));

        var spSites = new List<Site>();
        var oneDriveSites = new List<Site>();

        foreach (var site in sites)
        {
            if (site == null) continue;

            var compare = site.WebUrl?.Split(site.SiteCollection?.Hostname)[1].Split("/");
            if (compare.All(x => !string.IsNullOrEmpty(x)) || compare.Length < 1)
            {
                continue;
            }

            if (compare[1] == "sites" || string.IsNullOrEmpty(compare[1]))
                spSites.Add(site);
            else if (compare[1] == "personal")
                oneDriveSites.Add(site);
        }

        return (spSites, oneDriveSites);
    }

    /// <summary>
    /// Creates a Microsoft Planner Plan given a title and an owner
    /// </summary>
    /// <param name="title"></param>
    /// <param name="owner"></param>
    /// <returns></returns>
    [KernelFunction, Description("Creates a Microsoft Planner Plan given a title and an owner")]
    public async Task<PlannerPlan> CreatePlanAsync(
        [Description("The title of the Microsoft Planner Plan")] string title,
        [Description("The owner of the Microsoft Planner Plan")] string owner)
    {
        var plan = new PlannerPlan
        {
            Title = title,
            Owner = owner
        };

        var createdPlan =
            await GraphClient.Planner.Plans.PostAsync(plan);

        return createdPlan;
    }

    private async Task<PlannerPlan> CreatePlanAsync(string title)
    {
        var plan = new PlannerPlan
        {
            Title = title
        };

            var createdPlan =
            await GraphClient.Planner.Plans.PostAsync(plan);

        return createdPlan;
    }

    /// <summary>
    /// Creates a Microsoft Planner Bucket given a Microsoft Planner plan ID and a name
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [KernelFunction, Description("Creates a Microsoft Planner Bucket given a Microsoft Planner plan ID and a bucket name")]
    public async Task<PlannerBucket> CreateBucketAsync(
        [Description("The Microsoft Planner plan id")] string planId,
        [Description("The Microsoft Planner bucket name")] string name)
    {
        var bucket = new PlannerBucket
        {
            Name = name,
            PlanId = planId
        };

        var createdBucket = 
            await GraphClient.Planner.Buckets.PostAsync(bucket);

        return createdBucket;
    }
    /// <summary>
    /// Creates a Microsoft Planner Task given a plan ID, a bucket ID, and a task title
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="bucketId"></param>
    /// <param name="taskTitle"></param>
    /// <returns></returns>
    /// 
    [KernelFunction, Description("Creates a Microsoft Planner Task given a plan ID, a bucket ID, and a task title")]
    public async Task<PlannerTask> CreateTaskAsync(
        [Description("The Microsoft Planner plan id")] string planId,
        [Description("The Microsoft Planner bucket id")] string bucketId,
        [Description("The Microsoft Planner task title")] string taskTitle)
    {
        PlannerTask createdTask = null;

        var plannerTask = new PlannerTask
        {
            PlanId = planId,
            BucketId = bucketId,
            Title = taskTitle,
            DueDateTime = DateTimeOffset.UtcNow.AddDays(7), // Optional: Set a due date 7 days from now
            Assignments = new PlannerAssignments() // Optional: Assign the task (see note below)
        };

        try
        {
            createdTask = 
                await GraphClient.Planner.Tasks
                    .PostAsync(plannerTask);

            Console.WriteLine($"Task created: {createdTask.Title}");
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error creating task: {ex.Message}");
        }

        return createdTask;
    }

    /// <summary>
    /// Gets all the tasks in a Microsoft Planner bucket
    /// </summary>
    /// <param name="bucketId"></param>
    /// <returns></returns>
    [KernelFunction, Description("Gets all the tasks in a Microsoft Planner bucket")]
    public async Task<IEnumerable<PlannerTask>?> GetBucketTasksAsync(
        [Description("The Microsoft Planner bucket id")] string bucketId)
    {
        var tasks = 
            (await GraphClient.Planner.Buckets[bucketId].Tasks.GetAsync())?.Value;
        return tasks;
    }

    /// <summary>
    /// Updates a Microsoft Planner task to done
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    [KernelFunction, Description("Updates a Microsoft Planner task to done")]
    public async Task UpdateTaskToDoneAsync(
        [Description("The Microsoft Planner task")] PlannerTask task)
    {
        task.PercentComplete = 100;

        await GraphClient.Planner.Tasks[task.Id.ToString()]
            .PatchAsync(task);
    }

    /// <summary>
    /// Checks if all tasks in a Microsoft Planner bucket are done
    /// </summary>
    /// <param name="bucketId"></param>
    /// <returns></returns>
    [KernelFunction, Description("Checks if all tasks in a Microsoft Planner bucket are done")]
    public async Task<Boolean> AreAllTasksDoneInBucketAsync(
        [Description("The Microsoft Planner bucket id")] string bucketId)
    {
        var tasks = await GetBucketTasksAsync(bucketId);

        bool allDone = tasks.All(task => task.PercentComplete == 100);

        if (allDone)
        {
            // All tasks in the bucket are done. Take appropriate action.
            Console.WriteLine("All tasks in the bucket are completed.");
        }
        else
        {
            // Not all tasks in the bucket are done. Take appropriate action.
            Console.WriteLine("Not all tasks in the bucket are completed.");
        }

        return allDone;
    }

    /// <summary>
    /// Creates a Microsoft Planner Group
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="groupDescription"></param>
    /// <returns></returns>
    [KernelFunction, Description("Creates a Microsoft Planner Group, given a group name and description")]
    public async Task<Group> CreateGroupAsync(
        [Description("The Microsoft graph group name")] string groupName,
        [Description("The Microsoft graph group description")] string groupDescription)
    {
        var group = new Group
        {
            DisplayName = groupName,
            Description = groupDescription,
            GroupTypes = new List<string> { "Unified" }, // Indicates a Microsoft 365 Group
            MailEnabled = true,
            MailNickname = groupName.ToLower().Replace(" ", ""),
            SecurityEnabled = false
        };

        try
        {
            var createdGroup = await GraphClient.Groups
                .PostAsync(group);
            
            Console.WriteLine($"Group created with ID: {createdGroup.Id}");
            
            return createdGroup;
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error creating group: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all the Microsoft graph Groups
    /// </summary>
    /// <returns></returns>
    [KernelFunction, Description("Gets all the Microsoft graph Groups")]
    public async Task<List<Group>> GetAllGroupsAsync()
    {
        var result = await GraphClient.Groups.GetAsync();

        List<Group> groups =
            result.Value;

        return groups;
    }

    /// <summary>
    /// Gets all the Microsoft Planner Plans for a list of Microsoft graph Groups
    /// </summary>
    /// <param name="groups"></param>
    /// <returns></returns>
    [KernelFunction, Description("Gets all the Microsoft Planner Plans for a list of Microsoft graph Groups")]
    public async Task<List<PlannerPlan>> GetAllPlansAsync(
        [Description("The Microsoft Graph list of groups to retrieve plans from")] List<Group> groups)
    {
        List<PlannerPlan> allPlans = new List<PlannerPlan>();

        foreach (var group in groups)
        {
            var plansPage = await GraphClient.Groups[group.Id].Planner.Plans
                .GetAsync(); // If this direct call is correct for your SDK version

            var plans = plansPage.Value;
            allPlans.AddRange(plans);
        }

        return allPlans;
    }

    public async Task<User> GetUserAsync() 
    {
        // Get the current user's ID
        var currentUser =
            await GraphClient.Me
                .GetAsync();

        return currentUser;
    }

    /// <summary>
    /// Adds a user to a group
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    [KernelFunction, Description("Adds a user to a group")]
    public async Task AddUserToGroupAsync(
        [Description("The Microsoft Planner user id")] string userId,
        [Description("The Microsoft Planner group id")] string groupId)
    {
        var requestBody = new ReferenceCreate
        {
            // Correctly format the OdataId using the userId parameter
            OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{userId}",
        };

        await GraphClient.Groups[groupId].Members.Ref
            .PostAsync(requestBody);
    }

    public async Task AddCurrentUserToGroupAsync(string groupId)
    {
        try
        {
            var currentUser = await GetUserAsync();

            // Prepare the directory object for adding
            var directoryObject = new DirectoryObject
            {
                Id = currentUser.Id
            };

            var requestBody = new ReferenceCreate
            {
                OdataId = "https://graph.microsoft.com/v1.0/directoryObjects/{currentUser.Id}",
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=csharp

            // Add the current user to the group
            await GraphClient.Groups[groupId].Members.Ref.PostAsync(requestBody);

            Console.WriteLine("Current user added to the group successfully.");
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error adding the current user to the group: {ex.Message}");
        }
    }
}

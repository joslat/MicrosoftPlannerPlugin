using PlannerPluginDemo;

Console.WriteLine("Hello, World!");

PlannerPluginCheck checkPlugin = new PlannerPluginCheck();
await checkPlugin.ExecuteAsync();

Console.WriteLine("Done, planner checked!");
Console.ReadLine();

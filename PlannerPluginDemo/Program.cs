using PlannerPluginDemo;

Console.WriteLine("Hello, Semantic Kernel World!\n");

PlannerPluginCheck checkPlugin = new PlannerPluginCheck();
await checkPlugin.ExecuteAsync();

Console.WriteLine("\n\nDone, Goodbye, Semantic Kernel World!");
Console.ReadLine();

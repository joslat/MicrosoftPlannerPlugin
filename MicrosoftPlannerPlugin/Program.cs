// See https://aka.ms/new-console-template for more information
using MicrosoftPlannerPlugin;

Console.WriteLine("Hello, Semantic kernel World!");


PluginCheck pluginCheck = new PluginCheck();
await pluginCheck.ExecuteAsync();

Console.WriteLine("Goodbye, Semantic kernel World!");
Console.ReadLine();

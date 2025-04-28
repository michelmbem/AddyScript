using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace AddyScript.Gui;

internal static class Program
{
    internal static string[] Directories { get; set; }
    internal static string[] Assemblies { get; set; }
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var files = ParseOptions(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

        private static string[] ParseOptions(string[] args)
        {
            var files = new List<string>();
            var directories = new List<string>();
            var assemblies = new List<string>();

            /*if (Settings.Default.ScriptContextSettings != null)
            {
                directories.AddRange(Settings.Default.ScriptContextSettings.Directories);
                assemblies.AddRange(Settings.Default.ScriptContextSettings.Assemblies);
            }*/

            if (directories.Count <= 0)
                directories.Add(Path.GetFullPath(@"../../../samples"));

            if (assemblies.Count <= 0)
                assemblies.AddRange(["Microsoft.Data.SqlClient"]);

            var index = 0;

            while (index < args.Length && args[index][0] == '-')
            {
                switch (args[index])
                {
                    case "-d":
                        if (index == args.Length - 1 || args[index + 1][0] == '-')
                            throw new ApplicationException("A directory name is required after -d");
                        
                        var dirname = args[index + 1];
                        if (!Directory.Exists(dirname))
                            throw new ArgumentException("Directory '" + dirname + "' does not exist");
                        
                        if (!directories.Contains(dirname)) directories.Add(dirname);
                        break;
                    case "-r":
                        if (index == args.Length - 1 || args[index + 1][0] == '-')
                            throw new ApplicationException("An assembly name is required after -r");

                        var assemblyName = args[index + 1];
                        if (ScriptContext.LoadAssembly(assemblyName) == null)
                            throw new ApplicationException("Assembly '" + assemblyName + "' could not be loaded");

                        if (!assemblies.Contains(assemblyName)) assemblies.Add(assemblyName);
                        break;
                    default:
                        throw new ApplicationException("Invalid option: " + args[index]);
                }

                index += 2;
            }

            while (index < args.Length)
            {
                files.Add(args[index]);
                ++index;
            }

            Directories = [.. directories];
            Assemblies = [.. assemblies];

            return [.. files];
        }
}
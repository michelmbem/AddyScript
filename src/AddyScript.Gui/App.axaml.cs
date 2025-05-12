using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AddyScript.Gui;

public partial class App : Application
{
    public static string[] SearchPaths { get; set; } = [];
    public static string[] References { get; set; } = [];
    private static string[] InitialFiles { get; set; } = [];
    private static List<MainWindow> Windows { get; } = [];

    public static void ParseCmdLineArgs(string[] args)
    {
        var searchPaths = new List<string>();
        var references = new List<string>();
        var initialFiles = new List<string>();

        /*
         if (Settings.Default.ScriptContextSettings != null)
        {
            searchPaths.AddRange(Settings.Default.ScriptContextSettings.SearchPaths);
            references.AddRange(Settings.Default.ScriptContextSettings.References);
        }
        */

        if (searchPaths.Count <= 0)
            searchPaths.Add(Path.GetFullPath(@"../../../samples"));

        if (references.Count <= 0)
            references.AddRange(["Microsoft.Data.SqlClient"]);

        var index = 0;

        while (index < args.Length && args[index][0] == '-')
        {
            switch (args[index])
            {
                case "-d":
                    if (index == args.Length - 1 || args[index + 1][0] == '-')
                        throw new ArgumentException("A directory name is required after -d");
                    
                    var dirname = args[index + 1];
                    if (!Directory.Exists(dirname))
                        throw new ArgumentException("Directory '" + dirname + "' does not exist");
                    
                    if (!searchPaths.Contains(dirname))
                        searchPaths.Add(dirname);
                    break;
                case "-r":
                    if (index == args.Length - 1 || args[index + 1][0] == '-')
                        throw new ArgumentException("An assembly name is required after -r");

                    var assemblyName = args[index + 1];
                    if (ScriptContext.LoadAssembly(assemblyName) == null)
                        throw new ArgumentException("Assembly '" + assemblyName + "' could not be loaded");

                    if (!references.Contains(assemblyName))
                        references.Add(assemblyName);
                    break;
                default:
                    throw new ArgumentException("Invalid option: " + args[index]);
            }

            index += 2;
        }

        while (index < args.Length)
        {
            initialFiles.Add(args[index]);
            ++index;
        }

        SearchPaths = [.. searchPaths];
        References = [.. references];
        InitialFiles = [.. initialFiles];
    }

    public static void OpenWindow(string filePath = null)
    {
        var desktop = (IClassicDesktopStyleApplicationLifetime) Current?.ApplicationLifetime;
        var window = new MainWindow();
        
        if (filePath == null)
            window.Reset();
        else
            window.Open(filePath);

        Windows.Add(window);
        window.Closed += (_, _) =>
        {
            Windows.Remove(window);
            
            if (desktop!.MainWindow == window && Windows.Count > 0)
                desktop.MainWindow = Windows[^1];
        };
        
        window.Show();
        window.Activate();
        desktop!.MainWindow ??= window;  
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (InitialFiles.Length > 0)
                foreach (var file in InitialFiles)
                    OpenWindow(file);
            else
                OpenWindow();
            
            desktop.Exit += (_, _) =>
            {
                foreach (var window in Windows)
                    window.Close();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
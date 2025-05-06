using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AddyScript.Gui;

public partial class App : Application
{
    internal static string[] Directories { get; set; }
    internal static string[] Assemblies { get; set; }
    private static string[] InitialFiles { get; set; }
    private static List<MainWindow> Windows { get; } = [];
    
    private static IClassicDesktopStyleApplicationLifetime Desktop =>
        Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    public static void ParseCmdLineArgs(string[] args)
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
        InitialFiles = [.. files];
    }

    public static void OpenFile(string path = null)
    {
        var window = new MainWindow();
        
        if (path != null)
            window.Open(path);
        else
            window.Reset();
        
        Windows.Add(window);
        window.Closed += (sender, args) =>
        {
            Windows.Remove(window);
            if (Desktop.MainWindow == window && Windows.Count > 0)
                Desktop.MainWindow = Windows[^1];
        };
        
        window.Show();
        window.Activate();
        Desktop.MainWindow ??= window;  
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var file in InitialFiles)
                OpenFile(file);
            
            if (Windows.Count <= 0)
                OpenFile();
            
            desktop.Exit += (sender, args) =>
            {
                foreach (var window in Windows)
                    window.Close();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
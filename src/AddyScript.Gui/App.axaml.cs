using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace AddyScript.Gui;

public partial class App : Application
{
    public const string REPO_URL = "https://github.com/michelmbem/AddyScript";
    
    #region Properties
    
    public static string[] SearchPaths { get; set; } = [];
    
    public static string[] References { get; set; } = [];
    
    private static List<MainWindow> Windows { get; } = [];
    
    #endregion
    
    #region Utility Static Methods

    private static string[] ParseCmdLineArgs(string[] args)
    {
        List<string> searchPaths = [];
        List<string> references = [];
        List<string> initialFiles = [];

        /*
         if (Settings.Default.ScriptContextSettings != null)
        {
            searchPaths.AddRange(Settings.Default.ScriptContextSettings.SearchPaths);
            references.AddRange(Settings.Default.ScriptContextSettings.References);
        }
        */

        if (searchPaths.Count == 0)
            searchPaths.Add(Path.GetFullPath(@"../../../samples"));

        if (references.Count == 0)
            references.AddRange(["Microsoft.Data.SqlClient"]);

        var index = 0;

        while (index < args.Length && args[index][0] == '-')
        {
            switch (args[index])
            {
                case "-d":
                {
                    if (index == args.Length - 1 || args[index + 1][0] == '-')
                        throw new ArgumentException("A directory name is required after -d");

                    var dirname = args[index + 1];
                    if (!Directory.Exists(dirname))
                        throw new ArgumentException("Directory '" + dirname + "' does not exist");

                    if (!searchPaths.Contains(dirname))
                        searchPaths.Add(dirname);
                    break;
                }
                case "-r":
                {
                    if (index == args.Length - 1 || args[index + 1][0] == '-')
                        throw new ArgumentException("An assembly name is required after -r");

                    var assemblyName = args[index + 1];
                    if (ScriptContext.LoadAssembly(assemblyName) == null)
                        throw new ArgumentException("Assembly '" + assemblyName + "' could not be loaded");

                    if (!references.Contains(assemblyName))
                        references.Add(assemblyName);
                    break;
                }
                default:
                    throw new ArgumentException("Invalid option: " + args[index]);
            }

            index += 2;
        }

        while (index < args.Length)
        {
            initialFiles.Add(args[index++]);
        }

        SearchPaths = [.. searchPaths];
        References = [.. references];
        
        return [.. initialFiles];
    }

    public static void OpenWindow(string filePath = null)
    {
        var desktop = (IClassicDesktopStyleApplicationLifetime) Current?.ApplicationLifetime;
        var window = new MainWindow();
        
        if (string.IsNullOrWhiteSpace(filePath))
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
    
    private static void RegisterGrammar()
    {
        // Correct URI format avares://<assembly-name>/path/to/resource
        var uri = new Uri("avares://asgui/Assets/Grammars/AddyScript-Mode.xshd");
        using var stream = AssetLoader.Open(uri);
        using var reader = new XmlTextReader(stream);
        
        // Load the definition
        var asHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        
        // Register it with the highlighting manager
        HighlightingManager.Instance.RegisterHighlighting(
            "AddyScript",
            [".add", ".txt"], 
            asHighlighting);
    }
    
    #endregion
    
    #region Overrides

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterGrammar();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var initialFiles = ParseCmdLineArgs(desktop.Args);
            
            if (initialFiles.Length > 0)
                foreach (var file in initialFiles)
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
    
    #endregion
}
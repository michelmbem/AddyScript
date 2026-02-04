using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using AddyScript.Gui.Configuration;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace AddyScript.Gui;

public partial class App : Application
{
    #region Properties

    public static Options Options { get; set; }

    public static List<MainWindow> Windows { get; } = [];

    public static MainWindow ActiveWindow { get; private set; }

    #endregion

    #region Utility Static Methods

    /// <summary>
    /// Opens a new main window and displays its contents, optionally loading a file if a path is provided.
    /// </summary>
    /// <remarks>
    /// If multiple windows are open, the first opened window becomes the application's main
    /// window. When a window is closed, the main window is reassigned if necessary.
    /// </remarks>
    /// <param name="filePath">The path to the file to open in the new window. If null, the window opens without loading a file.</param>
    public static void OpenWindow(string filePath = null)
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        MainWindow window = new ();
        window.Activated += (_, _) => ActiveWindow = window;
        window.Opened += (_, _) =>
        {
            Windows.Add(window);
            desktop.MainWindow ??= window;
        };
        window.Closed += (_, _) =>
        {
            Windows.Remove(window);
            if (desktop.MainWindow == window && Windows.Count > 0)
                desktop.MainWindow = Windows[0];
        };

        PositionWindow(window);
        window.Open(filePath);
        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Automatically reposition the window on desktop to prevent all windows overlapping each others.
    /// </summary>
    /// <remarks>This feature is only required on platforms other than Windows</remarks>
    /// <param name="window">The window to position on screen</param>
    private static void PositionWindow(MainWindow window)
    {
        if (Windows.Count == 0 || OperatingSystem.IsWindows()) return;

        var screen = window.Screens.ScreenFromVisual(window);
        var workingArea = screen!.WorkingArea;
        var pos = Windows[^1].Position;

        int winX = pos.X + 32;
        if (winX + window.Width >= workingArea.Width)
            winX = workingArea.X;

        int winY = pos.Y + 32;
        if (winY + window.Height >= workingArea.Height)
            winY = workingArea.Y;

        window.Position = new PixelPoint(winX, winY);
    }

    /// <summary>
    /// Registers the AddyScript syntax highlighting definition with the highlighting manager.
    /// </summary>
    /// <remarks>
    /// This method loads the AddyScript highlighting definition from an embedded resource and
    /// associates it with the ".add" and ".txt" file extensions. It should be called before attempting to use
    /// AddyScript syntax highlighting in the application.
    /// </remarks>
    private static void RegisterGrammar()
    {
        // Correct URI format avares://<assembly-name>/path/to/resource
        var uri = new Uri("avares://asgui/Assets/Grammars/AddyScript-Mode.xshd");
        using var stream = AssetLoader.Open(uri);
        using var reader = new XmlTextReader(stream);

        // Load the definition
        var asHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

        // Register it with the highlighting manager
        HighlightingManager.Instance.RegisterHighlighting("AddyScript", [".add", ".txt"], asHighlighting);
    }

    /// <summary>
    /// Parses command-line arguments to configure search paths, assembly references, and initial files for script
    /// execution.
    /// </summary>
    /// <remarks>
    /// If no search paths or references are specified, default values are used. The method updates
    /// the SearchPaths and References properties with the parsed values.
    /// </remarks>
    /// <param name="args">
    /// An array of command-line argument strings to be parsed. Options include '-d' for specifying a search directory
    /// and '-r' for specifying an assembly reference. All other arguments are treated as initial file names.
    /// </param>
    /// <returns>
    /// An array of strings containing the initial file names specified in the command-line arguments.
    /// The array is empty if no file names are provided.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if an invalid option is encountered, if a required argument for '-d' or '-r' is missing, if the specified
    /// directory does not exist, or if the specified assembly cannot be loaded.
    /// </exception>
    private static string[] ParseCmdLineArgs(string[] args)
    {
        List<string> initialFiles = [];

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

                    if (!Options.SearchPaths.Contains(dirname))
                        Options.SearchPaths.Add(dirname);
                    break;
                }
                case "-r":
                {
                    if (index == args.Length - 1 || args[index + 1][0] == '-')
                        throw new ArgumentException("An assembly name is required after -r");

                    var assemblyName = args[index + 1];
                    if (ScriptContext.LoadAssembly(assemblyName) == null)
                        throw new ArgumentException("Assembly '" + assemblyName + "' could not be loaded");

                    if (!Options.References.Contains(assemblyName))
                        Options.References.Add(assemblyName);
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

        return [.. initialFiles];
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
        Options = ConfigurationManager.LoadOptions();

        if (Options?.Culture != null)
        {
            var currentThread = Thread.CurrentThread;
            currentThread.CurrentUICulture = currentThread.CurrentCulture = Options.Culture;
        }

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
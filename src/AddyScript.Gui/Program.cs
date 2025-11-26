using System;
using System.IO;
using System.Xml;
using Avalonia;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace AddyScript.Gui;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        App.ParseCmdLineArgs(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();
        
        RegisterGrammar();
        
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
    
    private static void RegisterGrammar()
    {
        using var stream = File.OpenRead("AddyScript-Mode.xshd");
        using var reader = new XmlTextReader(stream);
        
        // Load the definition
        var asHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        
        // Register it with the highlighting manager
        HighlightingManager.Instance.RegisterHighlighting(
            "AddyScript",
            [".add", ".txt"], 
            asHighlighting);
    }
}
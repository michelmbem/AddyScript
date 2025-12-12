using System.Collections.Generic;
using Avalonia.Media;

namespace AddyScript.Gui;

public abstract class EditorOptionsBase
{
    public FontFamily FontFamily { get; set; }
    
    public double FontSize { get; set; }
    
    public bool WordWrap { get; set; }

    public void CopyFrom(EditorOptionsBase other)
    {
        FontFamily = other.FontFamily;
        FontSize = other.FontSize;
        WordWrap = other.WordWrap;
    }
}

public class EditorOptions : EditorOptionsBase
{
    public bool ShowLineNumbers { get; set; }
    
    public bool ShowWhitespace { get; set; }
    
    public bool HighlightCurrentLine { get; set; }

    public EditorOptions Clone() => new EditorOptions
    {
        FontFamily = this.FontFamily,
        FontSize = this.FontSize,
        WordWrap = this.WordWrap,
        ShowLineNumbers = this.ShowLineNumbers,
        ShowWhitespace = this.ShowWhitespace,
        HighlightCurrentLine = this.HighlightCurrentLine,
    };
}

public class TerminalOptions : EditorOptionsBase
{
    public Color Background { get; set; }
    
    public Color Foreground { get; set; }

    public TerminalOptions Clone() => new TerminalOptions
    {
        Background = this.Background,
        Foreground = this.Foreground,
        FontFamily = this.FontFamily,
        FontSize = this.FontSize,
        WordWrap = this.WordWrap,
    };
}

public class Options
{
    public List<string> SearchPaths { get; set; }
    
    public List<string> References { get; set; }
    
    public EditorOptions Editor { get; set; }
    
    public TerminalOptions Terminal { get; set; }
    
    public bool UseEmulatedTerminal { get; set; }

    public Options Clone() => new Options
    {
        SearchPaths = [..this.SearchPaths],
        References = [..this.References],
        Editor = this.Editor?.Clone(),
        Terminal =  this.Terminal?.Clone(),
        UseEmulatedTerminal = this.UseEmulatedTerminal,
    };
}
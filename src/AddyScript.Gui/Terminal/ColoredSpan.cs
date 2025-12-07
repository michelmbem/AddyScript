using Avalonia.Media;

namespace AddyScript.Gui.Terminal;

public class ColoredSpan
{
    public int StartOffset { get; set; }
    public int Length { get; set; }
    public Color Foreground { get; set; }
    public Color Background { get; set; }
}

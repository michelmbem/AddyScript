using AddyScript.Gui.Extensions;
using Avalonia;
using Avalonia.Media;
using Projektanker.Icons.Avalonia;

namespace AddyScript.Gui;

internal static class ImageFactory
{
    public static IImage LoadFontIcon(string key, int size = 16, Color? color = null)
    {
        var icon = new Icon
        {
            Value = key,
            Width = size,
            Height = size,
        };
        
        if (color.HasValue)
            icon.Foreground = new SolidColorBrush(color.Value);
        
        icon.Measure(Size.Infinity);
        icon.Arrange(new Rect(icon.DesiredSize));
        
        return icon.RenderToBitmap(size);
    }
}

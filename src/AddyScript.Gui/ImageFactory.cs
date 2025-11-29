using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Projektanker.Icons.Avalonia;

namespace AddyScript.Gui;

internal static class ImageFactory
{
    public static IImage LoadFontIcon(string key, int size = 16)
    {
        var icon = new Icon
        {
            Value = key,
            Width = size,
            Height = size
        };
        
        icon.Measure(Size.Infinity);
        icon.Arrange(new Rect(icon.DesiredSize));

        return RenderControlToBitmap(icon, size, size);
    }

    private static RenderTargetBitmap RenderControlToBitmap(Control control, int width, int height)
    {
        var pixelSize = new PixelSize(width, height);
        var dpi = new Vector(96, 96); // standard DPI

        var rtb = new RenderTargetBitmap(pixelSize, dpi);
        rtb.Render(control);
        return rtb;
    }
}

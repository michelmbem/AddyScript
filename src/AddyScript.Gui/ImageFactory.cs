using AddyScript.Gui.Extensions;
using Avalonia.Media;
using Projektanker.Icons.Avalonia;

namespace AddyScript.Gui;

/// <summary>
/// A helper class for loading and/or generating images.
/// </summary>
internal static class ImageFactory
{
    /// <summary>
    /// Loads a glyph from a vectorial icon font
    /// </summary>
    /// <param name="key">The glyph identifier</param>
    /// <param name="size">The desired glyph size</param>
    /// <param name="color">The desired glyph color</param>
    /// <returns>An <see cref="IImage"/></returns>
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

        icon.Pack();
        
        return icon.ToBitmap(size);
    }
}

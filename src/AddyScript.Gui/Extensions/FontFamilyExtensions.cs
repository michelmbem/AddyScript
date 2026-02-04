using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides extension methods for the FontFamily type to assist with text measurement and font analysis.
/// </summary>
internal static class FontFamilyExtensions
{
    private static readonly char[] TestChars = ['i', 'W', '.', 'm', '0', '1'];

    /// <summary>
    /// Measures the rendered size of the specified text string when drawn with the given font family and font size.
    /// </summary>
    /// <remarks>
    /// The returned size reflects the layout of the text as it would appear when rendered, including
    /// font metrics and shaping. This method does not account for additional layout constraints such as wrapping or
    /// trimming.
    /// </remarks>
    /// <param name="fontFamily">The font family to use when measuring the text.</param>
    /// <param name="text">The text string to measure.</param>
    /// <param name="fontSize">The font size, in device-independent units (DIPs), to use for measuring the text. Must be greater than zero.</param>
    /// <returns>
    /// A Size structure representing the width and height, in device-independent units (DIPs), required to render the
    /// specified text with the given font family and size.
    /// </returns>
    public static Size TextSize(this FontFamily fontFamily, string text, double fontSize)
    {
        var typeface = new Typeface(fontFamily);
        var shaperOptions = new TextShaperOptions(typeface.GlyphTypeface, fontSize);
        var shapedBuffer = TextShaper.Current.ShapeText(text, shaperOptions);
        var runProperties = new GenericTextRunProperties(typeface, fontSize);
        var run = new ShapedTextRun(shapedBuffer, runProperties);

        return run.Size;
    }

    /// <summary>
    /// Checks whether a font family a monospaced or not.
    /// </summary>
    /// <param name="fontFamily">The font family to check</param>
    /// <param name="fontSize">A reference font size</param>
    /// <returns><b>true</b> if all glyphes have equal width in the given font family, <b>false</b> otherwise</returns>
    public static bool IsMonospaced(this FontFamily fontFamily, double fontSize = 14)
    {
        double? width = null;

        foreach (var ch in TestChars)
        {
            var measured = fontFamily.TextSize(ch.ToString(), fontSize).Width;

            if (width == null)
                width = measured;
            else if (Math.Abs(width.Value - measured) > 0.01)
                return false;
        }

        return true;
    }
}

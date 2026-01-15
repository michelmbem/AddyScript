using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides extension methods for working with controls and visual elements.
/// </summary>
internal static class ControlExtensions
{
    /// <summary>
    /// Standard DPI
    /// </summary>
    private static readonly Vector StandardDpi = new (96, 96);

    /// <summary>
    /// Renders <paramref name="visual"/> to a bitmap.
    /// </summary>
    /// <param name="visual">The <see cref="Visual"/> to render as a bitmap</param>
    /// <param name="width">The desired bitmap's width</param>
    /// <param name="height">The desired bitmap's height. Defaults to <paramref name="width"/></param>
    /// <returns>A <see cref="RenderTargetBitmap"/> representing the given visual as it appears on screen</returns>
    public static RenderTargetBitmap ToBitmap(this Visual visual, int width, int? height = null)
    {
        var pixelSize = new PixelSize(width, height ?? width);
        var bitmap = new RenderTargetBitmap(pixelSize, StandardDpi);
        bitmap.Render(visual);
        
        return bitmap;
    }

    /// <summary>
    /// Updates <see cref="Control"/> layout.
    /// </summary>
    /// <param name="control">The <see cref="Control"/> to layout</param>
    public static void Pack(this Control control)
    {
        control.Measure(Size.Infinity);
        control.Arrange(new Rect(control.DesiredSize));
    }

    /// <summary>
    /// Forces a <see cref="Control"/> to re-render itself.
    /// </summary>
    /// <param name="control">The <see cref="Control"/> to re-render</param>
    public static void Repaint(this Control control)
    {
        control.InvalidateMeasure();
        control.InvalidateVisual();
        control.UpdateLayout();
    }
}
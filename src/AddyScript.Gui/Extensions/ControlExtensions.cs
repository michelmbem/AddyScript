using System;
using System.Collections.Generic;
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
    public static RenderTargetBitmap RenderToBitmap(this Visual visual, int width, int? height = null)
    {
        var pixelSize = new PixelSize(width, height ?? width);
        var bitmap = new RenderTargetBitmap(pixelSize, StandardDpi);
        bitmap.Render(visual);
        
        return bitmap;
    }

    /// <summary>
    /// Renders a region of <paramref name="visual"/> to a bitmap.
    /// </summary>
    /// <param name="visual">The <see cref="Visual"/> to render as a bitmap</param>
    /// <param name="region">The region that should be rendered as a bitmap</param>
    /// <returns>A <see cref="RenderTargetBitmap"/> representing a region of the given visual as it appears on screen</returns>
    public static RenderTargetBitmap RenderRegionToBitmap(this Visual visual, Rect region)
    {
        var pixelSize = new PixelSize((int)region.Width, (int)region.Height);
        var bitmap = new RenderTargetBitmap(pixelSize, StandardDpi);
        
        using var ctx = bitmap.CreateDrawingContext(false);

        ctx.PushClip(region);
        ctx.PushTransform(Matrix.CreateTranslation(new Vector(-region.X, -region.Y)));
        visual.Render(ctx);
        
        return bitmap;
    }

    /// <summary>
    /// Gets a collection of bitmaps representing the pages of a <see cref="Control"/>.
    /// </summary>
    /// <param name="control">The <see cref="Control"/> that should be rendered</param>
    /// <param name="pageSize">The desired page size. Defaults to US Letter in portrait mode</param>
    /// <returns>A list of bitmaps representing the pages of a <paramref name="control"/></returns>
    public static List<RenderTargetBitmap> GetPagesAsBitmaps(this Control control, Size? pageSize = null)
    {
        // Ensure proper layout
        control.Repaint();

        // Choose your PDF page size (default to Letter size)
        var pageWidth = (pageSize?.Width ?? 8.5) * 96;
        var pageHeight = (pageSize?.Height ?? 11) * 96;

        // Fit TextView to the PDF's width so wrapping matches the PDF
        control.Width = pageWidth;
        control.Measure(new Size(pageWidth, double.PositiveInfinity));
        control.Arrange(new Rect(control.DesiredSize));

        var totalHeight = control.Bounds.Height;
        var totalPages = (int)Math.Ceiling(totalHeight / pageHeight);

        var pageVisuals = new List<RenderTargetBitmap>();

        for (var page = 0; page < totalPages; page++)
        {
            var y = page * pageHeight;
            var region = new Rect(0, y, pageWidth, pageHeight);
            var bmp = control.RenderRegionToBitmap(region);
            pageVisuals.Add(bmp);
        }

        // Produce the final pages
        return pageVisuals;
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
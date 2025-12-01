using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AddyScript.Gui.Extensions;

public static class ControlExtensions
{
    public static RenderTargetBitmap RenderToBitmap(this Control control, int width, int height)
    {
        var pixelSize = new PixelSize(width, height);
        var dpi = new Vector(96, 96); // standard DPI
        var bitmap = new RenderTargetBitmap(pixelSize, dpi);
        bitmap.Render(control);
        
        return bitmap;
    }
    
    public static RenderTargetBitmap RenderRegionToBitmap(this Control control, Rect region)
    {
        var pixelSize = new PixelSize((int)region.Width, (int)region.Height);
        var dpi = new Vector(96, 96); // standard DPI
        var bitmap = new RenderTargetBitmap(pixelSize, dpi);
        
        using var ctx = bitmap.CreateDrawingContext(false);
        ctx.PushClip(region);
        ctx.PushTransform(Matrix.CreateTranslation(new Vector(-region.X, -region.Y)));
        control.Render(ctx);
        
        return bitmap;
    }
    
    public static List<RenderTargetBitmap> GetPagesAsBitmaps(this Control control, Size? pageSize = null)
    {
        // Ensure proper layout
        control.InvalidateMeasure();
        control.InvalidateVisual();
        control.UpdateLayout();

        // Choose your PDF page size (default to Letter size)
        var pageWidth = (pageSize?.Width ?? 8.5) * 96;
        var pageHeight = (pageSize?.Height ?? 1056) * 96;

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
    
    public static void Repaint(this Control control)
    {
        control.InvalidateMeasure();
        control.InvalidateVisual();
        control.UpdateLayout();
    }
}
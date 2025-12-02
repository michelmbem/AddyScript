using Avalonia;
using Avalonia.Media;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// A set of additional methods for the <see cref="DrawingContext"/> class.
/// </summary>
internal static class DrawingContextExtensions
{
    /// <summary>
    /// Draws a squiggle in the given rectangle.
    /// </summary>
    /// <param name="ctx">The target drawing context</param>
    /// <param name="rect">The region in which to draw</param>
    /// <param name="brush">The line color</param>
    public static void DrawSquiggle(this DrawingContext ctx, Rect rect, IBrush brush)
    {
        double y = rect.Bottom - 1;
        double step = 4;
        double amplitude = 2;

        var geometry = new StreamGeometry();
        using var ctx2 = geometry.Open();

        ctx2.BeginFigure(new Point(rect.X, y), false);
        bool up = false;

        for (double x = rect.X; x <= rect.Right; x += step)
        {
            ctx2.LineTo(new Point(x, y + (up ? -amplitude : amplitude)));
            up = !up;
        }

        ctx.DrawGeometry(null, new Pen(brush, 1), geometry);
    }
}
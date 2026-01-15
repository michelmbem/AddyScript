using Avalonia;
using Avalonia.Media;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides extension methods for drawing custom shapes on a <see cref="DrawingContext"/>.
/// </summary>
internal static class DrawingContextExtensions
{
    /// <summary>
    /// Draws a squiggly line horizontally across the specified rectangle using the provided brush.
    /// </summary>
    /// <remarks>
    /// The squiggle is drawn along the bottom edge of the rectangle, alternating up and down to
    /// create a wavy effect. The thickness of the line is fixed at 1 unit. This method does not fill the rectangle; it
    /// only draws the squiggly outline.
    /// </remarks>
    /// <param name="ctx">The drawing context to which the squiggle will be rendered.</param>
    /// <param name="rect">The rectangle that defines the horizontal bounds and position of the squiggle.</param>
    /// <param name="brush">The brush used to draw the squiggle line.</param>
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
using Avalonia;
using Avalonia.Media;

namespace AddyScript.Gui.Extensions;

internal static class DrawingContextExtensions
{
    public static void DrawSquiggle(this DrawingContext ctx, Rect rect, IBrush brush)
    {
        double y = rect.Bottom - 1;
        double step = 4;
        double amplitude = 2;

        var geometry = new StreamGeometry();
        using (var ctx2 = geometry.Open())
        {
            bool up = false;
            ctx2.BeginFigure(new Point(rect.X, y), false);

            for (double x = rect.X; x <= rect.Right; x += step)
            {
                ctx2.LineTo(new Point(x, y + (up ? -amplitude : amplitude)));
                up = !up;
            }
        }

        ctx.DrawGeometry(null, new Pen(brush, 1), geometry);
    }
}
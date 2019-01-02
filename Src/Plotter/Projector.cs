using System.Drawing;


namespace Plotter
{
    public class Projector
    {
        private readonly double a, b, c, d;

        public Projector(Window window, Rectangle viewport)
        {
            a = viewport.Width / window.Width;
            b = (viewport.Left * window.Right - viewport.Right * window.Left) / window.Width;
            c = -viewport.Height / window.Height;
            d = (viewport.Bottom * window.Top - viewport.Top * window.Bottom) / window.Height;
        }

        public void Project(double realX, double realY,
                            out int screenX, out int screenY)
        {
            screenX = (int)(a * realX + b);
            screenY = (int)(c * realY + d);
        }

        public void Unproject(int screenX, int screenY,
                              out double realX, out double realY)
        {
            realX = (screenX - b) / a;
            realY = (screenY - d) / c;
        }
    }
}

using System.Drawing;


namespace AddyScript.Plotter
{
    public class Projector(Window window, Rectangle viewport)
    {
        private readonly double a = viewport.Width / window.Width;
        private readonly double b = (viewport.Left * window.Right - viewport.Right * window.Left) / window.Width;
        private readonly double c = -viewport.Height / window.Height;
        private readonly double d = (viewport.Bottom * window.Top - viewport.Top * window.Bottom) / window.Height;

        public void Project(double realX, double realY, out int screenX, out int screenY)
        {
            screenX = (int)(a * realX + b);
            screenY = (int)(c * realY + d);
        }

        public void Unproject(int screenX, int screenY, out double realX, out double realY)
        {
            realX = (screenX - b) / a;
            realY = (screenY - d) / c;
        }
    }
}

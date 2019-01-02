using System;


namespace Plotter
{
    public struct Window
    {
        public double Left, Right, Bottom, Top;

        public Window(double left, double right, double bottom, double top)
        {
            Left = Math.Min(left, right);
            Right = Math.Max(left, right);
            Bottom = Math.Min(bottom, top);
            Top = Math.Max(bottom, top);
        }

        public double Width
        {
            get { return Right - Left; }
        }

        public double Height
        {
            get { return Top - Bottom; }
        }

        public Range GetVerticalAxis(double step)
        {
            return new Range(Math.Ceiling(Left), Right, step);
        }

        public Range GetHorizontalAxis(double step)
        {
            return new Range(Math.Ceiling(Bottom), Top, step);
        }
    }
}

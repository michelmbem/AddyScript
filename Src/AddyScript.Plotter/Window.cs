using System;


namespace AddyScript.Plotter
{
    public struct Window(double left, double right, double bottom, double top)
    {
        public double Left = Math.Min(left, right);
        public double Right = Math.Max(left, right);
        public double Bottom = Math.Min(bottom, top);
        public double Top = Math.Max(bottom, top);

        public readonly double Width => Right - Left;

        public readonly double Height => Top - Bottom;

        public readonly Range GetVerticalAxis(double step)
        {
            return new Range(Math.Ceiling(Left), Right, step);
        }

        public readonly Range GetHorizontalAxis(double step)
        {
            return new Range(Math.Ceiling(Bottom), Top, step);
        }
    }
}

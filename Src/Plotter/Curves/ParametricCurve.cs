namespace Plotter.Curves
{
    public class ParametricCurve : ICurve
    {
        private readonly Function x;
        private readonly Function y;

        public ParametricCurve(Function x, Function y)
        {
            this.x = x;
            this.y = y;
        }

        public PointD GetPoint(double t)
        {
            return new PointD(x[t], y[t]);
        }
    }
}

namespace AddyScript.Plotter.Curves
{
    public class ParametricCurve(Function x, Function y) : ICurve
    {
        private readonly Function x = x;
        private readonly Function y = y;

        public PointD GetPoint(double t)
        {
            return new PointD(x[t], y[t]);
        }
    }
}

namespace AddyScript.Plotter.Curves
{
    public class CartesianCurve(Function f) : ICurve
    {
        private readonly Function f = f;

        public PointD GetPoint(double x)
        {
            return new PointD(x, f[x]);
        }
    }
}
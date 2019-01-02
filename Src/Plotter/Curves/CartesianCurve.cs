namespace Plotter.Curves
{
    public class CartesianCurve : ICurve
    {
        private readonly Function f;

        public CartesianCurve(Function f)
        {
            this.f = f;
        }

        public PointD GetPoint(double x)
        {
            return new PointD(x, f[x]);
        }
    }
}
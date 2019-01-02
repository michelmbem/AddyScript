using System;


namespace Plotter.Curves
{
    public class PolarCurve : ICurve
    {
        private readonly Function rho;

        public PolarCurve(Function rho)
        {
            this.rho = rho;
        }

        public PointD GetPoint(double t)
        {
            double r = rho[t];
            return new PointD(r * Math.Cos(t), r * Math.Sin(t));
        }
    }
}

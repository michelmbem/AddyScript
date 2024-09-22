using System;


namespace AddyScript.Plotter.Curves
{
    public class PolarCurve(Function rho) : ICurve
    {
        private readonly Function rho = rho;

        public PointD GetPoint(double t)
        {
            double r = rho[t];
            return new PointD(r * Math.Cos(t), r * Math.Sin(t));
        }
    }
}

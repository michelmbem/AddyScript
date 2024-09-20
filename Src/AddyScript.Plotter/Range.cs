using System;
using System.Collections;
using System.Collections.Generic;


namespace AddyScript.Plotter
{
    public struct Range(double start, double end, double step) : IEnumerable<double>
    {
        public double Start = Math.Min(start, end);
        public double End = Math.Max(start, end), Step = Math.Abs(step);

        public readonly IEnumerator<double> GetEnumerator()
        {
            for (double current = Start; current <= End; current += Step)
                yield return current;
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

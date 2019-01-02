using System;
using System.Collections;
using System.Collections.Generic;


namespace Plotter
{
    public struct Range : IEnumerable<double>
    {
        public double Start, End, Step;

        public Range(double start, double end, double step)
        {
            Start = Math.Min(start, end);
            End = Math.Max(start, end);
            Step = Math.Abs(step);
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (double current = Start; current <= End; current += Step)
                yield return current;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

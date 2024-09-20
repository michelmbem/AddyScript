using System;
using System.Numerics;

using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Void : DataItem
    {
        public static readonly Void Value = new ();

        private Void()
        {
        }

        public override Class Class
        {
            get { return Class.Void; }
        }

        public override bool AsBoolean
        {
            get { return false; }
        }

        public override int AsInt32
        {
            get { return 0; }
        }

        public override BigInteger AsBigInteger
        {
            get { return BigInteger.Zero; }
        }

        public override Rational32 AsRational32
        {
            get { return Rational32.Zero; }
        }

        public override double AsDouble
        {
            get { return 0.0; }
        }

        public override BigDecimal AsBigDecimal
        {
            get { return BigDecimal.Zero; }
        }

        public override object AsNativeObject
        {
            get { return null; }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Empty;
        }

        public override bool IsEmpty()
        {
            return true;
        }
    }
}

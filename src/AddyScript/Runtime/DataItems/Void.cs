using System;
using System.Numerics;

using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Void : DataItem
{
    public static readonly Void Value = new ();

    private Void()
    {
    }

    public override Class Class => Class.Void;

    public override bool AsBoolean => false;

    public override int AsInt32 => 0;

    public override BigInteger AsBigInteger => BigInteger.Zero;

    public override Rational32 AsRational32 => Rational32.Zero;

    public override double AsDouble => 0.0;

    public override BigDecimal AsBigDecimal => BigDecimal.Zero;

    public override object AsNativeObject => null;

    public override string ToString(string format, IFormatProvider formatProvider) => string.Empty;

    public override bool IsEmpty() => true;
}

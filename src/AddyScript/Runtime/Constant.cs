using System;
using System.Numerics;

using SysComplex = System.Numerics.Complex;

using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.NativeTypes;

using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Complex = AddyScript.Runtime.DataItems.Complex;
using Decimal = AddyScript.Runtime.DataItems.Decimal;
using String = AddyScript.Runtime.DataItems.String;


namespace AddyScript.Runtime;


/// <summary>
/// Represents a constant (i.e. a readonly variable).
/// </summary>
public class Constant : IFrameItem
{
    #region Constructors

    /// <summary>
    /// Initializes a constant.
    /// </summary>
    /// <param name="value">The value of the constant</param>
    public Constant(DataItem value)
    {
        Value = value is Boolean or Integer or Long or Rational or Float or Decimal or Complex or Date or Duration or String
              ? value
              : throw new InvalidOperationException(string.Format(Resources.InvalidConstantType, value.Class.Name));
    }

    /// <summary>
    /// Initializes a boolean constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(bool value)
    {
        Value = Boolean.FromBool(value);
    }

    /// <summary>
    /// Initializes an integer constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(int value)
    {
        Value = new Integer(value);
    }

    /// <summary>
    /// Initializes a big integer constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(BigInteger value)
    {
        Value = new Long(value);
    }

    /// <summary>
    /// Initializes a rational constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(Fraction value)
    {
        Value = new Rational(value);
    }

    /// <summary>
    /// Initializes a floatting-point constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(double value)
    {
        Value = new Float(value);
    }

    /// <summary>
    /// Initializes a decimal constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(BigDecimal value)
    {
        Value = new Decimal(value);
    }

    /// <summary>
    /// Initializes a complex constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(SysComplex value)
    {
        Value = new Complex(value);
    }

    /// <summary>
    /// Initializes a date-time constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(DateTime value)
    {
        Value = new Date(value);
    }

    /// <summary>
    /// Initializes a string constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(string value)
    {
        Value = new String(value);
    }

    #endregion

    /// <summary>
    /// Gets the kind of frame item a constant is.
    /// </summary>
    public FrameItemKind Kind => FrameItemKind.Constant;

    /// <summary>
    /// Gets the constant's value.
    /// </summary>
    /// <returns>The embedded <see cref="DataItem"/></returns>
    public DataItem Value { get; }
}
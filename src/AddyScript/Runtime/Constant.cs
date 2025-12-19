using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
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
        Value = value.Class.ClassID switch
        {
            ClassID.Boolean or ClassID.Integer or ClassID.Long or ClassID.Rational or ClassID.Float or
            ClassID.Decimal or ClassID.Complex or ClassID.Date or ClassID.String => value,
            _ => throw new InvalidOperationException(string.Format(Resources.InvalidConstantType, value.Class.Name)),
        };
    }

    /// <summary>
    /// Initializes a boolean constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(bool value)
    {
        this.Value = Boolean.FromBool(value);
    }

    /// <summary>
    /// Initializes an integer constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(int value)
    {
        this.Value = new Integer(value);
    }

    /// <summary>
    /// Initializes a big integer constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(BigInteger value)
    {
        this.Value = new Long(value);
    }

    /// <summary>
    /// Initializes a rational constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(Rational32 value)
    {
        this.Value = new Rational(value);
    }

    /// <summary>
    /// Initializes a floatting-point constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(double value)
    {
        this.Value = new Float(value);
    }

    /// <summary>
    /// Initializes a decimal constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(BigDecimal value)
    {
        this.Value = new Decimal(value);
    }

    /// <summary>
    /// Initializes a complex constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(Complex64 value)
    {
        this.Value = new Complex(value);
    }

    /// <summary>
    /// Initializes a date-time constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(DateTime value)
    {
        this.Value = new Date(value);
    }

    /// <summary>
    /// Initializes a string constant.
    /// </summary>
    /// <param name="value">The constant's value</param>
    public Constant(string value)
    {
        this.Value = new String(value);
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
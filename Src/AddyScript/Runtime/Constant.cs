#region 'using' Directives

using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Properties;
using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.NativeTypes;
using Boolean = AddyScript.Runtime.Dynamics.Boolean;
using Complex = AddyScript.Runtime.Dynamics.Complex;
using Decimal = AddyScript.Runtime.Dynamics.Decimal;
using String = AddyScript.Runtime.Dynamics.String;

#endregion

namespace AddyScript.Runtime
{
    /// <summary>
    /// Represents a constant (i.e. a readonly variable).
    /// </summary>
    public class Constant : IFrameItem
    {
        /// <summary>
        /// Stores the constant's value.<br/>
        /// This must be an immutable object.
        /// </summary>
        private readonly Dynamic value;

        #region Constructors

        /// <summary>
        /// Initializes a constant.
        /// </summary>
        /// <param name="value">The value of the constant</param>
        public Constant(Dynamic value)
        {
            switch (value.Class.ClassID)
            {
                case ClassID.Boolean:
                case ClassID.Integer:
                case ClassID.Long:
                case ClassID.Rational:
                case ClassID.Float:
                case ClassID.Decimal:
                case ClassID.Complex:
                case ClassID.Date:
                case ClassID.String:
                    this.value = value;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(Resources.InvalidConstantType, value.Class.Name));
            }
        }

        /// <summary>
        /// Initializes a boolean constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(bool value)
        {
            this.value = Boolean.FromBool(value);
        }

        /// <summary>
        /// Initializes an integer constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(int value)
        {
            this.value = new Integer(value);
        }

        /// <summary>
        /// Initializes a big integer constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(BigInteger value)
        {
            this.value = new Long(value);
        }

        /// <summary>
        /// Initializes a rational constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(Rational32 value)
        {
            this.value = new Rational(value);
        }

        /// <summary>
        /// Initializes a floatting-point constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(double value)
        {
            this.value = new Float(value);
        }

        /// <summary>
        /// Initializes a decimal constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(BigDecimal value)
        {
            this.value = new Decimal(value);
        }

        /// <summary>
        /// Initializes a complex constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(Complex64 value)
        {
            this.value = new Complex(value);
        }

        /// <summary>
        /// Initializes a date-time constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(DateTime value)
        {
            this.value = new Date(value);
        }

        /// <summary>
        /// Initializes a string constant.
        /// </summary>
        /// <param name="value">The constant's value</param>
        public Constant(string value)
        {
            this.value = new String(value);
        }

        #endregion

        /// <summary>
        /// Gets the kind of this frame's item.
        /// </summary>
        public FrameItemKind Kind
        {
            get { return FrameItemKind.Constant; }
        }

        /// <summary>
        /// Gets the constant's value.
        /// </summary>
        /// <returns>The embedded <see cref="Dynamic"/></returns>
        public Dynamic GetValue()
        {
            return value;
        }
    }
}
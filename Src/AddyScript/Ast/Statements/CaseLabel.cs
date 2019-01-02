using AddyScript.Runtime.Dynamics;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// A <b>case</b> label in a <b>switch</b> block.
    /// </summary>
    public class CaseLabel : Label
    {
        /// <summary>
        /// Initializes a new instance of CaseLabel.
        /// </summary>
        /// <param name="address">The address of the statement that follows the case.</param>
        /// <param name="value">The value that follows the 'case' keyword in the script.</param>
        public CaseLabel(int address, Dynamic value)
            : base(address)
        {
            Value = value;
        }

        /// <summary>
        /// The value that follows the <b>case</b> keyword in the script.
        /// </summary>
        public Dynamic Value { get; private set; }

        #region Overrides

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is CaseLabel) && Value == ((CaseLabel) obj).Value;
        }

        #endregion
    }
}
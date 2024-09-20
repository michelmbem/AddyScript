using AddyScript.Runtime.DataItems;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// A <b>case</b> label in a <b>switch</b> block.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of CaseLabel.
    /// </remarks>
    /// <param name="address">The address of the statement that follows the case.</param>
    /// <param name="value">The value that follows the 'case' keyword in the script.</param>
    public class CaseLabel(int address, DataItem value) : Label(address)
    {

        /// <summary>
        /// The value that follows the <b>case</b> keyword in the script.
        /// </summary>
        public DataItem Value { get; private set; } = value;

        #region Overrides

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is CaseLabel label) && Value.Equals(label.Value);
        }

        #endregion
    }
}
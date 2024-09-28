using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// The base class of all expression that can be accepted as lValue in an assignment.
    /// </summary>
    public abstract class Reference : Expression
    {
        public abstract void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue);
    }
}

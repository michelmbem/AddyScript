using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// The common interface of all expression that can be accepted as lValue in an assignment.
    /// </summary>
    public interface IReference
    {
        void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue);
    }
}

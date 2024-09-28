using AddyScript.Ast.Expressions;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Translators;


public interface IAssignmentProcessor
{
    void AssignToVariable(VariableRef varRef, DataItem rValue);
    
    void AssignToItem(ItemRef itemRef, DataItem rValue);
    
    void AssignToProperty(PropertyRef propertyRef, DataItem rValue);

    void AssignToStaticProperty(StaticPropertyRef staticRef, DataItem rValue);

    void AssignToParentItem(ParentIndexerRef pir, DataItem rValue);

    void AssignToParentProperty(ParentPropertyRef ppr, DataItem rValue);
}

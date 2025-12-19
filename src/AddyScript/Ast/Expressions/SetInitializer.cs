using System;
using System.Collections.Generic;
using System.Linq;
using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a set's initializer: a set of item initializer into braces.
/// </summary>
/// <remarks>
/// Initializes a new instance of SetInitializer. Can be used as lvalue to deconstruct an object
/// and initialize multiple values at once with its properties.
/// </remarks>
/// <param name="items">The <see cref="Argument"/>s that are listed between the delimiters</param>
public class SetInitializer(params Argument[] items) : SequenceInitializer(items), IReference
{
    /// <summary>
    /// Operates assignment to this reference.
    /// Handles object destructuring.
    /// </summary>
    /// <param name="processor">The assignment processor to use</param>
    /// <param name="rValue">The value that should be assigned to this reference</param>
    public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
    {
        if (processor is not ITranslator translator) return;

        if (Items.Length == 0)
            throw new InvalidOperationException(Resources.ListCantBeEmpty);

        var parentObj = new Literal(rValue);
        string collectorName = null;
        HashSet<string> excludedMembers = ["type"];

        foreach (var item in Items)
        {
            switch (item)
            {
                case {Spread: true, Expression: VariableRef varRef}
                when collectorName == null:
                    collectorName = varRef.Name;
                    break;
                case {Spread: true}:
                    throw new InvalidOperationException(Resources.NotAReference);
                case {Expression: VariableRef varRef}:
                    excludedMembers.Add(varRef.Name);
                    new Assignment(varRef, new PropertyRef(parentObj, varRef.Name))
                        .AcceptTranslator(translator);
                    break;
                case {Expression: Assignment
                {
                    Operator: BinaryOperator.None,
                    RightOperand: VariableRef varRef
                } assignment}:
                    excludedMembers.Add(varRef.Name);
                    new Assignment(
                        assignment.LeftOperand,
                        new Assignment(varRef, new PropertyRef(parentObj, varRef.Name)))
                        .AcceptTranslator(translator);
                    break;
                case {Expression: Assignment
                {
                    Operator: BinaryOperator.None,
                    LeftOperand: VariableRef varRef
                } assignment}:
                    excludedMembers.Add(varRef.Name);
                    new Assignment(
                            varRef,
                            new TernaryExpression(
                                new TypeVerification(new PropertyRef(parentObj, varRef.Name), Class.Void.Name),
                                assignment.RightOperand,
                                new PropertyRef(parentObj, varRef.Name)))
                        .AcceptTranslator(translator);
                    break;
                default:
                    throw new InvalidOperationException(Resources.NotAReference);
            }
        }

        if (collectorName == null) return;

        var dataMembers = rValue.Class.GetMembers(MemberKind.Field | MemberKind.Property);

        excludedMembers.UnionWith(dataMembers.Where(m =>
                m.Scope != Scope.Public ||
                m.Modifier is not (Modifier.Default or Modifier.Final) ||
                m is ClassProperty {CanRead: false})
            .Select(m => m.Name)
            .ToHashSet());

        var setters = dataMembers.Where(m =>
                m.Scope == Scope.Public &&
                m.Modifier is Modifier.Default or Modifier.Final &&
                m is ClassField or ClassProperty {CanRead: true} &&
                !excludedMembers.Contains(m.Name))
            .Select(m => new VariableSetter(m.Name, new PropertyRef(parentObj, m.Name)))
            .ToList();

        setters.AddRange([.. rValue.AsDynamicObject
            .Where(pair => rValue.Class.GetField(pair.Key) == null && !excludedMembers.Contains(pair.Key))
            .Select(pair => new VariableSetter(pair.Key, new Literal(pair.Value)))]);

        new Assignment(new VariableRef(collectorName), new ObjectInitializer([.. setters]))
            .AcceptTranslator(translator);
    }
    
    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateSetInitializer(this);
    }
}
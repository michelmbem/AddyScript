using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using AddyScript.Ast.Statements;
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;

using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Object = AddyScript.Runtime.DataItems.Object;
using String = AddyScript.Runtime.DataItems.String;
using Void = AddyScript.Runtime.DataItems.Void;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a pattern to match in a <see cref="PatternMatching"/> expression.
/// </summary>
public abstract class Pattern : ScriptElement
{
    /// <summary>
    /// Gets the <see cref="Expression"/> that will be evaluated to tell whether this <see cref="Pattern"/>
    /// matches the value of <paramref name="arg"/> or not.
    /// </summary>
    /// <param name="arg">The value to match against this <see cref="Pattern"/></param>
    /// <returns>An <see cref="Expression"/> that evaluates to <b>true</b> or <b>false</b></returns>
    public abstract Expression GetMatchTest(Expression arg);
    
    /// <summary>
    /// Gets the <see cref="Statement"/> that extracts data from the value of <paramref name="arg"/>
    /// if this <see cref="Pattern"/> matches it.
    /// </summary>
    /// <param name="arg">The expression to extract data from</param>
    /// <returns>A <see cref="Statement"/> that performs the extraction, or <b>null</b> if no extraction is needed</returns>
    public virtual Statement GetExtractionAction(Expression arg) => null;
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that always matches any value.
/// </summary>
public class AlwaysTruePattern : Pattern
{
    /// <summary>
    /// The symbol that indicates that <see cref="AlwaysTruePattern"/> is used.
    /// </summary>
    public const string Symbol = "_";

    public override Expression GetMatchTest(Expression arg) => new Literal(Boolean.True);
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match satisfies a relational operation.
/// </summary>
/// <param name="_operator">The relational operator to use for the comparison</param>
/// <param name="value">The value to compare against</param>
public class RelationalPattern(BinaryOperator _operator, DataItem value) : Pattern
{
    /// <summary>
    /// The relational operator to use for the comparison.
    /// </summary>
    public BinaryOperator Operator => _operator;

    /// <summary>
    /// The value to compare against.
    /// </summary>
    public DataItem Value => value;

    public override Expression GetMatchTest(Expression arg) =>
        new BinaryExpression(_operator, arg, new Literal(value));
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match satisfies a regex pattern.
/// </summary>
/// <param name="value">The regex pattern to match against</param>
public class RegexPattern(DataItem value) : RelationalPattern(BinaryOperator.Matches, value)
{
    public override Expression GetMatchTest(Expression arg) =>
        new BinaryExpression(BinaryOperator.AndAlso,
                             new TypeVerification(arg, Class.String.Name),
                             base.GetMatchTest(arg));
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match is of a particular type.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="TypePattern"/>.
/// </remarks>
/// <param name="typeName">The name of the type values should be for this <see cref="Pattern"/> to match them</param>
public class TypePattern(string typeName) : Pattern
{
    /// <summary>
    /// The name of the type values should be for this <see cref="Pattern"/> to match them.
    /// </summary>
    public string TypeName => typeName;

    public override Expression GetMatchTest(Expression arg) => string.IsNullOrWhiteSpace(typeName)
        ? new UnaryExpression(UnaryOperator.Not, new TypeVerification(arg, Class.Void.Name))
        : new TypeVerification(arg, typeName);
}

/// <summary>
/// A pattern that matches a property of an object against another pattern.
/// </summary>
/// <param name="path">The path to the property to match</param>
/// <param name="pattern">The pattern to match the property against</param>
public class PropertyMatcher(string[] path, Pattern pattern) : ScriptElement
{
    /// <summary>
    /// The path to the property to match.
    /// </summary>
    public string[] Path => path;

    /// <summary>
    /// The pattern to match the property against.
    /// </summary>
    public Pattern Pattern => pattern;

    /// <summary>
    /// Gets the <see cref="Expression"/> that will be evaluated to tell whether the property
    /// of <paramref name="ownerRef"/> matches the pattern or not.
    /// </summary>
    /// <param name="ownerRef">The expression representing the object owning the property</param>
    /// <returns>>An <see cref="Expression"/> that evaluates to <b>true</b> or <b>false</b></returns>
    public Expression GetMatchTest(Expression ownerRef)
    {
        var propRef = path.Aggregate(ownerRef, (current, next) => new PropertyRef(current, next));
        var propIsVoid = new TypeVerification(propRef, Class.Void.Name);
        var propIsNotVoid = new UnaryExpression(UnaryOperator.Not, propIsVoid);
        return new BinaryExpression(BinaryOperator.AndAlso, propIsNotVoid, pattern.GetMatchTest(propRef));
    }
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match is of the same type than a particular object
/// and have properties that matches the given patterns.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ObjectPattern"/>.
/// </remarks>
/// <param name="typeName">The name of the type values should be for this <see cref="Pattern"/> to match them</param>
/// <param name="propertyMatchers">A set of patterns that some properties of the given object should match</param>
public class ObjectPattern(string typeName, PropertyMatcher[] propertyMatchers) : TypePattern(typeName)
{
    /// <summary>
    /// A set of patterns that some properties of the given object should match.
    /// </summary>
    public PropertyMatcher[] PropertyMatchers => propertyMatchers;

    public override Expression GetMatchTest(Expression arg) =>
        propertyMatchers.Aggregate(base.GetMatchTest(arg), (current, next) =>
            new BinaryExpression(BinaryOperator.AndAlso, current, next.GetMatchTest(arg)));
}


public class DestructuringPattern(string typeName, string[] propertyNames) : TypePattern(typeName)
{
    public string[] PropertyNames => propertyNames;

    public override Expression GetMatchTest(Expression arg) =>
        propertyNames.Select(name => new TypeVerification(new PropertyRef(arg, name), Class.Void.Name))
                     .Select(test => new UnaryExpression(UnaryOperator.Not, test))
                     .Aggregate(base.GetMatchTest(arg), (current, next) =>
                        new BinaryExpression(BinaryOperator.AndAlso, current, next));

    public override Statement GetExtractionAction(Expression arg) =>
        new Assignment(new SetInitializer([.. propertyNames.Select(name => new Argument(new VariableRef(name)))]),
                       arg);
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that wraps other patterns into parentheses.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="GroupingPattern"/>.
/// </remarks>
/// <param name="child">The pattern wrapped by this <see cref="GroupingPattern"/></param>
public class GroupingPattern(Pattern child) : Pattern
{
    /// <summary>
    /// The pattern wrapped by this <see cref="GroupingPattern"/>.
    /// </summary>
    public Pattern Child => child;

    public override Expression GetMatchTest(Expression arg) => child.GetMatchTest(arg);
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that negates its child patterns.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="NegativePattern"/>.
/// </remarks>
/// <param name="child">The pattern to negate</param>
public class NegativePattern(Pattern child) : GroupingPattern(child)
{
    public override Expression GetMatchTest(Expression arg) =>
        new UnaryExpression(UnaryOperator.Not, child.GetMatchTest(arg));
}


/// <summary>
/// A subclass of <see cref="Pattern"/> made of 2 child patterns.
/// It checks that the value to match satisfies either one or both of its children.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="LogicalPattern"/>.
/// </remarks>
/// <param name="inclusive">Controls how children are combined</param>
/// <param name="left">The first child pattern</param>
/// <param name="right">The second child pattern</param>
public class LogicalPattern(bool inclusive, Pattern left, Pattern right) : Pattern
{
    /// <summary>
    /// Controls how children are combined.
    /// </summary>
    public bool Inclusive => inclusive;

    /// <summary>
    /// The first child of this <see cref="LogicalPattern"/>.
    /// </summary>
    public Pattern Left => left;

    /// <summary>
    /// The second child of this <see cref="LogicalPattern"/>.
    /// </summary>
    public Pattern Right => right;

    public override Expression GetMatchTest(Expression arg) =>
        new BinaryExpression(inclusive ? BinaryOperator.AndAlso : BinaryOperator.OrElse,
                             left.GetMatchTest(arg),
                             right.GetMatchTest(arg));
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that matches a positional collection against a set of patterns.
/// It checks that the value to match is an iterable collection with the same number of items as
/// the number of patterns, and that each item matches the corresponding pattern.
/// </summary>
/// <param name="items">The patterns to match each item of the collection</param>
public class PositionalPattern(params Pattern[] items) : Pattern
{
    /// <summary>
    /// An inner function that checks if its argument is iterable.
    /// </summary>
    private static readonly InnerFunction IsIterable = new ("isIterable", [new ("arg")], IsIterableLogic);
    
    /// <summary>
    /// The patterns to match each item of the collection.
    /// </summary>
    public Pattern[] Items => items;

    public override Expression GetMatchTest(Expression arg)
    {
        var tupleRef = new VariableRef("__" + Guid.NewGuid().ToString("N"));
        var tupleAssignment = new Assignment(tupleRef, new TupleInitializer(new Argument(arg, true)));
        var initialTest = new BinaryExpression(BinaryOperator.AndAlso,
                                               new InnerFunctionCall(IsIterable, arg),
                                               new BinaryExpression(BinaryOperator.Equal,
                                                                    new PropertyRef(tupleAssignment, "size"),
                                                                    new Literal(new Integer(items.Length))));
        
        return items.Select((item, index) => item.GetMatchTest(new ItemRef(tupleRef, new Literal(new Integer(index)))))
                    .Aggregate(initialTest, (current, next) => new BinaryExpression(BinaryOperator.AndAlso, current, next));
    }

    /// <summary>
    /// The logic of the inner function <see cref="IsIterable"/>.
    /// </summary>
    /// <param name="arguments">The arguments passed to the function</param>
    /// <returns>A <see cref="Boolean"/> indicating whether the argument is iterable</returns>
    private static DataItem IsIterableLogic(DataItem[] arguments)
    {
        DataItem arg = arguments[0];
        return Boolean.FromBool(arg.Class.IsSequential ||
                                arg is Resource { AsNativeObject: IEnumerable } ||
                                HasIterator(arg.Class)); 
    }

    /// <summary>
    /// Determines whether the given class has iterator methods.
    /// </summary>
    /// <param name="klass">The class to check</param>
    /// <returns>><b>true</b> if the class has iterator methods; otherwise, <b>false</b></returns>
    private static bool HasIterator(Class klass)
    {
        var iteratorMethod = klass.GetMethod("iterator");
        if (isIteratorMethod(iteratorMethod)) return true;
        
        var moveFirstMethod = klass.GetMethod("moveFirst");
        var hasNextMethod = klass.GetMethod("hasNext");
        var moveNextMethod = klass.GetMethod("moveNext");
        return isIteratorMethod(moveFirstMethod) && isIteratorMethod(hasNextMethod) && isIteratorMethod(moveNextMethod);
    }
    
    /// <summary>
    /// Checks whether the given method is a valid iterator method.
    /// </summary>
    /// <param name="method">The method to check</param>
    /// <returns>><b>true</b> if the method is a valid iterator method; otherwise, <b>false</b></returns>
    private static bool isIteratorMethod(ClassMethod method) =>
        method is { Scope: Scope.Public, Modifier: Modifier.Default or Modifier.Final, Function.Parameters.Length: 0 };
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match satisfies a regex pattern and extracts variables from it.
/// </summary>
/// <param name="regex">The regex pattern to match against</param>
/// <param name="variableNames">The names of the variables to extract from the matched groups</param>
public class StringDestructuringPattern(Regex regex, string[] variableNames) : Pattern
{
    /// <summary>
    /// An inner function that checks if its argument matches a regex and extracts variables from it.
    /// </summary>
    private static readonly InnerFunction IsMatch = new ("isMatch", [new ("arg"), new ("regex"), new ("varNames")], IsMatchLogic);
    
    /// <summary>
    /// The regex pattern to match against.
    /// </summary>
    public Regex Regex => regex;

    /// <summary>
    /// The names of the variables to extract from the matched groups.
    /// </summary>
    public string[] VariableNames => variableNames;

    /// <summary>
    /// The <see cref="Object"/> extracted by the last call to <see cref="IsMatch"/>.
    /// </summary>
    private static Object LastExtracted { get; set; }
    
    public override Expression GetMatchTest(Expression arg) =>
        new BinaryExpression(BinaryOperator.AndAlso,
                             new TypeVerification(arg, Class.String.Name),
                             new InnerFunctionCall(IsMatch,
                                                   arg,
                                                   new Literal(new Resource(regex)),
                                                   new Literal(new Resource(variableNames))));
    
    public override Statement GetExtractionAction(Expression arg) =>
        new Assignment(
            new SetInitializer([.. variableNames.Select(name => new Argument(new VariableRef(name)))]),
            new Literal(LastExtracted));

    private static DataItem IsMatchLogic(DataItem[] arguments)
    {
        var arg = arguments[0];
        if (!arg.InstanceOf(Class.String)) return Boolean.False;

        var regex = (Regex)arguments[1].AsNativeObject;
        var match = regex.Match(arg.ToString());
        if (!match.Success) return Boolean.False;

        var variableNames = (string[])arguments[2].AsNativeObject;
        var extractedFields = new Dictionary<string, DataItem>();
        foreach (var variableName in variableNames)
        {
            var group = match.Groups[variableName];
            extractedFields.Add(variableName, group.Success ? new String(group.Value) : Void.Value);
        }

        LastExtracted = new Object(extractedFields);
        return Boolean.True;
    }
}
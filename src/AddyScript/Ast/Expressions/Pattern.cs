using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;


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
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that always matches any value.
/// </summary>
public class AlwaysPattern : Pattern
{
    /// <summary>
    /// The symbol that indicates that <see cref="AlwaysPattern"/> is used.
    /// </summary>
    public const string Symbol = "_";

    public override Expression GetMatchTest(Expression arg) => new Literal(Boolean.True);
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match is null.
/// </summary>
public class NullPattern : Pattern
{
    /// <summary>
    /// The symbol that indicates that <see cref="NullPattern"/> is used.
    /// </summary>
    public const string Symbol = "null";

    public override Expression GetMatchTest(Expression arg) =>
        new BinaryExpression(BinaryOperator.Identical, arg, new Literal());
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match is equal to an initially fixed value.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ValuePattern"/>.
/// </remarks>
/// <param name="value">The value this <see cref="Pattern"/> compares other values to in order to match them</param>
public class ValuePattern(DataItem value) : Pattern
{
    /// <summary>
    /// The value this <see cref="Pattern"/> compares other values to in order to match them.
    /// </summary>
    public DataItem Value => value;

    public override Expression GetMatchTest(Expression arg) =>
        new BinaryExpression(BinaryOperator.Equal, arg, new Literal(value));
}


/// <summary>
/// A subclass of <see cref="Pattern"/> that checks if the value to match is in a particular range.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="RangePattern"/>.
/// </remarks>
/// <param name="lowerBound">
/// The lower bound of the range in which values must be for this <see cref="Pattern"/> to match them
/// </param>
/// <param name="upperBound">
/// The upper bound of the range in which values must be for this <see cref="Pattern"/> to match them.
/// </param>
public class RangePattern(DataItem lowerBound, DataItem upperBound) : Pattern
{
    /// <summary>
    /// The lower bound of the range in which values must be for this <see cref="Pattern"/> to match them.
    /// </summary>
    public DataItem LowerBound => lowerBound;

    /// <summary>
    /// The upper bound of the range in which values must be for this <see cref="Pattern"/> to match them.
    /// </summary>
    public DataItem UpperBound => upperBound;

    public override Expression GetMatchTest(Expression arg)
    {
        var lowerBoundCheck = lowerBound != null
            ? new BinaryExpression(BinaryOperator.GreaterThanOrEqual, arg, new Literal(lowerBound))
            : null;

        var upperBoundCheck = upperBound != null
            ? new BinaryExpression(BinaryOperator.LessThanOrEqual, arg, new Literal(upperBound))
            : null;

        // Assuming both lowerBound and upperBound cannot be null at the same time!
        if (lowerBoundCheck == null) return upperBoundCheck!;
        if (upperBoundCheck == null) return lowerBoundCheck;
        return new BinaryExpression(BinaryOperator.AndAlso, lowerBoundCheck, upperBoundCheck);
    }
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

public class PropertyMatcher(string propertyName, Pattern pattern) : ScriptElement
{
    public string PropertyName => propertyName;

    public Pattern Pattern => pattern;

    public Expression GetMatchTest(Expression ownerRef)
    {
        var propRef = new PropertyRef(ownerRef, propertyName);
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

    public override Expression GetMatchTest(Expression arg)
    {
        Expression matchTest = base.GetMatchTest(arg);

        foreach (var matcher in propertyMatchers)
            matchTest = new BinaryExpression(BinaryOperator.AndAlso, matchTest, matcher.GetMatchTest(arg));

        return matchTest;
    }
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
/// Initializes a new instance of <see cref="CompositePattern"/>.
/// </remarks>
/// <param name="inclusive">Controls how children are combined</param>
/// <param name="left">The first child pattern</param>
/// <param name="right">The second child pattern</param>
public class CompositePattern(bool inclusive, Pattern left, Pattern right) : Pattern
{
    /// <summary>
    /// Controls how children are combined.
    /// </summary>
    public bool Inclusive => inclusive;

    /// <summary>
    /// The first child of this <see cref="CompositePattern"/>.
    /// </summary>
    public Pattern Left => left;

    /// <summary>
    /// The second child of this <see cref="CompositePattern"/>.
    /// </summary>
    public Pattern Right => right;

    public override Expression GetMatchTest(Expression arg) => inclusive
        ? new BinaryExpression(BinaryOperator.AndAlso, left.GetMatchTest(arg), right.GetMatchTest(arg))
        : new BinaryExpression(BinaryOperator.OrElse, left.GetMatchTest(arg), right.GetMatchTest(arg));
}
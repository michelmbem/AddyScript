using AddyScript.Ast.Statements;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Ast.Expressions
{
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

        public override Expression GetMatchTest(Expression arg)
        {
            return new BinaryExpression(BinaryOperator.Identical, arg, new Literal());
        }
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

        public override Expression GetMatchTest(Expression arg)
        {
            return new BinaryExpression(BinaryOperator.Equal, arg, new Literal(value));
        }
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
            if (lowerBoundCheck == null) return upperBoundCheck;
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

        public override Expression GetMatchTest(Expression arg)
        {
            return new TypeVerification(arg, typeName);
        }
    }


    /// <summary>
    /// A subclass of <see cref="Pattern"/> that checks if the value to match is of the same type than a particular object
    /// and have equal values for some properties.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="ObjectPattern"/>.
    /// </remarks>
    /// <param name="typeName">The name of the type values should be for this <see cref="Pattern"/> to match them</param>
    /// <param name="example">An object that will be compared property by property to any object to be matched</param>
    public class ObjectPattern(string typeName, DataItem example) : TypePattern(typeName)
    {
        /// <summary>
        /// An object that will be compared property by property to any object to be matched.
        /// </summary>
        public DataItem Example => example;

        public override Expression GetMatchTest(Expression arg)
        {
            Expression matchTest = base.GetMatchTest(arg);

            foreach (var exampleProp in example.AsDynamicObject)
            {
                var propRef = new PropertyRef(arg, exampleProp.Key);
                var voidCheck = new UnaryExpression(UnaryOperator.Not, new TypeVerification(propRef, "void"));
                var propCmp = new BinaryExpression(BinaryOperator.Equal, propRef, new Literal(exampleProp.Value));
                var propTest = new BinaryExpression(BinaryOperator.AndAlso, voidCheck, propCmp);

                matchTest = new BinaryExpression(BinaryOperator.AndAlso, matchTest, propTest);
            }

            return matchTest;
        }
    }


    /// <summary>
    /// A subclass of <see cref="Pattern"/> that uses a predicate to match values.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="PredicatePattern"/>.
    /// </remarks>
    /// <param name="parameterName">
    /// The name of the parameter of the predicate that this <see cref="Pattern"/> invokes to match a value
    /// </param>
    /// <param name="predicate">The predicate that this <see cref="Pattern"/> invokes to match a value</param>
    public class PredicatePattern(string parameterName, Expression predicate) : Pattern
    {
        /// <summary>
        /// The name of the parameter of the predicate that this <see cref="Pattern"/> invokes to match a value.<br/>
        /// It is basically an alias for the value that is being tested within the predicate's body.
        /// </summary>
        public string ParameterName => parameterName;

        /// <summary>
        /// The predicate that this <see cref="Pattern"/> invokes to match a value.
        /// </summary>
        public Expression Predicate => predicate;

        public override Expression GetMatchTest(Expression arg)
        {
            var parameter = new ParameterDecl(parameterName, false, false, null);
            var inlineFn = new InlineFunction([parameter], Block.Return(predicate));
            return new AnonymousCall(inlineFn, [arg], null);
        }
    }


    /// <summary>
    /// A subclass of <see cref="Pattern"/> made of a collection of child patterns.
    /// It checks that the value to match satisfies at least one of its children.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="CompositePattern"/>.
    /// </remarks>
    /// <param name="components"></param>
    public class CompositePattern(params Pattern[] components) : Pattern
    {
        /// <summary>
        /// The children of this <see cref="CompositePattern"/>.
        /// </summary>
        public Pattern[] Components => components;

        public override Expression GetMatchTest(Expression arg)
        {
            Expression matchTest = components[0].GetMatchTest(arg);

            for (int i = 1; i < components.Length; ++i)
                matchTest = new BinaryExpression(BinaryOperator.OrElse, matchTest, components[i].GetMatchTest(arg));

            return matchTest;
        }
    }
}

using System.Collections.Generic;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// An representation of a call to a function or method with named optional arguments.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of CallWithNamedArgs
    /// </remarks>
    /// <param name="positionalArgs">The list of positional arguments passed to the function or method</param>
    /// <param name="namedArgs">The collection of named arguments passed to the function or method</param>
    public abstract class CallWithNamedArgs(Expression[] positionalArgs, Dictionary<string, Expression> namedArgs)
        : Call(positionalArgs)
    {

        /// <summary>
        /// Represents the collection of named arguments passed to the function or method.
        /// </summary>
        public Dictionary<string, Expression> NamedArgs { get; private set; } = namedArgs;
    }
}
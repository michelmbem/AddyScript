using System.Collections.Generic;
using System.Linq;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to a static method.<br/>
    /// May also match an instance method.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of StaticMethodCall
    /// </remarks>
    /// <param name="name">The qualified method's name</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the method</param>
    /// <param name="namedArgs">The collection of named arguments passed to the method</param>
    public class StaticMethodCall(QualifiedName name, ListItem[] positionalArgs, Dictionary<string, Expression> namedArgs)
        : CallWithNamedArgs(positionalArgs, namedArgs)
    {

        /// <summary>
        /// Initializes a new instance of StaticMethodCall
        /// </summary>
        /// <param name="name">The qualified method's name</param>
        /// <param name="arguments">The arguments passed to the method</param>
        public StaticMethodCall(QualifiedName name, params Expression[] arguments)
            : this(name, ToListItems(arguments), null)
        {
        }

        /// <summary>
        /// The qualified method's name.
        /// </summary>
        public QualifiedName Name => name;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateStaticMethodCall(this);
        }
    }
}
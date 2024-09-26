using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to a constructor.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ConstructorCall
    /// </remarks>
    /// <param name="name">The name of the class to instanciate</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the constructor</param>
    /// <param name="namedArgs">The collection of named arguments passed to the constructor</param>
    /// <param name="initializers">A set of property initializers for the new object</param>
    public class ConstructorCall(QualifiedName name, Expression[] positionalArgs,
                                 Dictionary<string, Expression> namedArgs = null,
                                 PropertyInitializer[] initializers = null)
        : StaticMethodCall(name, positionalArgs, namedArgs)
    {

        /// <summary>
        /// A set of property initializers for the newly created object.
        /// </summary>
        public PropertyInitializer[] PropertyInitializers { get; private set; } = initializers;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateConstructorCall(this);
        }
    }
}
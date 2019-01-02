using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    public class PropertyRef : Expression
    {
        /// <summary>
        /// Initializes a new instance of PropertyRef
        /// </summary>
        /// <param name="owner">The object to which the property belongs</param>
        /// <param name="propertyName">The property's name</param>
        public PropertyRef(Expression owner, string propertyName)
        {
            Owner = owner;
            PropertyName = propertyName;
        }

        /// <summary>
        /// The object to which this field belongs.
        /// </summary>
        public Expression Owner { get; private set; }

        /// <summary>
        /// The field name.
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// A factory method to quickly create instances of <see cref="PropertyRef"/>
        /// where the owner is always the keyword <i>this</i>.
        /// </summary>
        /// <param name="propertyName">The property's name</param>
        /// <returns>A <see cref="PropertyRef"/></returns>
        public static PropertyRef This(string propertyName)
        {
            return new PropertyRef(new ThisReference(), propertyName);
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompilePropertyRef(this);
        }
    }
}
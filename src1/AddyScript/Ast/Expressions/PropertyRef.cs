using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of PropertyRef
    /// </remarks>
    /// <param name="owner">The object to which the property belongs</param>
    /// <param name="propertyName">The property's name</param>
    public class PropertyRef(Expression owner, string propertyName) : Expression
    {

        /// <summary>
        /// The object to which this field belongs.
        /// </summary>
        public Expression Owner { get; private set; } = owner;

        /// <summary>
        /// The property's name.
        /// </summary>
        public string PropertyName { get; private set; } = propertyName;

        /// <summary>
        /// Determines whether to stop null reference propagation or not.
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// A factory method to quickly create instances of <see cref="PropertyRef"/>
        /// where the owner is always the keyword <i>this</i>.
        /// </summary>
        /// <param name="propertyName">The property's name</param>
        /// <returns>A <see cref="PropertyRef"/></returns>
        public static PropertyRef This(string propertyName)
        {
            return new PropertyRef(new SelfReference(), propertyName);
        }

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslatePropertyRef(this);
        }
    }
}
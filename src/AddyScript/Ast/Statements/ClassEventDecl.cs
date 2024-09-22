using System.Linq;

using AddyScript.Runtime.OOP;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of class event.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ClassEventDecl.
    /// </remarks>
    /// <param name="name">The event's name</param>
    /// <param name="scope">The scope of this event</param>
    /// <param name="modifier">Determines whether this event is final, static or not</param>
    /// <param name="parameters">The signature of any closure that could be used to handle this event</param>
    public class ClassEventDecl(string name, Scope scope, Modifier modifier, ParameterDecl[] parameters)
        : ClassMemberDecl(name, scope, modifier)
    {

        /// <summary>
        /// The signature of any closure that could be used to handle this event.
        /// </summary>
        public ParameterDecl[] Parameters { get; private set; } = parameters;

        /// <summary>
        /// Creates a <see cref="ClassMember"/> from this instance.
        /// </summary>
        public override ClassMember ToClassMember()
        {
            return new ClassEvent(Name, Scope, Modifier, Parameters.Select(p => p.ToParameter()).ToArray());
        }
    }
}

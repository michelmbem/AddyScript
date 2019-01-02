using System.Collections.Generic;

using AddyScript.Compilers;
using AddyScript.Runtime;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the definition of a class.
    /// </summary>
    public class ClassDefinition : StatementWithAttributes
    {
        /// <summary>
        /// Initializes a new instance of ClassDefinition
        /// </summary>
        /// <param name="className">The name of the class to be created</param>
        /// <param name="superClassName">The name of an eventual super class</param>
        /// <param name="modifier">Determines how the class supports inheritance and member access</param>
        /// <param name="constructor">The constructor of the new class</param>
        /// <param name="fields">The fields of the new class</param>
        /// <param name="properties">The properties of the new class</param>
        /// <param name="methods">The methods of the new class</param>
        /// <param name="events">The events of the new class</param>
        public ClassDefinition(string className, string superClassName, Modifier modifier,
                               ClassMethod constructor, ClassField[] fields,
                               ClassProperty[] properties, ClassMethod[] methods,
                               ClassEvent[] events)
        {
            ClassName = className;
            SuperClassName = superClassName;
            Modifier = modifier;
            Constructor = constructor;
            Fields = fields;
            Properties = properties;
            Methods = methods;
            Events = events;
        }

        /// <summary>
        /// The class's name
        /// </summary>
        public string ClassName { get; private set; }

        /// <summary>
        /// The superclass's name
        /// </summary>
        public string SuperClassName { get; private set; }

        /// <summary>
        /// The constructor of the class.
        /// </summary>
        public ClassMethod Constructor { get; private set; }

        /// <summary>
        /// Determines how the class supports inheritance and member access.
        /// </summary>
        public Modifier Modifier { get; private set; }

        /// <summary>
        /// The fields of the class.
        /// </summary>
        public ClassField[] Fields { get; private set; }

        /// <summary>
        /// The properties of the class.
        /// </summary>
        public ClassProperty[] Properties { get; private set; }

        /// <summary>
        /// The methods of the class.
        /// </summary>
        public ClassMethod[] Methods { get; private set; }

        /// <summary>
        /// The events of the class.
        /// </summary>
        public ClassEvent[] Events { get; private set; }

        /// <summary>
        /// Gets an array of all the members of the class
        /// </summary>
        /// <param name="kind">The kind of member to get</param>
        /// <returns>An array of <see cref="ClassMember"/>s</returns>
        public ClassMember[] GetMembers(MemberKind kind)
        {
            var members = new List<ClassMember>();

            if ((kind & MemberKind.Constructor) != MemberKind.None)
                members.Add(Constructor);

            if ((kind & MemberKind.Field) != MemberKind.None)
                members.AddRange(Fields);

            if ((kind & MemberKind.Property) != MemberKind.None)
                members.AddRange(Properties);

            if ((kind & MemberKind.Method) != MemberKind.None)
                members.AddRange(Methods);

            if ((kind & MemberKind.Event) != MemberKind.None)
                members.AddRange(Events);

            return members.ToArray();
        }

        /// <summary>
        /// Gets an array of all the members declared in a class
        /// </summary>
        /// <returns>An array of <see cref="ClassMember"/>s</returns>
        public ClassMember[] GetMembers()
        {
            return GetMembers(MemberKind.All);
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileClassDefinition(this);
        }
    }
}
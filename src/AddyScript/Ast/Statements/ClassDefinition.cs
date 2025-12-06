using System.Collections.Generic;

using AddyScript.Translators;
using AddyScript.Runtime.OOP;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the definition of a class.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ClassDefinition
    /// </remarks>
    /// <param name="className">The name of the class to be created</param>
    /// <param name="superClassName">The name of an eventual super class</param>
    /// <param name="modifier">Determines how the class supports inheritance and member access</param>
    /// <param name="constructor">The constructor of the new class</param>
    /// <param name="indexer">The indexer of the new class</param>
    /// <param name="properties">The properties of the new class</param>
    /// <param name="methods">The methods of the new class</param>
    /// <param name="events">The events of the new class</param>
    public class ClassDefinition(string className, string superClassName, Modifier modifier,
                                 ClassMethodDecl constructor, ClassPropertyDecl indexer,
                                 ClassFieldDecl[] fields, ClassPropertyDecl[] properties,
                                 ClassMethodDecl[] methods, ClassEventDecl[] events) : StatementWithAttributes
    {

        /// <summary>
        /// The class's name
        /// </summary>
        public string ClassName => className;

        /// <summary>
        /// The superclass's name
        /// </summary>
        public string SuperClassName => superClassName;

        /// <summary>
        /// Determines how the class supports inheritance and member access.
        /// </summary>
        public Modifier Modifier => modifier;

        /// <summary>
        /// The constructor of the class.
        /// </summary>
        public ClassMethodDecl Constructor => constructor;

        /// <summary>
        /// The indexer of the class.
        /// </summary>
        public ClassPropertyDecl Indexer => indexer;

        /// <summary>
        /// The fields of the class.
        /// </summary>
        public ClassFieldDecl[] Fields => fields;

        /// <summary>
        /// The properties of the class.
        /// </summary>
        public ClassPropertyDecl[] Properties => properties;

        /// <summary>
        /// The methods of the class.
        /// </summary>
        public ClassMethodDecl[] Methods => methods;

        /// <summary>
        /// The events of the class.
        /// </summary>
        public ClassEventDecl[] Events => events;

        /// <summary>
        /// Gets an array of all the members of the class
        /// </summary>
        /// <param name="kind">The kind of member to get</param>
        /// <returns>An array of <see cref="ClassMemberDecl"/>s</returns>
        public ClassMemberDecl[] GetMembers(MemberKind kind)
        {
            var members = new List<ClassMemberDecl>();

            if (!((kind & MemberKind.Constructor) == MemberKind.None || Constructor == null))
                members.Add(Constructor);

            if (!((kind & MemberKind.Indexer) == MemberKind.None || Indexer == null))
                members.Add(Indexer);

            if ((kind & MemberKind.Field) != MemberKind.None)
                members.AddRange(Fields);

            if ((kind & MemberKind.Property) != MemberKind.None)
                members.AddRange(Properties);

            if ((kind & MemberKind.Method) != MemberKind.None)
                members.AddRange(Methods);

            if ((kind & MemberKind.Event) != MemberKind.None)
                members.AddRange(Events);

            return [.. members];
        }

        /// <summary>
        /// Gets an array of all the members declared in a class
        /// </summary>
        /// <returns>An array of <see cref="ClassMemberDecl"/>s</returns>
        public ClassMemberDecl[] GetMembers() => GetMembers(MemberKind.All);

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateClassDefinition(this);
        }
    }
}
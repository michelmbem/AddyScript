using System.Linq;

using AddyScript.Runtime;
using AddyScript.Runtime.OOP;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents the declaration of class method.
/// </summary>
/// <remarks>
/// Initializes a new instance of ClassMethodDecl.
/// </remarks>
/// <param name="name">The method's name</param>
/// <param name="scope">The scope of this method</param>
/// <param name="modifier">Determines whether this method is abstract, final, static or not</param>
/// <param name="parameters">The method's parameters</param>
/// <param name="body">The method's body</param>
public class ClassMethodDecl(string name, Scope scope, Modifier modifier, ParameterDecl[] parameters, Block body)
    : ClassMemberDecl(name, scope, modifier)
{
    /// <summary>
    /// The parameters of this method.
    /// </summary>
    public ParameterDecl[] Parameters => parameters;

    /// <summary>
    /// The body of this method.
    /// </summary>
    public Block Body => body;

    /// <summary>
    /// Gets if the method being declared has the same signature than an existing one.
    /// </summary>
    /// <param name="method">The existing method</param>
    /// <returns><b>true</b> if both methods have the same scope and prototype. <b>false</b> otherwise</returns>
    public bool MatchesSignature(ClassMethod method)
    {
        if (!(Name == method.Name && Scope == method.Scope &&
              Parameters.Length == method.Function.Parameters.Length)) return false;


        for (var i = 0; i < Parameters.Length; ++i)
        {
            var p1 = Parameters[i];
            var p2 = method.Function.Parameters[i];

            if (p1.ByRef != p2.ByRef ||
                p1.VaList != p2.VaList ||
                (p1.DefaultValue == null && p2.DefaultValue != null) ||
                (p1.DefaultValue != null && p2.DefaultValue == null))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a <see cref="ClassMember"/> from this instance.
    /// </summary>
    public override ClassMember ToClassMember()
    {
        Parameter[] memberParams = [.. Parameters.Select(p => p.ToParameter())];
        return new ClassMethod(Name, Scope, Modifier, new Function(memberParams, Body));
    }
}
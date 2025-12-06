using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.Frames;


/// <summary>
/// Represents the context under which a <see cref="MethodFrame"/> is created.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="InvocationContext"/>.
/// </remarks>
/// <param name="methodHolder">The class that holds the definition of the invoked method</param>
/// <param name="methodTarget">
/// The target instance of <paramref name="methodHolder"/>, represents the value of <b>this</b>
/// </param>
/// <param name="methodName">The invoked method's methodName</param>
public class InvocationContext(Class methodHolder, DataItem methodTarget, string methodName)
{

    /// <summary>
    /// The invoked method's holding class. May be null for global functions.
    /// </summary>
    public Class MethodHolder => methodHolder;

    /// <summary>
    /// The method's target instance. May be null for global functions or static methods.
    /// </summary>
    public DataItem MethodTarget => methodTarget;

    /// <summary>
    /// The invoked method's or function's name. May be null for lambda expressions and anonymous functions.
    /// </summary>
    public string MethodName => methodName;

    /// <summary>
    /// Gets if the invoked method is a constructor or not.
    /// </summary>
    /// <returns>
    /// <b>true</b> if the invoked method is homonymous to its holding class, <b>false</b> otherwise.
    /// </returns>
    public bool MethodIsConstructor()
    {
        return MethodHolder != null && MethodHolder.Name.Equals(MethodName);
    }
}

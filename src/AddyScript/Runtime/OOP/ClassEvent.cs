using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;


namespace AddyScript.Runtime.OOP;


/// <summary>
/// Represents an event in a class.
/// </summary>
/// <remarks>
/// Initializes an instance of ClassEvent.
/// </remarks>
/// <param name="name">The event's name</param>
/// <param name="scope">The scope of this event</param>
/// <param name="modifier">Determines whether this event is static or not</param>
/// <param name="parameters">The signature of any closure that could be used to handle this event</param>
public class ClassEvent(string name, Scope scope, Modifier modifier, Parameter[] parameters)
    : ClassMember(name, scope, modifier)
{

    /// <summary>
    /// The signature of any closure that could be used to handle this event.
    /// </summary>
    public Parameter[] Parameters { get; private set; } = parameters;

    /// <summary>
    /// Gets the name of the field that will automatically be generated
    /// to hold the collection of handlers of this event.
    /// </summary>
    private string HandlerSetName => $"__{Name}_handlers";

    /// <summary>
    /// Gets the name of the method that will automatically be generated
    /// to register handlers for this event.
    /// </summary>
    private string AddHandlerName => $"add_{Name}";

    /// <summary>
    /// Gets the name of the method that will automatically be generated
    /// to unregister handlers for this event.
    /// </summary>
    private string RemoveHandlerName => $"remove_{Name}";

    /// <summary>
    /// Gets the name of the method that will automatically be generated to trigger this event.
    /// </summary>
    private string TriggerEventName => $"trigger_{Name}";

    /// <summary>
    /// Generates a field to hold a collection of handlers for this event.
    /// </summary>
    /// <returns>A <see cref="ClassField"/></returns>
    public ClassField CreateHandlerSetField() =>
        new (HandlerSetName, Scope.Private, Modifier, new SetInitializer());

    /// <summary>
    /// Generates a method to make it easier to register handlers for this event.
    /// </summary>
    /// <returns>A <see cref="ClassMethod"/></returns>
    public ClassMethod CreateAddHandlerMethod()
    {
        var addHandlerFunc = new Function([new ("handler")],
                                          new Block(new MethodCall(PropertyRef.OfSelf(HandlerSetName),
                                                                   "add",
                                                                   new VariableRef("handler")),
                                                    new Return()));

        return new (AddHandlerName, Scope, Modifier, addHandlerFunc);
    }

    /// <summary>
    /// Generates a method to make it easier to unregister handlers for this event.
    /// </summary>
    /// <returns>A <see cref="ClassMethod"/></returns>
    public ClassMethod CreateRemoveHandlerMethod()
    {
        var removeHandlerFunc = new Function([new ("handler")],
                                             new Block(new MethodCall(PropertyRef.OfSelf(HandlerSetName),
                                                                      "remove",
                                                                      new VariableRef("handler")),
                                                       new Return()));

        return new (RemoveHandlerName, Scope, Modifier, removeHandlerFunc);
    }

    /// <summary>
    /// Generates a method to make it easier to trigger this event.
    /// </summary>
    /// <returns>A <see cref="ClassMethod"/></returns>
    /// <remarks>The so generated method is always private</remarks>
    public ClassMethod CreateTriggerEventMethod()
    {
        var arguments = new Expression[Parameters.Length];
        for (int i = 0; i < arguments.Length; ++i)
            arguments[i] = new VariableRef(Parameters[i].Name);

        var triggerEventFunc = new Function(Parameters,
                                            new Block(new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                      "handler",
                                                                      PropertyRef.OfSelf(HandlerSetName),
                                                                      new FunctionCall("handler", arguments)),
                                                      new Return()));

        return new (TriggerEventName, Scope.Private, Modifier, triggerEventFunc);
    }
}

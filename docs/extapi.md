# Extending the API

### Adding custom builtin functions

AddyScript's inner functions are all instances of the _AddyScript.Runtime.InnerFunction_ class. This class has a static property called _Globals_ that contains all of its predefined instances. To extend the list of the scripting engine's built-in functions, simply create new instances of _InnerFunction_ and add them to the _InnerFunction.Globals_ collection. The _InnerFunction_ constructor takes as arguments a string representing the name of the function, an array of _AddyScript.Runtime.Parameter_ objects representing the list of parameters that the function expects, and an instance of _AddyScript.Runtime.InnerFunctionLogic_ representing the body of the function. _InnerFunctionLogic_ is a delegate type that takes an array of _AddyScript.Runtime.DataItems.DataItem_ objects as a parameter and returns an object of the same _DataItem_ type as a result. Any method in a .Net class that has this prototype can be used as the body of an inner function. Here is a C# code example that demonstrates how to add a "clrscr" function to AddyScript to clear the screen:

```CSharp
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;

public static class MyExtensions
{
    public static void RegisterFunctions()
    {
        // Here we create the clrscr InnerFunction.
        // It takes no parameter and uses ClearScreenLogic as its body
        var ClearScreen = new InnerFunction("clrscr", [], ClearScreenLogic);

        // And here we add it to the InnerFunction.Globals collection
        InnerFunction.Globals.Add(ClearScreen);
    }

    // Here we define the logic of clrscr
    private static DataItem ClearScreenLogic(DataItem[] arguments)
    {
        System.Console.Clear();
        return Void.Value; // Here we are returning 'null'
    }
}
```

**Note**: Just make sure to call `MyExtensions.RegisterFunctions();` somewhere in your code before launching the interpreter.

### Adding custom builtin classes

Defining a new built-in class in AddyScript can have two meanings: it can mean adding a primitive type to the scripting engine. It can also mean adding a new object type to the scripting engine. Defining a new primitive type requires much more effort than creating a new object class. In all cases, you will need to create an instance of the _AddyScript.Runtime.OOP.Class_ meta-class and add it to the AddyScript._Runtime.Class.OOP.Class.Predefined_ collection.

#### Object classes

For a new object class, you will make it reference _AddyScript.Runtime.OOP.Class.Object_ directly or indirectly as its base class. The meta-class has a constructor that allows you to specify the parent class. Afterwards, you will only need to provide member definitions to the new class. All members can be defined manually. For methods, this means creating their AST from scratch. But there is a shortcut which consists in creating an _InnerFunction_ which will not be added to the _InnerFunction.Globals_ collection but will instead be converted to _AddyScript.Runtime.OOP.ClassMethod_ using one of the _ToInstanceMethod_ or _ToStaticMethod_ methods of the _InnerFunction_ class.

Example:

This is how we could define the _Exception_ class if it didn't exist in AddyScript

```CSharp
using System.Collections.Generic;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;

public static class MyExtensions
{
    public static void RegisterClasses()
    {
        // Exception is defined as a subclass of Class.Object with the name "Exception" and the given members
        var Exception = new Class(Class.Object, "Exception", Modifier.Default, GetExceptionConstructor(),
                                  GetExceptionIndexer(), GetExceptionFields(), GetExceptionProperties(),
                                  GetExceptionMethods(), GetExceptionEvents());
        
        // Here we add Exception to the collection of predefined classes
        Class.Predefined.Add(Exception);
    }

    // Below, the definition of all the members

    /**
    * The constructor is defined as:
    * public constructor (name, msg = null)
    * {
    *     if (msg === null)
    *         this._message = name;
    *     else
    *     {
    *         this._name = name!;
    *         this._message = msg;
    *     }
    * }
    */
    private static ClassMethod GetExceptionConstructor()
    {
        var ctorFunc = new Function([new Parameter("name"), new Parameter("msg", DataItems.Void.Value)],
                                    new Block(new IfElse(new BinaryExpression(BinaryOperator.Identical, new VariableRef("msg"), new Literal()),
                                                         new Assignment(PropertyRef.This("_message"), new VariableRef("name")),
                                                         new Block(new Assignment(PropertyRef.This("_name"),
                                                                                  new UnaryExpression(UnaryOperator.NotEmpty, new VariableRef("name"))),
                                                                   new Assignment(PropertyRef.This("_message"), new VariableRef("msg")))),
                                              new Return()));

        return new ClassMethod("Exception", Scope.Public, Modifier.Default, ctorFunc);
    }

    // No indexer
    private static ClassProperty GetExceptionIndexer()
    {
        return null;
    }

    /**
    * Two fields:
    * private _name = "Exception";
    * private _message;
    */
    private static IEnumerable<ClassField> GetExceptionFields()
    {
        return [
            new ClassField("_name", Scope.Private, Modifier.Default, new Literal(new String("Exception"))),
            new ClassField("_message", Scope.Private, Modifier.Default, new Literal(new String("")))
        ];
    }

    /**
    * Two properties:
    * public property name => this._name;
    * public property message => this._message;
    */
    private static IEnumerable<ClassProperty> GetExceptionProperties()
    {
        return [
            new ClassProperty("name", Scope.Public, Modifier.Default, "_name", PropertyAccess.Read),
            new ClassProperty("message", Scope.Public, Modifier.Default, "_message", PropertyAccess.Read)
        ];
    }

    /**
    * A single toString method that overrides the inherited method from object:
    * public function toString(format = "") => this.name;
    */
    private static IEnumerable<ClassMethod> GetExceptionMethods()
    {
        var toStringFunc = new Function([new Parameter("format", new String(""))],
                                        Block.Return(PropertyRef.This("name")));

        return [new ClassMethod("toString", Scope.Public, Modifier.Default, toStringFunc)];
    }

    // No event
    private static IEnumerable<ClassMethod> GetExceptionEvents()
    {
        return null;
    }
}
```

#### Primitive types

Creating a new primitive type goes through these same steps. But before that, you must add a new member to the _AddyScript.Runtime.OOP.ClassID_ enumeration to represent the new type. Afterwards, you will have to create a new instance of the meta-class as described above. This new class will not have a reference to a parent class but it will have the newly defined _ClassID_ (there is a suitable constructor in _Class_). After that you will need to create a new child class of _AddyScript.Runtime.DataItems.DataItem_ to represent data of the type being defined. The _Class_ property of this _DataItem_ type should return the reference to the previously created _Class_ instance. _DataItem_ provides a whole range of virtual methods that define the behavior of an object in arithmetic operations, conversions and property accesses. Overriding one of these methods allows you to customize the behavior of the new data type. You will probably also want to take a look at the _AddyScript.Runtime.DataItems.DataItemFactory_ and _AddyScript.Runtime.DataItems.DataItemBinder_ classes to add support for your data type in _Marshalling_ operations. _AddyScript.Runtime.DataItems.DataItemFactory_ has a _CreateDataItem_ method that converts a .Net _System.Object_ into a _DataItem_, you will certainly want to add support for your data type. _AddyScript.Runtime.DataItems.DataItemBinder_ on its side has a _Mismatch_ method that evaluates the degree of compatibility between .Net data types and AddyScript data types. You will also need to add support for your data type.

The last step in the process of creating a primitive type is to decide how you want to create data of this type. You may want it to have literal values ​​or initializers or simply a static factory method. If the choice of the factory method is made then the work is done: the factory method is probably already part of the class definition. On the other hand, for a literal value or an initializer, it will be necessary to update the analyzers so that they recognize a new category of symbols. It will also be necessary to modify the translators so that they know how to translate this new type of symbol.

Have a look at how existing primitive types are defined to better understand the whole process.

[Home](README.md) | [Previous](grammar.md) | [Next](extlang.md)
# Extending the API

### Adding custom builtin functions

AddyScript's inner functions are all instances of the _AddyScript.Runtime.InnerFunction_ class. This class has a static property called _Globals_ that contains all of its predefined instances. To extend the list of the scripting engine's built-in functions, simply create new instances of _InnerFunction_ and add them to the _InnerFunction.Globals_ collection. The _InnerFunction_ constructor takes as arguments a string representing the name of the function, an array of _AddyScript.Runtime.Parameter_ objects representing the list of parameters that the function expects, and an instance of _AddyScript.Runtime.InnerFunctionLogic_ representing the body of the function. _InnerFunctionLogic_ is a delegate type that takes an array of _AddyScript.Runtime.DataItems.DataItem_ objects as a parameter and returns an object of the same _DataItem_ type as a result. Any method in a .Net class that has this prototype can be used as the body of an inner function. Here is a C# code example that demonstrates how to add a "clrscr" function to AddyScript to clear the screen:

```CSharp
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;

namespace AddyScript.Interactive;

public static class MyExtensions
{
    public static void RegisterFunctions()
    {
        // Here we create the clrscr InnerFunction.
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

To define a built-in class, you first create an instance of the _AddyScript.Runtime.OOP.Class_ class and add it to the AddyScript._Runtime.Class.OOP.Predefined_ list. Creating an instance of _Class_ means registering members in it. For some members, you must provide an AST representing the structure and logic of the member. This is not that difficult, but for a method or property, the easiest solution is to create an internal function that does the work, and instead of registering it in the list of global functions, you can embed it as a method or property in a class. The _InnerFunction_ class has helper methods for this purpose. The class initializers for _AddyScript.Runtime.OOP.Class_ and _AddyScript.Runtime.InnerFunction_ illustrate both approaches.

[Home](README.md) | [Previous](grammar.md) | [Next](extlang.md)
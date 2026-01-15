## What is it?

AddyScript is a scripting engine for the .NET platform. It can be used to add support for programmability to professional applications or simply as a learning tool for younger users.

## Syntax overview

AddyScript has a straightforward C-like syntax with notable loans from popular scripting languages like JavaScript, PHP, Python, and Ruby.
It can be summarized as an interpreted, dynamically typed version of the C language with support for object-oriented programming.

## Main features

* Dynamic typing and dynamically declared variables (declared just when they are first assigned a value).
* Classes with encapsulated properties, events, operator overloading and introspection support.
* Closures (in the form of anonymous functions, lambda expressions and references to pre-declared functions).
* A structured error handling mechanism.
* Literals for most primitive types, initializers for some composite types.
* The ability to import an existing script from another.
* The ability to instantiate and manipulate .NET and COM types.
* The ability to invoke native DLL functions (still experimental).
* The entire scripting engine is exposed as a .NET class library and can be easily integrated into any application.
* Availability of a graphical script editor as well as an interactive command line console.

## Why is it different?

Unlike many other scripting languages, AddyScript does not rely on a language recognition tool for its lexical analyzer and parser.
Instead, its parsers are entirely hand-coded. This makes it easier for anyone with knowledge of C\# (and some basic compilation theory)
to modify the code without having to learn anything else (though it is fairly easy to regenerate these classes with tools like [Irony](https://github.com/IronyProject/Irony) or [ANTLR](https://www.antlr.org/)).
Also note that at this point, AddyScript is not built on the [DLR](https://learn.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/dynamic-language-runtime-overview). For some features like dynamic typing, this may seem like reinventing the wheel.
But actually the real goal is to keep the engine as easy to maintain as it is to migrate to other platforms (even native ones).

## When could it be useful?

* When you want to allow end users of your application to influence its behavior by providing custom scripts; these can be administrative scripts or business logic scripts.
* When you want to give the end user of your application the ability to edit and execute formulas at runtime (like in spreadsheets).
* When you want to teach your youngest child programming with a non-binding language.
* You can also use AddyScript as an administrative tool (in the same spirit as VBScript or JScript).

Perhaps in the future, more emphasis will be placed on this aspect of AddyScript's potential use.

## How to use it?

There are several usage scenarios for AddyScript.

Let's say you just want to parse and run a script from your program. Then do the following:

1. First, add a reference to _AddyScript.dll_ to your project.
2. Next, import the _AddyScript_ namespace.
3. You can also import any namespace from the _AddyScript.dll_ assembly depending on what you want to do with it.
4. Type your script somewhere (either in a file or as a string in memory).
5. Finally, add a code snippet like this:

```CSharp
var program = ScriptEngine.ParseFile("path/to/your/script");
// Alternatively: var program = ScriptEngine.ParseString("your script text in memory");
var context = new ScriptContext();
context.Bindings["stringVar"] = "John Doe";
context.Bindings["floatVar"] = 18.5;
ScriptEngine.Interpret(program, context);
Console.WriteLine(context.Bindings["stringVar"]);
Console.WriteLine(context.Bindings["floatVar"]);
```

Now suppose you want to evaluate a formula contained in a string. Here's how to do it:

1. Add a reference to _AddyScript.dll_ to your project.
2. Import _AddyScript_ and any other namespaces in the _AddyScript.dll_ assembly depending on what you want to do with them.
3. I'm assuming the formula to be evaluated is stored in a string called _strFormula_ and expects a parameter named _x_; successive values of the _x_ parameter are stored in a collection named _inputValues_.
4. Add a code snippet like this:

```CSharp
var formula = ScriptEngine.ParseExpression(strFormula);
var context = new ScriptContext();

foreach (var value in inputValues)
{
    context.Bindings["x"] = value;
    double result = ScriptEngine.Evaluate(formula, context).AsDouble;
    Console.WriteLine(result);
}
```

The last part of this introductory tutorial is about how to invoke parts of a script from C# or VB.NET (or any other language) code:

1. Add a reference to _AddyScript.dll_ to your project.
2. Import _AddyScript_ and any other namespaces from the _AddyScript.dll_ assembly.
3. The rest is as follows:

```CSharp
// Declare a type of delegate with a signature that matches that of a function you are about to define in the script
delegate double DummyDelegate(double x);

var context = new ScriptContext();
var engine = new ScriptEngine(context);

// Declare a dummy function in the script; note that this matches the prototype of DummyDelegate
engine.Evaluate("function dummy (x) { return 2 * x + 5; }");

// Invoke dummy from client code
var res1 = engine.Invoke("dummy", -7);
Console.WriteLine(res1.AsDouble);

// Create a delegate that wraps dummy and invoke it
var dummy = engine.GetDelegate<DummyDelegate>("dummy");
var res2 = dummy(4);
Console.WriteLine(res2);
```

## Features added over time

* Date and decimal types (ver. 0.7)
* Support for object-oriented programming (ver. 0.8)
* A **try-catch-finally** statement (ver. 0.8)
* Support for introspection (ver. 0.8)
* An **import** directive (ver. 0.8)
* Date, string and array arithmetic (ver. 0.8)
* Function references and inline function declarations (ver. 0.8)
* Localized error messages (ver. 0.9. From version 0.9.2, French language support was added)
* A C-like type conversion syntax (ver. 0.9)
* Better handling of assignment and parameters passed by reference (ver. 0.9)
* An empty for loop is equivalent to an infinite loop (ver. 0.9)
* Better support for foreach loop including iterator protocol for user-defined classes (demonstrated in the _xrange.add_ sample script) (ver. 0.9)
* Support for a **goto** statement and labels (ver. 0.9)
* Better control over the use of **continue**, **break**, **this**, **super** and **return** keywords (ver. 0.9)
* Complete redesign of the GUI, with the ability to load multiple scripts simultaneously (ver. 0.9)
* Ability to export the AST to XML (ver. 0.9)
* Ability to regenerate source code from the AST (mostly used for automatic code formatting) (ver. 0.9.1. Improved in 0.9.2)
* Improved **try-catch-finally** instruction: no **return** in the **finally** block; no jump out of the **finally** block; jumps invoked from **try** or **catch** blocks are well remembered after the **finally** block is executed. (ver. 0.9.1)
* Ability to create instances of .NET and COM types and invoke their members (ver. 0.9.4)
* Ability to convert a closure to a delegate and attach it as an event handler for a .NET object (ver. 0.9.4)
* Reworked external functions functionality (now targets native functions; ver. 0.9.4.1)
* Replaced the initial array type with a more robust collection framework (ver. 0.9.5).
* **long**, **rational**, and **complex** data types; **long** maps to a large integer type (ver. 0.9.5).
* New kinds of class members: properties, events, and operators (ver. 0.9.5).
* Reimplemented the **decimal** data type: it now maps to a big decimal type (ver. 0.9.5).
* A search interface for the ScriptEngine class (ver. 0.9.5).
* Replaced the **local** keyword with a **var** keyword (ver. 0.9.5).
* Introduced constant declaration and **const** keyword (which was just a reserved word in older versions (ver. 0.9.5).
* An interactive shell (ver. 0.9.5).
* Better closure handling (ver. 0.9.5).
* Support for indexers in custom classes (ver. 0.9.9).
* Support for .NET generic types (ver. 0.9.9).
* Ability to overload postfix unary operators in custom classes (ver. 0.9.9).
* Ability to invoke a base type property or indexer in derived classes (ver. 0.9.9).
* Ability to invoke functions with explicitly named parameters in random order (ver. 0.9.9).
* Migration to .NET 8.0 (ver. 0.9.9).
* Introduction of a **blob** type as an abstraction for arrays of byte (ver. 0.9.9.9).
* Tuples and tuple initializers (ver. 0.9.9.9).
* Support for indexers in user-defined classes (ver. 0.9.9.9).
* Support for using the overriden version of _equals_, _hashCode_ and _toString_ methods in user-defined classes for searches in collections (ver. 0.9.9.9).
* Migration of the GUI to [AvaloniaUI](https://avaloniaui.net/) for better cross-platform support (ver. 0.9.9.9).
* Pattern matching using the **switch** and **is** operators (ver. 0.9.9.9).
* Object destructuring (ver. 0.9.9.9).
* A better handling of conversions from **float** or **decimal** to **rational** (ver. 0.9.9.9).
* Additional localized UI resources (Spanish and Portuguese both generated with the help of online translators. May not be perfect!).
* A **duration** data type that better handles date arithmetic (ver. 0.9.9.9).

## Features still awaited

The following features may appear in future versions of the scripting engine:

* The ability to compile a script to a .NET assembly via Reflection.Emit. This would improve runtime performance and bring other features like the ability to subclass .NET types from a script.
* An alternative syntax for the **import** directive to make it look like a function call with a string argument.
* A variable number of strongly typed catch clauses in the **try-catch-finally** statement.

Well, that's all for the moment. Hope you'll enjoy. Waiting for your feedback.
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

## Recently added features

* Migration to .NET 8.0 (ver. 0.9.9).
* Introduction of a **blob** type as an abstraction for arrays of byte (ver. 0.9.9.9).
* Tuples and tuple initializers (ver. 0.9.9.9).
* Support for indexers in user-defined classes (ver. 0.9.9.9).
* Support for using the overridden version of _equals_, _hashCode_ and _toString_ methods in user-defined classes for indexing and/or search in collections (ver. 0.9.9.9).
* Migration of the GUI to [AvaloniaUI](https://avaloniaui.net/) for an improved cross-platform support (ver. 0.9.9.9).
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

## Learn more

You can find more examples in the _samples_ folder of the distribution package.<br>
You can also check the [Wiki](https://michelmbem.github.io/AddyScript/) for more documentation and code samples.

Well, that's all for the moment. Hope you'll enjoy. Waiting for your feedback.
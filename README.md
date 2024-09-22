## What is it?

AddyScript is a scripting engine for the .Net platform. It can be used to add programmability to professional applications or simply as a learning tool for younger users.

## Syntax overview

AddyScript has a straightforward C-like syntax with notable borrowings from popular scripting languages ​​like JavaScript, PHP, Python, and Ruby. It can be summarized as an interpreted, dynamically typed version of the C language with support for object-oriented programming.

## Main features

* Dynamic typing and dynamically declared variables (declared just when they are first assigned a value).
* Classes with encapsulated properties, events, operator overloading and introspection support.
* Closures (in the form of anonymous functions, lambda expressions and references to pre-declared functions).
* A structured error handling mechanism.
* Literals for most primitive types, initializers for some composite types.
* The ability to import an existing script from another.
* The ability to instantiate and manipulate .Net and COM types.
* The ability to invoke native DLL functions (still experimental).
* The entire scripting engine is exposed as a .Net class library and can be easily integrated into any application.
* Availability of a graphical script editor as well as an interactive command line console.

## Why is it different?

Unlike many other scripting languages, AddyScript does not rely on a language recognizer for its lexical analyzer and parser. Instead, its parsers are entirely hand-coded. This makes it easier for anyone with knowledge of C\# (and some basic compilation theory) to modify the code without having to learn anything else (though it is fairly easy to regenerate these classes with tools like [Irony](https://github.com/IronyProject/Irony) or [ANTLR](https://www.antlr.org/)). Also note that at this point, AddyScript is not built on the [DLR](https://learn.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/dynamic-language-runtime-overview). For some features like dynamic typing, this may seem like reinventing the wheel. But actually the real goal is to keep the engine as easy to maintain as it is to migrate to other platforms (even native ones).

## When could it be useful?

* When you want to allow end users of your application to influence its behavior by providing custom scripts; these can be administrative scripts or business logic scripts.
* When you want to give the end user of your application the ability to edit and execute formulas at runtime (like in spreadsheets).
* When you want to teach your youngest child programming with a non-binding language.
* You can also use AddyScript as an administrative tool (in the same spirit as VBScript or JScript).

Perhaps in the future, more emphasis will be placed on this aspect of AddyScript's potential use.

## How to use it?

There are several usage scenarios for AddyScript.

Let's say you just want to parse and run a script from your program. Then do the following:
First, add a reference to AddyScript.dll to your project.
Next, import the AddyScript namespace.
You can also import any namespace from the AddyScript.dll assembly depending on what you want to do with it.
Type your script somewhere (either in a file or as a string in memory).
Finally, add a code snippet like this:

```Cpp
var program = ScriptEngine.ParseFile('path/to/your/script');
// Alternatively: var program = ScriptEngine.ParseString('path/to/your/script');
var context = new ScriptContext();
context. Bindings["stringVar"] = "John Doe";
context.Bindings["floatVar"] = 18.5;
ScriptEngine.Interpret(program, context);
Console.WriteLine(context.Bindings["stringVar"]);
Console.WriteLine(context. Bindings["floatVar"]);
```

Now suppose you want to evaluate a formula contained in a string. Here's how to do it:
Add a reference to AddyScript.dll to your project.
Import AddyScript and any other namespaces in the AddyScript.dll assembly depending on what you want to do with them.
I'm assuming the formula to be evaluated is stored in a string called strFormula and expects a parameter named x; successive values of the x parameter are stored in a collection named inputValues.
Add a code snippet like this:

```Cpp
var formula = ScriptEngine.ParseExpression(strFormula);
var context = new ScriptContext();

foreach (var value in inputValues)
{
	context.Bindings["x"] = value;
	double result = ScriptEngine.Evaluate(formula, context).AsDouble;
	Console.WriteLine(result);
}
```

The last part is about how to invoke parts of a script from C# or VB.Net (or any other language) code:
Add a reference to AddyScript.dll to your project.
Import AddyScript and any other namespaces from the AddyScript.dll assembly.
The rest is as follows:

```Cpp
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

## About the demo

The editor window uses the [Scintilla.NET](https://github.com/desjarlais/Scintilla.NET) UI widget and some of its [extensions](https://github.com/desjarlais/Scintilla.NET#utility-assemblies). So it has some nice features like syntax highlighting, autocomplete, line numbers, and call tips. Some samples are provided in the _samples_ subdirectory. All this is done to help you get started easily.

## What has been recently added?

* Added date and decimal types (ver. 0.7)
* Support for object-oriented programming (ver. 0.8)
* Added the **try-catch-finally** statement (ver. 0.8)
* Support for introspection (ver. 0.8)
* Added the **import** directive (ver. 0.8)
* Date, string and array arithmetic (ver. 0.8)
* Function references and inline function declarations (ver. 0.8)
* Localized error messages (ver. 0.9. From version 0.9.2, French language support was added)
* A C type conversion syntax (ver. 0.9)
* Better handling of assignment and parameters passed by reference (ver. 0.9)
* An empty for loop is now equivalent to an infinite loop (ver. 0.9)
* Better support for foreach loop including iterator protocol for user-defined classes (demonstrated in _xrange.add_ sample script) (ver. 0.9)
* Support for goto statement and labels (ver. 0.9)
* Better control over the use of **continue**, **break**, **this**, **super** and **return** keywords (ver. 0.9)
* Completely redesigned GUI for the demo application, with the ability to load multiple scripts simultaneously (ver. 0.9)
* An additional demo application named _Plotter_ (the name is self-explanatory)
* Ability to export the AST to XML (ver. 0.9)
* Ability to regenerate source code from the AST (mostly used for automatic code formatting) (ver. 0.9.1. Improved in 0.9.2)
* Improved **try-catch-finally** instruction: no return in the finally block; no jump out of the finally block; jumps invoked from try or catch blocks are well remembered after the finally block is executed. (ver. 0.9.1)
* Ability to create instances of .Net and COM types and invoke their members (ver. 0.9.4)
* Ability to convert a closure to a delegate and attach it as an event handler for a .Net object (ver. 0.9.4)
* Reworked external functions functionality (now targets native functions; ver. 0.9.4.1)
* Replaced the initial array type with a more robust collection framework (ver. 0.9.5).
* Added **long**, **rational**, and **complex** data types; **long** maps to a large integer type (ver. 0.9.5).
* Added new class member types: properties, events, and operators (ver. 0.9.5).
* Reimplemented the **decimal** data type: it now maps to a large decimal type (ver. 0.9.5).
* A search interface for the ScriptEngine class (ver. 0.9.5).
* Replaced the **local** keyword with a **var** keyword (ver. 0.9.5).
* Introduced constant declaration and **const** keyword (which was just a reserved word in older versions (ver. 0.9.5).
* An interactive shell (ver. 0.9.5).
* Better closure handling (ver. 0.9.5).
* Support for indexers in custom classes (ver. 0.9.9).
* Support for dotnet generic types (ver. 0.9.9).
* Ability to overload postfix unary operators in custom classes (ver. 0.9.9).
* Ability to invoke a base type property or indexer in derived classes (ver. 0.9.9).
* Ability to invoke functions with explicitly named parameters in random order (ver. 0.9.9).
* Migration to dotnet 8.0 (ver. 0.9.9).

See the Changelog page of the help manual for more details.

## What's still to come?

* The ability to compile a script to a DLL or executable managed via Reflection.Emit; this is the real next step. This will improve runtime performance and bring other features like the ability to subclass a .Net type from a script.
* Reworked the **import** directive syntax: it should look like a function call with a string argument.
* Migrated the UI to [AvaloniaUI](https://avaloniaui.net/) for better cross-platform support;
* Additional localized UI resources (up to you).
* Maybe a variable number of strongly typed catch clauses in the **try-catch-finally** statement.

Well, that's all for the moment. Hope you'll enjoy. Waiting for your feedback.
## What is it?
AddyScript is a scripting engine for the .Net platform. It can be used to add scripting capabilities to business applications or simply as a learning tool for younger people.

## Syntax overview
AddyScript has a straightforward C-like syntax with noticeable loans to popular scripting languages like JavaScript, PHP, Python and Ruby. It can be summarized as an interpreted dynamically typed version of the C language with OOP features.

## Main features
*	Dynamic typing and dynamically declared variables (declared just as they are assigned a value for the first time).
*	Classes with properties, events, operator overloading and introspection support.
*	Closures (in the form of anonymous functions, lambda expressions and references to pre-declared functions).
*	A Structured error handling mechanism.
*	Literals for most primitive types, initializers for some composite types.
*	The ability to import an existing script from another.
*	The ability to instantiate and manipulate .Net and COM types.
*	The ability to invoke native DLL functions (still at an experimental stage).
*	The whole scripting engine is exposed as an API to the .Net world and can be tightly integrated to any application.
*	Availability of a graphical script editor as well as an interactive command line console.
*	A small help file in the CHM format is provided to help people getting started.

## Why is it different?
Unlike many other scripting languages, AddyScript does not rely on a language recognition tool for its lexer and parsers. Rather than that, its lexer and parser are entirely hand-coded. This makes it easier for anyone who knows C## (and has some compilation theory rudiments) to edit the code without needing to learn another stuff (However, it's quite easy to regenerate those classes with tools like [Irony](https://github.com/IronyProject/Irony) or [ANTLR](https://www.antlr.org/)). Also note that at this stage, AddyScript is not built on top of the [DLR](https://learn.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/dynamic-language-runtime-overview). For some features like dynamic typing, this may seam like re-inventing the wheel. Not at all, The real goal is to keep the engine easy to migrate to other (even native) platforms.

## When could it be useful?
*	When you want to allow the end users of your application to affect its behavior by providing custom scripts; those may be admin scripts or business logic scripts.
*	When you want to give to the end-user of your application the ability to edit and execute formulas at runtime (like in worksheets).
*	When you want to teach programming to your younger kid with a not constraining language.
*	You can also use AddyScript as an administration tool (in the same spirit than VBScript or JScript). Maybe in the future, there'll be more emphasis on this aspect of the potential AddyScript's use.

## How to use it?
There are several usage scenarios for AddyScript.

Suppose that you simply want to parse and run a script from your program. Then simply proceed as follows:
First, add a reference to AddyScript.dll to your project.
Then import the AddyScript namespace.
You could additionally import any namespace from the AddyScript.dll assembly depending on what you want to do with it.
Type your script somewhere (either in a file or in an in-memory string).
Finally, add a code snippet like this:

	var program = ScriptEngine.ParseFile(path_to_your_script);
	// Alternatively: var program = ScriptEngine.ParseString(string_containing_your_script);
	var context = new ScriptContext();
	context.Bindings["stringVar"] = "John Doe";
	context.Bindings["floatVar"] = 18.5;
	ScriptEngine.Interpret(program, context);
	Console.WriteLine(context.Bindings["stringVar"]);
	Console.WriteLine(context.Bindings["floatVar"]);

Now, suppose that you want to evaluate a formula contained in a string. Here is how to do:
Add a reference to AddyScript.dll to your project.
Import the AddyScript and any other namespaces from the AddyScript.dll assembly depending on what you want to do with it.
I suppose that the formula to evaluate is store in a string called strFormula and expects a parameter named x; successive values for parameter x are stored in a collection named inputValues.
Add a code snippet like this:

	var formula = ScriptEngine.ParseExpression(strFormula);
	var context = new ScriptContext();

	foreach (var value in inputValues)
	{
		context.Bindings["x"] = value;
		double result = ScriptEngine.Evaluate(formula, context).AsDouble;
		Console.WriteLine(result);
	}

The last stuff is about how to invoke parts of a script from C# or VB.Net (or any other language) code:
Add a reference to AddyScript.dll to your project.
Import the AddyScript and any other namespaces from the AddyScript.dll assembly.
The rest is like it follows:

	delegate double DummyDelegate(double x);

	var context = new ScriptContext();
	var engine = new ScriptEngine(context);

	// Declare a dummy function in the script; note that this one matches the prototype of DummyDelegate
	engine.Evaluate("function dummy (x) { return 2 * x + 5; }");

	// Invoke dummy from the client code
	var res1 = engine.Invoke("dummy", -7);
	Console.WriteLine(res1.AsDouble);

	// Create a delegate that wraps dummy and invoke it
	var dummy = engine.GetDelegate<DummyDelegate>("dummy");
	var res2 = dummy(4);
	Console.WriteLine(res2);

## About the demo
The editor window uses the [Scintilla.NET](https://github.com/desjarlais/Scintilla.NET) UI widget and some of its [extensions](https://github.com/desjarlais/Scintilla.NET#utility-assemblies). So, it has some nice features like syntax highlighting, auto-completion, line numbers and call tips. It's also shipped with a small help manual in CHM format. A few samples are provided in the _Examples_ subdirectory of the solution's folder. All that is made to help you getting started.

## What has been recently added?
*	Date and decimal types (ver. 0.7)
*	OOP support (ver. 0.8)
*	try-catch-finally (ver. 0.8)
*	reflection (ver. 0.8)
*	import directive (ver. 0.8)
*	date, string and array arithmetic (ver. 0.8)
*	function references and in-line function declarations (ver. 0.8)
*	localized error messages (ver. 0.9. From vesion 0.9.2, french language support has been added)
*	a C-like conversion syntax (ver. 0.9)
*	a better management of assignment and byref parameters (ver. 0.9)
*	an empty for loop is now equivalent to an infinite loop (ver. 0.9)
*	a better support of the foreach loop including an iterator protocol for user defined classes (demonstrated in the _xrange.add_ sample script) (ver. 0.9)
*	a goto statement and labels management (ver. 0.9)
*	a better control on the usage of the **continue**, **break**, **this**, **super** and **return** keywords (ver. 0.9)
*	a totally redesigned GUI for the demo application, with the ability of simultaneously loading several scripts (ver. 0.9)
*	a small help file for the demo application (ver. 0.9)
*	an additional demo application named _Plotter_ (the name is self explanatory) 100% compatible with mono 2.6.3 and higher (ver. 0.9)
*	a demo application for the mono platform (ver 0.9; improved in version 0.9.3)
*	the possibility to export the syntax tree to XML format (ver 0.9)
*	the possibility to regenerate the source-code from the syntax tree (essentially used to allow automatic code formatting) (ver. 0.9.1. Improved in version 0.9.2)
*	improved try-catch-finally statement : no return in the finally block; no jump out of the finally block; jumps invoked from the try or catch blocks are well remembered after the execution of the finally block. (ver. 0.9.1)
*	ability to create instances of .Net and COM types and invoke their members (ver. 0.9.4)
*	ability to convert a closure to a delegate and attach it as a handler for a .Net object's event (ver 0.9.4)
*	redesigned external functions feature (now target native functions; ver 0.9.4.1)
*	replacement of the initial array type by a more robust collection framework (ver. 0.9.5).
*	addition of the long, rational and complex data types; long maps a big integer type (ver. 0.9.5).
*	addition of new kinds of class members: properties, events and operators (ver. 0.9.5).
*	re-implementation of the decimal data type: now it maps a big decimal type (ver. 0.9.5).
*	a reacher interface for the ScriptEngine class (ver. 0.9.5).
*	replacement of the **local** keyword by a **var** keyword (ver. 0.9.5).
*	introduction of constants declaration and the **const** keyword (which was just a reserved word in older releases (ver. 0.9.5).
*	an interactive shell (ver. 0.9.5).
*	a better management of closures (ver. 0.9.5).
*	Support for indexers in custom classes (ver. 0.9.9).
*	Support for dotnet generic types (ver. 0.9.9).
*	Ability to overload postfix unary operators in custom classes (ver. 0.9.9).
*	Ability to invoke base type property or indexer in derived classes (ver. 0.9.9).
*	Ability to invoke functions with explicitly named parameters in random order (ver. 0.9.9).
*	Migration to dotnet 8.0 (ver. 0.9.9).
consult the Changes history page of the help manual for more details.

## What's still to come?
*	The ability to compile a script to a managed DLL or executable via Reflection.Emit; that's the real next step. This will enhance runtime performances as well as bringing other functionalities like the ability to subclass a .Net's type from a script.
*	Redesign of the **import** directive syntax: it should look like a function call with a string argument.
*	Migration of the GUI to [AvaloniaUI](https://avaloniaui.net/) for a better multiplatform support;
*	Additional localized UI resources (It's up to you to do that).
*	Maybe a variant number of strongly typed catch clause in the try-catch-finally statement.

Well, that's all. Hope you'll enjoy. Waiting for your feedback.
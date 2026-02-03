# Interpreting a script

## The simpler approach

To interpret a script with AddyScript, just proceed as follows:

1. Type the script in an editor (here I suppose that the editor is a text widget named _txtScript_).
2. Save it in a file (or anywhere else) or keep it in memory if you prefer to do so.
3. Then add a reference to _AddyScript.dll_ in your project.
4. Create a GUI in your project to allow the user to invoke the scripting engine.
5. In your code file import the _AddyScript_ namespace.
6. You could import any additional namespace from the AddyScript assembly depending on what you intend to do.
7. Finally, type a code snippet like the following one in an event handler:

```csharp linenums="1"
var context = new ScriptContext();
context.Bindings["myString"] = "Hello!";
context.Bindings["myFloat"] = 0.9;
ScriptEngine.ExecuteString(txtScript.Text, context);
// Alternatively: ScriptEngine.ExecuteFile(@"path/to/my/script", context);
Console.WriteLine("myString = " + context.Bindings["myString"]);
Console.WriteLine("myFloat = " + context.Bindings["myFloat"]);
```

### Notes:

Don't forget to embed this code in a try-catch structure.

## Parsing once, running later

Sometimes you need to parse the script once and run it multiple times later without needing to restart the parsing process.
The _ScriptEngine_ class provides means to accomplish this in a straightforward manner.

Look at this example:

```csharp linenums="1"
var context = new ScriptContext();
var program = ScriptEngine.ParseString(txtScript.Text);
// Alternatively: var program = ScriptEngine.ParseFile(@"path/to/my/script");

foreach (var item in myArray)
{
    context.Bindings["myValue"] = item;
    ScriptEngine.Execute(program, context);
    Console.WriteLine("myValue = " + context.Bindings["myValue"]);
 }
```

This will typically run the same script multiple times with different values of the "myValue" context variable
without needing to parse the source code each time.

## Sequentially parsing and running commands

Here is where the non-static version of the _ScriptEngine.Execute_ method comes to action.
It is used to sequentially interpret commands on an instance of the _ScriptEngine_ class.
Each command is interpreted in the same context as the previous one. So, the result of a command may affect a following one
(for example, previously declared functions can be called by following commands).
If a command includes multiple statements, all of these will be interpreted.
If the tail of the command is insufficient to be parsed as a full statement,
the _ScriptEngine.Execute_ method keeps a copy of it and expects the following command to complete the previous one.
The _Satisfied_ property of the _ScriptEngine_ class indicates whether the continuation of a statement is expected or not.
The AddyScript Interactive Shell (**asis**) heavily relies on that possibility to work.

Here is an example of how to use the _ScriptEngine.Execute_ instance method:

```csharp linenums="1"
const string MAIN_PROMPT = ">>> ";
const string CONTINUATION_PROMPT = "... ";

var context = new ScriptContext();
var engine = new ScriptEngine(context);
string prompt = MAIN_PROMPT;

while (true)
{
    Console.Write(prompt);
    string command = Console.ReadLine();

    if (string.IsNullOrEmpty(command)) continue;
    if (engine.Satisfied && command == "exit") break;

    try
    {
        var result = engine.Execute(command);
        if (result != null) Console.WriteLine(result);
        prompt = engine.Satisfied ? MAIN_PROMPT : CONTINUATION_PROMPT;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        prompt = MAIN_PROMPT;
    }
}
```

## Interacting with the scripting engine

The instance version of the _ScriptEngine.Execute_ method is itself a way of interacting with the scripting engine.
But sometimes, you may need a closer interaction.
Suppose for example that you want to call a function you've previously defined in the script from your C# (or VB) code.
How to accomplish this? Well, the _ScriptEngine_ class provides useful methods that allow such an interaction.
The _ScriptEngine.Invoke_ method can be used to invoke from user code a function defined in the script.
It takes the function's name and an arbitrary sized array of objects as parameters.
The arbitrary sized array of objects represents the arguments that will be passed to the function.
The _ScriptEngine.Invoke_ method returns an instance of the AddyScript.Runtime.DataItems.DataItem class.
You can use one of the _AsXXX_ properties of the returned object [or its `ConvertTo(Type targetType)` method] to cast it to the desired type.
The _ScriptEngine.GetDelegate_ method goes further by allowing the user code to retrieve a delegate that could be used to invoke a function defined in the script.
It takes the function's name as a parameter. The target delegate type is indicated as a generic type parameter.

Here is an example of how to use both methods:

```csharp linenums="1"
delegate int SumType(int a, int b);

var context = new ScriptContext();
var engine = new ScriptEngine(context);

// We define a 'sum' function prior to any call to Invoke or GetDelegate
engine.Execute("function sum(a, b) => a + b;");

var x = engine.Invoke("sum", 10, 5).AsInt32;
Console.WriteLine($"sum(10, 5) gives: {x}");

var sum = engine.GetDelegate<SumType>("sum");
Console.WriteLine($"sum(7, -3) gives: {sum(7, -3)}");
```

## The ScriptContext class

So far you have been reading this tutorial, you may have noticed that on each example, we create and use an instance of a class called _ScriptContext_.
Well, this class is simply a mean for providing initial settings to the scripting engine.
It exposes three interesting properties that are listed and explained in the following table:

| Property                                       | Description                                                                                                                   |
|------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| `Dictionary<string, object> Bindings { get; }` | A set of variables bindings: they will be automatically declared in the script and can be retrieved upon script's completion. |
| `string[] ImportPaths { get; set; }`           | The list of directories in which the import directive searches the files to include.                                          |
| `Assembly[] References { get; set; }`          | The list of assemblies in which references to .NET types will be searched for.                                                |

Notice that by default, the _ImportPaths_ property is empty while the _References_ property contains references to some essential .NET assemblies.
You don't need to put a dot (symbolizing the current working directory) to the _ImportPaths_ property since imported scripts are always first searched in the same directory as the importing script.

## The RuntimeServices class

We have not demonstrated the utility of the _AddyScript.Runtime.RuntimeServices_ class here.
However, it provides services that can be very helpful at runtime.
For example, its **In** and **Out** static properties are used by the scripting engine as standard input and (respectively) standard output devices for the currently running script.
So if you want to redirect the script's input or output, just assign anything you want to those properties before invoking any _Execute_ method on the _ScriptEngine_ class.

<div markdown class="web-only">

[Home](README.md) | [Previous](features.md) | [Next](evaluate.md)

</div>
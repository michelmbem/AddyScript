# Evaluating expressions

### The simpler approach

To evaluate an expression with AddyScript, do the following:

1. Type the expression into an editor (I'll assume the editor is a TextBox control named _txtExpr_).
2. Add a reference to AddyScript.dll in your project.
3. Create a GUI in your project to allow the user to invoke the scripting engine.
4. In your code file, import the AddyScript namespace.
5. You can import any additional namespaces from the AddyScript assembly depending on what you intend to do.
6. Finally, type a code snippet like this into an event handler:

```CSharp
var context = new ScriptContext();
context.Bindings["myString"] = "Hello!";
context.Bindings["myFloat"] = 0.9;
var result = ScriptEngine.EvaluateString(txExpr.Text, context);
Console.WriteLine($"Given {context.Bindings["myString"]} and {context.Bindings["myFloat"]}, we obtain {result}");
```

**Remark:** Don't forget to embed this code in a try-catch block.

### Parsing once, running later

Below is another example where the expression is parsed once and evaluated multiple times in a loop. Before testing it, you need to create the GUI and provide the logic for the _MoveTo_ and _LineTo_ methods yourself:

```CSharp
// txtFunction is a TextBox in which the user types the expression.
// A single parameter named 'x' is expected.
var expression = ScriptEngine.ParseExpression(txtFunction.Text);
var context = new ScriptContext();

// txtFrom, txtTo and txtBy are TextBoxes in which the user types the plotting range.
double start = double.Parse(txtFrom.Text);
double end = double.Parse(txtTo.Text);
double step = double.Parse(txtBy.Text);

// Determine the initial point and move the graphical cursor there.
double x = start;
context.Bindings["x"] = x;
double y = ScriptEngine.Evaluate(expression, context).AsDouble;
MoveTo(x, y);

// Plot the curve segment by segment.
do
{
    x += step;
    context.Bindings["x"] = x;
    y = ScriptEngine.Evaluate(expression, context).AsDouble;
    LineTo(x, y);
 } while (x < end);
```

This example is very similar to what is done in the source code of AddyScript Plotter (**asplot**), a demo tool provided with the scripting engine and which could be used for real needs of plotting mathematical function curves. Here is an overview:

![asplot](asplot.jpg)

[Home](README.md) | [Previous](interpret.md) | [Next](scriptengine.md)
# More with the ScriptEngine class

### Exporting the AST to XML

You can easily export an Abstract Syntax Tree to Xml format by invoking the _ExportXml_ static method of the _ScriptEngine_ class. Simply do as follows:

```CSharp
var program = ScriptEngine.ParseString(txtScript.Text);
// Alternatively: var program = ScriptEngine.ParseFile('path/To/My/Script');
ScriptEngine.ExportXml(program, @"C:\myScript.xml");
// Alternatively: ScriptEngine.ExportXml(program, someStream);
```

That functionality is specially helpful for debugging purpose. You can use it to ensure that the parsed script has the expected logical structure.

### Regenerating the source code

The _ScriptEngine_ class has a _GenerateCode_ static method than can be used to regenerate the source of a script given its AST (as an instance of the _AddyScript.Ast.Program_ class). The generated source code is so nicely formatted than the "Reformat code" functionality of the AddyScript Graphical Editor (**asgui**) fully relies on the _ScriptEngine.GenerateCode_ method. here is an example of how to use it:

```CSharp
var program = ScriptEngine.ParseString(txtScript.Text);
txtScript.Text = ScriptEngine.GenerateCode(program);
```

[Home](README.md) | [Previous](evaluate.md) | [Next](asgui-asis.md)
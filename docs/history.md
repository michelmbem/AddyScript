# Changes history

**(DRAFT! Needs more work!)**

So many changes have occured in AddyScript since the very first release than one may wonder why so?
Please just remember that AddyScript is still under construction and may change between versions without advice.
If you have downloaded an earlier version of it and started your own work based on that version, you are free to ignore the changes that were introduced in subsequent versions.
I'll try to summarize here, the most important changes that were introduced in versions of the scripting engine over the years.

### Changes in the language syntax

#### Data types:

* The following data types are new to AddyScript: long, rational, complex, list, map, set, queue and stack.
* The array data type no more belongs to AddyScript: The former sole collection type of AddyScript is now replaced by list, map, set, queue and stack. That type was very close to the PHP's array type: it had a double indexed-associative nature, was very dynamic (indexes had not to be contiguous) and could participate to some kind of arithmetic like sets. I hope the new AddyScript's collection framework is richer in features than this array type. You can still simulate a dynamic array by using a map with integral keys.
* The following data types have changed:
* float: formerly called double, nothing has changed apart the name. I think, even if values of this type are double-precision floatting-point numbers, float is more natural as name than double.
* decimal: The internal representation has changed. Initially, it was a wrapper around the .Net's Decimal type. Now, it wraps a big decimal value. It's somehow similar to the long type in that it can handle very large (really very large) values. In fact, a long can be considered as a decimal with its decimal part fixed to zero. Both types have been created in regard to handle overflow in arithmetic operations and/or provide more accuracy than the float type does.
* resource: formerly named native, then handle and finally resource. The new name is influenced by the fact that in PHP, anything that comes from the outter world is of the resource type.
* ClassInfo is now called TypeInfo; it still plays the same role but most of its former methods have been replaced by properties. Similar changes have been made to the Exception, MemberInfo, FieldInfo, MethodInfo and ParameterInfo types. There are now a PropertyInfo and an EventInfo types; those are subclasses of MemberInfo that provide additionnal informations about the properties and events of a class.

#### Literals:

* There are now literals for all scalar types including dates. Most compound types have initializers which act like literals for those types.
* Numeric literals can include undescores to make them more readable.

#### Constants:

* Declaring constants is a new feature to AddyScript. In versions earlier than 0.9.5, const was a reserved word but not a keyword. From version 0.9.5 it's used to evaluate expressions and mark the result as read-only.

#### Variables:

* The local keyword has been replaced by the var keyword. There is nomore need to intialize a variable when it's declared. However, the variable must be initialized prior to its first use. Also notice that the scope of a variable is now extended to its declaring function. So, it's nomore possible to redeclare a variable in an inner block.

#### Functions:

* A function can nomore access to the local variables of its calling function (that was a serious weekness of previous versions of AddyScript). The set of variables that are accessible to a function is now limited to its proper local variables and to global ones.
* Better management of closures: a closure can now access to the local variables of the function or method in which it's declared and modify them even after the declaring function/method is returned.
* Most global functions have been replaced by equivalent methods into target classes. In fact, in earlier versions of AddyScript, the user had the choice of using a global function or a method for some operations on certain data types. That duplicity has been removed so that each functionality becomes available by a unique mean.
* The connect function is nomore needed since it was invented to handle a weekness in the scripting engine: the unability to convert any function to a native .Net delegate. This problem is now solved and you can attach a handler to any .Net object's event using the corresponding add_XXX method (like the add_Click method of a Button).
* The format, print and println functions are now implemented to take into account any override of the toString method by custom classes. They are nomore simple wrappers around their .Net counterparts.
* Functions char and ascii are now respectively named chr and ord: it seems more natural to an old time Pascal programmer like me (us?).

#### Operators:

* Two operators have been added to AddyScript: === and !===. They make a comparison based on type and value.
* For a variable to be used as a collection or an object, it must first be set an empty initializer (of the right type). Automatic conversions to compound types are nomore handled by the runtime engine and statements like n[0] = x; or n.x = y; (where n has never been seen before) will lead to an error.

#### Classes:

* Two more kinds of members have been added to classes: properties and events. A property is a couple of methods (called accessors) used to safely access to a particular field. An event is a mean of informing the hole script that something is happened on a particular object so that interested parts of the script could properly react; it can be viewed as the combination of a field (the list of registered handlers) and 3 methods (one for registering handlers, one for unregistering handlers and one for triggering the event itself). AddyScript now propose a dedicated syntax for defining those kinds of members.
* Aside properties and events, AddyScript now supports operators overloading. An operator is a method with a special name that is invoked to handle arithmetic or logical operations on user-defined types. While looking essentially like a syntactic sugar in AddyScript, operator overloading really helps to write more readable scripts.
* Overriding methods must match the signature of the overriden one.
* Introduced to the language in release 0.9.6, a new variant of the import directive similar to the C# using directive allows one to use .Net and/or Mono types without having to provide their fully qualified name each time.

### Changes in the implementation

#### A new organization:

The AddyScript solution is now structured differently than before. The AddyScript.Enumerations and AddyScript.Exceptions have been removed. Their content is now dispatched into several other namespaces. I think there is no need to have a special namespace for enumerations and another one for exceptions. The AddyScript.Frames namespace is now called AddyScript.Runtime since it encompaces types that mostly intervent at runtime. The AddyScript.Statements and AddyScript.Expressions namespaces are now grouped into the AddyScript.Ast namespace.

#### A refactored lexer:

The NextToken method of the lexer is now splitted into several specialized methods. This is to increase readability and maintenability. The previous implementation (a huge switch block) was certainly more performant but as time elapsed and symbols was added to the language, it started to become very difficult to maintain. The new design makes it easier to add new functionnalities without wondering what that huge NextToken method does.

#### Refactored parsers:

Some methods in ExpressionParser and Parser classes have been rewritten to make them more readable and easier to maintain. Since AddyScript doesn't use a parser generator, it's essential that its lexer and parsers be easy to update by anyone that knows its syntax and intends to improve it. In accordance to that, I've also started to fill the Grammar page of this manual.

#### Several variable types:

In earlier versions of AddyScript, there were a class called Variable that was used to represent variables at runtime. A Variable was essentially a couple of attributes: a type attribute and a value attribute. For each operation, a switch was made on the type attribute to now how to handle the operation properly, thus making the addition of new data types or new operations quite complex. Now, each data type is mapped to a specific class in the AddyScript.Runtime.Dynamics namespace. All those classes are subclasses of the absract AddyScript.Runtime.Dynamics.Dynamic class. They provide methods to handle any operation at runtime in a more specialized manner than the older Variable class did. Some of those classes encapsulate instances of types defined in the AddyScript.Runtime.NativeTypes namespace. Notice that some of the so called native types may have equivalent in the .Net Framework 4.0 but since AddyScript targets .Net 2.0, it still needs to rely on a custom implementation of them.

#### The RuntimeServices class:

This class provides services that are typically needed at runtime by parts of the runtime engine that are out of the Interpreter class (like invoking a method on some object and getting the result). AddyScript.Runtime.RuntimeServices keeps a reference to the currently running interpreter and uses it to provide useful services to other parts of the scripting runtime. It also exposes an In and an Out properties respectively corresponding the the script's standard input and output devices.

#### A richer ScriptEngine class:

Yes, the ScriptEngine class now provides more useful methods than before. The complete set of ScriptEngine's methods is summarized in the following table:

|Method| Description                                                                                   |
|-|-----------------------------------------------------------------------------------------------|
|`Dynamic Execute(string command)`| Executes a command and returns the resulting value if the command is an expression.           |
|`Dynamic Invoke(string function, params object[] args)`| Invokes a scripted function from user code.                                                   |
|`Delegate GetDelegate(string function, Type delegateType)`| Generates a delegate that wraps a scripted function and that could be invoked from user code. |
|`static Program ParseString(string script)`| Parses an in-memory string containing a script and returns the corresponding AST.             |
|`static Program ParseFile(string path)`| Parses a file containing a script and returns the corresponding AST.                          |
|`static Expression ParseExpression(string text)`| Parses an in-memory string containing a single expression and returns the corresponding AST.  |
|`static void Execute(Program program, ScriptContext context)`| Interprets a previously parsed script.                                                        |
|`static void ExecuteString(string script, ScriptContext context)`| A shorthand for `Execute(ParseString(script), context)`                                       |
|`static void ExecuteFile(string path, ScriptContext context)`| A shorthand for `Execute(ParseFile(path), context)`                                           |
|`static Dynamic Evaluate(Expression expression, ScriptContext context)`| Evaluates a previously parsed expression and returns the result.                              |
|`static Dynamic EvaluateString(string expression, ScriptContext context)`| A shorthand for `Evaluate(ParseExpression(expression), context)`                              |
|`static void ExportXml(Program program, Stream output)`| Exports the AST to XML format.                                                                |
|`static void ExportXml(Program program, string fileName)`| Exports the AST to XML format.                                                                |
|`static string GenerateCode(Program program)`| Regenerates the source-code of a script from its AST.                                         |


Of course the first three methods are instance methods, and you must create an instance of the ScriptEngine class before invoking them.
The static version of the _Excecute_ method was initially called _Interpret_.
If you are not satisfied by the API provided by the ScriptEngine class, nothing prevents you from creating an instance of the interpreter class
and manage it yourself; this is exactly what the ScriptEngine class does most of the time.

[Home](README.md) | [Previous](aboutauth.md)
# Changes history

Many changes have occurred in AddyScript since its first release in 2009. It's reasonable to ask why.
Keep in mind that AddyScript is constantly evolving. Its syntax can change from one version to the next without prior notice.
If you downloaded an earlier version and started your work from there,
you can ignore the changes made in later versions and continue your work as you began.

Here is a quick summary of the most significant changes made to the project over the versions:

## Version 0.7

* Introduction of the **date** and **decimal** data types.

## Version 0.8

* Added support for object-oriented programming.
* Introduction of **try-catch-finally** statement for structured error handling.
* Added support for introspection, refined in version 0.9.9.9.
* Introduction of an **import** directive.
* Added support for date, string, and array arithmetic.
* Introduction of function references and inline function declarations.

## Version 0.9

* Localized error messages. From version 0.9.2, French language support was added. Spanish and Portuguese language support was added in version 0.9.9.9 (though obtained with the help of some online tools; the translation may not be perfect).
* Introduction of a C-like conversion syntax.
* Better handling of assignment and parameters passed by reference.
* An empty **for** loop is equivalent to an infinite loop.
* Better support for the **foreach** loop, including a protocol for making user-defined classes iterable (demonstrated in the _xrange.add_ sample script).
* Support for a **goto** jump and labels.
* Better control over the use of **continue**, **break**, **this**, **super** and **return** keywords.
* Complete redesign of the GUI, with the ability to load multiple scripts simultaneously.
* Ability to export the AST to XML.

## Version 0.9.1

* Ability to regenerate source code from the AST (mostly used for automatic code formatting). Improved in 0.9.2.
* Improved **try-catch-finally** instruction: no **return** in the **finally** block; no jump out of the **finally** block; jumps invoked from **try** or **catch** blocks are well remembered after the **finally** block is executed.

## Version 0.9.4

* Ability to create instances of .NET and COM types and invoke their members.
* Ability to convert a **closure** to a delegate and attach it as an event handler for a .NET object.

## Version 0.9.4.1

* Reworked external functions functionality (were initially used as a mean to invoke static .NET classes methods from a script, were then revamped as a mea to invoke to target native libraries functions).

## Version 0.9.5

* Replacement of the initial (PHP-like) **array** data type with a more robust collections framework made of the **list**, **map**, **set**, **queue**, and **stack** data types.
* Introduction of the **long**, **rational**, and **complex** data types; **long** maps to a large integer type (Initially AddyScript used to provide its own implementation of _BigInteger_ to handle this. That implementation was latterly replaced by the standard _BinInt_ type).
* Introduction of new class members: **properties**, **events**, and **operators**.
* Redesign of the **decimal** data type to map to a (Java-like) big decimal type.
* A search interface for the ScriptEngine class.
* Replacement of the **local** keyword by the **var** keyword for local variable declaration.
* Introduction of constant declaration and the **const** keyword (which was just a reserved word in older versions).
* Introduction of an interactive shell (asis).
* Improved closure handling with support for automatically capturing the local variables of the block in which the closure is defined.

## Version 0.9.9

* Added support for declaring indexers in user-defined classes.
* Added support for importing and/or instantiating .NET generic types.
* Ability to overload postfix unary operators in user-defined classes.
* Ability to invoke a base type property or indexer in derived classes.
* Ability to invoke functions with explicitly named parameters in random order.
* Migration from .NET Framework 4.x to .NET 8.0.
* Introduction of **mutable strings** and **string interpolation** as a language feature.
* Introduction of a **with** operator and **mutable copies of objects** as a language feature.

## Version 0.9.9.9

* Introduction of a **blob** type as an abstraction for arrays of byte.
* Introduction of a **tuple** data type and tuple initializers. This required revoking the syntax formerly used for complex number initializers and reusing it for tuple initializers.
* Added support for initializing multiple variables in a single statement using tuple deconstruction syntax.
* Added support for extracting and/or updating range of elements in collections using the new range operator (..).
* Introduction of a _spread_ operator (..) for expanding collections into individual elements.
* Added support for **literal complex numbers** using the new syntax _a + bi_ or _a - bi_.
* Added support for using the overridden version of _equals_, _hashCode_ and _toString_ methods in user-defined classes for indexing and/or search in collections.
* Migration of the GUI to [AvaloniaUI](https://avaloniaui.net/) for an improved cross-platform support.
* Introduction of **pattern matching** as a language feature using the **switch** and **is** operators.
* Added support for **destructuring objects** using a set initializer as a _lvalue_.
* A better handling of conversions from **float** or **decimal** to **rational**.
* Introduction of a **duration** data type that maps to the TimeSpan type for a better handling of date arithmetic.
* Fixed math functions to properly handle arguments of type **long**, **rational**, and **complex**.
* Added an alternative syntax for converting values to a given type using the type name as a function.
* Added support for using the **in** keyword as a reverse version of the **contains** operator.
* Moved the _join_ method from the **list** class to the **string** class to make it compatible with all iterables.
* Introduction of record types.
* Changed the **with** operator to be only compatible with record types.

## Version 1.0.1.0

* Fixed asis path resolution when a script is executed from the GUI.
* Added custom fonts to the GUI.

## Version 1.1.0.0

* Improved native terminal invocation in the macOS version of the GUI app using `osascript`.
* Fixed `asis` path resolution in the Windows version of the GUI app.
* Optimized `asis` executable lookup and improved macOS packaging by registering `asis` as a helper inside the application bundle.
* Added localized GUI error messages for cases where the `asis` executable cannot be found.

## Current development

* Refined constructor handling so derived classes implicitly invoke the superclass constructor when no explicit call is provided.
* Adjusted the handling of the **final** modifier in the parser and runtime class model.
* Continued refining macOS application bundle creation in the release workflow, including bundle layout and Finder integration.

<div markdown class="web-only">

[Home](README.md) | [Previous](aboutauth.md)

</div>

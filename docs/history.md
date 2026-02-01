# Changes history

Many changes have occurred in AddyScript since its first release in 2009. It's reasonable to ask why.
Simply keep in mind that AddyScript is constantly evolving. Its syntax can change from one version to the next without prior notice.
If you downloaded an earlier version and started your work from there, you can ignore the changes made in later versions and continue your work as you began.

Here is a quick summary of the most significant changes made to the the scripting engine over the versions:

* Introduction of the **date** and **decimal** data types: version 0.7.
* Added support for object-oriented programming: version 0.8.
* Introduction of **try-catch-finally** statement for structured error handling: version 0.8.
* Added support for introspection: version 0.8, refined in version 0.9.9.9.
* Introduction of an **import** directive: version 0.8.
* Added support for date, string and array arithmetic: version 0.8.
* Introduction of function references and inline function declarations: version 0.8.
* Localized error messages: version 0.9. From version 0.9.2, French language support was added. Spanish and Portuguese language support was added in version 0.9.9.9 (though obtained with the help of some online tools; the translation may not be perfect).
* Introduction of a C-like conversion syntax: version 0.9.
* Better handling of assignment and parameters passed by reference: version 0.9.
* An empty for loop is equivalent to an infinite loop: version 0.9.
* Better support for the **foreach** loop including an _iterator protocol_ for user-defined classes (demonstrated in the _xrange.add_ sample script): version 0.9.
* Support for a **goto** statement and labels: version 0.9.
* Better control over the use of **continue**, **break**, **this**, **super** and **return** keywords: version 0.9.
* Complete redesign of the GUI, with the ability to load multiple scripts simultaneously: version 0.9.
* Ability to export the AST to XML: version 0.9.
* Ability to regenerate source code from the AST (mostly used for automatic code formatting): version 0.9.1. Improved in 0.9.2.
* Improved **try-catch-finally** instruction: no **return** in the **finally** block; no jump out of the **finally** block; jumps invoked from **try** or **catch** blocks are well remembered after the **finally** block is executed.: version 0.9.1.
* Ability to create instances of .NET and COM types and invoke their members: version 0.9.4.
* Ability to convert a **closure** to a delegate and attach it as an event handler for a .NET object: version 0.9.4.
* Reworked external functions functionality (were initially used as a mean to invoke static .NET classes methods from a script, were then revamped as a mea to invoke to target native libraries functions): version 0.9.4.1.
* Replacement of the initial (PHP-like) **array** data type with a more robust collections framework made of the **list**, **map**, **set**, **queue**, and **stack** data types: version 0.9.5.
* Introduction of the **long**, **rational**, and **complex** data types; **long** maps to a large integer type (Initially AddyScript used to provide its own implementation of _BigInteger_ to handle this. That implementation was latterly replaced by the standard _BinInt_ type): version 0.9.5).
* Introduction of new kinds of class members: **properties**, **events**, and **operators**: version 0.9.5.
* Redesign of the **decimal** data type to map to a (Java-like) big decimal type: version 0.9.5.
* A search interface for the ScriptEngine class: version 0.9.5.
* Replacement of the **local** keyword by the **var** keyword for local variable declaration: version 0.9.5.
* Introduction of constant declaration and the **const** keyword (which was just a reserved word in older versions: version 0.9.5.
* Introduction of an interactive shell (asis): version 0.9.5.
* Added support for a better closure handling: version 0.9.5.
* Added support for declaring indexers in user-defined classes: version 0.9.9.
* Added support for importing and/or instantiating .NET generic types: version 0.9.9.
* Ability to overload postfix unary operators in custom classes: version 0.9.9.
* Ability to invoke a base type property or indexer in derived classes: version 0.9.9.
* Ability to invoke functions with explicitly named parameters in random order: version 0.9.9.
* Migration to .NET 8.0: version 0.9.9.
* Introduction of **mutable strings** and **string interpolation** as a language feature: version 0.9.9.
* Introduction of a **with** operator and **mutable copies of objects** as a language feature: version 0.9.9.
* Introduction of a **blob** type as an abstraction for arrays of byte: version 0.9.9.9.
* Introduction of a **tuple** data type and tuple initializers: version 0.9.9.9. This required to revoke the syntax formerly used for complex number initializers and reuse it for tuple initializers.
* Added support for initializing multiple variables in a single statement using tuple deconstruction syntax: version 0.9.9.9.
* Added support for extracting and/or updating range of elements in collections using the new range operator (..): version 0.9.9.9.
* Introduction of a _spread_ operator (..) for expanding collections into individual elements: version 0.9.9.9.
* Added support for **literal complex numbers** using the new syntax _a + bi_ or _a - bi_: version 0.9.9.9.
* Added support for using the overridden version of _equals_, _hashCode_ and _toString_ methods in user-defined classes for indexing and/or search in collections: version 0.9.9.9.
* Migration of the GUI to [AvaloniaUI](https://avaloniaui.net/) for an improved cross-platform support: version 0.9.9.9.
* Introduction of **pattern matching** as a language feature using the **switch** and **is** operators: version 0.9.9.9.
* Added support for **destructuring objects** using a set initializer as a _lvalue_: version 0.9.9.9.
* A better handling of conversions from **float** or **decimal** to **rational**: version 0.9.9.9.
* Introduction of a **duration** data type that maps to the TimeSpan type for a better handling of date arithmetic: version 0.9.9.9.
* Fixed math functions to properly handle arguments of type **long**, **rational**, and **complex**: version 0.9.9.9.
* Added an alternative syntax for converting values to a given type using the type name as a function: version 0.9.9.9.
* Added support for using the **in** keyword as a reverse version of the **contains** operator: version 0.9.9.9.
* Moved the _join_ method from the **list** class to the **string** class: version 0.9.9.9.
* Introduction of record types: version 0.9.9.9.

<div markdown class="web-only">

[Home](README.md) | [Previous](aboutauth.md)

</div>
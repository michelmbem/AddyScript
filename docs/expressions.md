# Controlling the program flow

### Variables

AddyScript is a dynamic language. This means that you don't have to declare variables. A variable starts existing as soon as you assign a value to it. This also means that variables are dynamically typed: they simply accept anything you put into them. As far as that aspect is concerned, a single variable can hold an integer at a certain time and hold a list of dates later.

Examples:

```Cpp
n = 10; // n is an integer
n = 7.5; // now n is of type float
n = now(); // n becomes a date
s = 'I like AddyScript'; // s is a string
t = "Hello y'all!"; // t is also a string
v = ["joe", 45, 18.2, 'house']; // v is a list, items are not necessarily of the same type
o = new {firstName = "James", lastName = "Bond", number = 007}; // o is an object, field names don't have to be known in advance
```

#### Variable's scope

As stated before, a variable exists from where it is initialized first until the end of the block in which it appears. If a variable is created at the root level, it will be accessible to the remain of the script. AddyScript doesn't check variable access from functions; neither does it check that for functions themselves. This means that a function can refer to a variable before the variable is actually created. As long as the function is not called before the creation of the variable, this will not lead to any error. However, you should avoid doing so in functions that you want to reuse out of their original script because there is no guarantee that the referred variable will exist in the calling context.

#### The **var** keyword

Even if AddyScript is able to dynamically declare variables, there are still some situations where you may need to explicitly declare them. In particular, when you want to avoid confusion between some local variable and an eventually predefined global homonymous one. This is where the **var** keyword comes into action. Just as in other C-based scripting languages, it is used to explicitly declare variables. Such a variable will hide any homonymous constant or variable declared in the global scope.

Example:

```Cpp
// a global variable named toto
toto = 10;

// a function in which toto is declared locally
function foo()
{
    var toto = 15;
    println('in foo, toto = ' + toto);
}

// a function in which toto is not declared locally
function bar()
{
    toto = 20;
    println('in bar, toto = ' + toto);
}

// var in action:
println('in main, toto = ' + toto);
foo();
println('back to main, toto = ' + toto);
bar();
println('back to main, toto = ' + toto);
```

### Constants

A constant is a read-only variable. That is: it's assigned a value once and cannot be altered afterward. You will typically declare constants using the **const** keyword like in the following example.

Example:

```Cpp
// declare a constant named MAX_ITEMS
const MAX_ITEMS = 10;

// try to alter the constant: this will lead to an error
MAX_ITEMS = 100;
```

#### The following constants are predefined in AddyScript:

|Constant|Value|Description|
|:-:|-|-|
|MININT|-2147483648|The minimum value for the **int** type.|
|MAXINT|+2147483647|The maximum value for the **int** type.|
|MINFLOAT|-1,79769313486232E+308|The minimum value for the **float** type.|
|MAXFLOAT|+1,79769313486232E+308|The maximum value for the **float** type.|
|PI|3,14159265358979|A numeric value used in geometry.|
|E|2,71828182845905|Exponential one.|
|EPSILON|4,94065645841247E-324 on my developer's machine|A value that varies from machine to machine and indicates which precision to use in arithmetic operations on floating-point numbers.|
|NINFINITY|(None)|A symbolic representation of the negative infinity.|
|PINFINITY|(None)|A symbolic representation of the positive infinity.|
|NAN|(None)|A value indicating that a floating-point number is in an invalid state.|
|I|Well, **i** itself|The imaginary one complex number.|
|MINDATE|01/01/0001 00:00:00|The minimum value for the **date** type.|
|MAXDATE|31/12/9999 23:59:59|The maximum value for the **date** type.|
|NEWLINE|"\r\n" on Windows, "\n" on Unix based systems|The sequence of characters used to mark the end of a line by the underlying platform.|

### Data types

Even if you don't have to explicitly define the type of your variables in AddyScript, they still have a type. In fact, AddyScript recognizes a set of 27 predefined data types. In addition to that, you can create your own classes and add them to the set of existing data types. Below are listed the AddyScript's built-in types and their meaning:

|Type|Description|.Net equivalent|
|:-:|-|-|
|void|The type of null or anything that's undefined|System.Void|
|bool|A boolean: **true** or **false**|System.Boolean|
|int|A 32-bits signed integer: ranging from -2147483648 to 2147483647. Operations on this type may produce a result of the **long** type as soon as an overflow occurs. Dividing two **int**s produces a result of the **rational** type when the dividend is not divisible by the divisor.|System.Int32|
|long|A limitless-precision signed integer.|System.Numerics.BigInteger|
|rational|A rational number (i.e. a fraction).|(None)|
|float|A double precision floating-point number.|System.Double|
|decimal|A limitless-precision decimal number. The scale is however limited to 50 decimal digits.|System.Decimal (but more like Java's BigDecimal)|
|complex|A double precision complex number.|System.Numerics.Complex|
|date|A date and/or time value.|System.DateTime|
|string|An immutable sequence of unicode characters.|System.String|
|list|A dynamically sized sequence of data items accessible by index in read and write mode|System.Collections.ArrayList, System.Collections.Generic.List&lt;T&gt;|
|map|A set of key-value pairs. Each value is accessible in read and write mode by its key.|System.Collections.HashTable, System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;|
|set|An emulation of the mathematical concept of a set.|System.Collections.Generic.HashSet&lt;T&gt;|
|queue|A _first-in-first-out_ type of collection.|System.Collections.Queue, System.Collections.Generic.Queue&lt;T&gt;|
|stack|A _last-in-first-out_ type of collection.|System.Collections.Stack, System.Collections.Generic.Stack&lt;T&gt;|
|object|An object in with dynamic fields. Fields are dynamic in number and type.|System.Object (but more like System.Collections.Generic.Dictionary&lt;System.String, System.Object&gt;)|
|resource|A reference to an imported .Net or COM object|System.Object|
|closure|A reference to a function or method, a callback.|System.Delegate|
|Exception|The representation of an error that occurs at runtime.|System.Exception|
|Attribute|||
|TypeInfo|A set of information describing a type. Used for introspection.|System.Type|
|MemberInfo|A set of information describing a class member, the base class of FieldInfo, Property, MethodInfo and EventInfo. Used for introspection.|System.Reflection.MemberInfo|
|FieldInfo|A set of information describing a field. Used for introspection.|System.Reflection.FieldInfo|
|PropertyInfo|A set of information describing a property. Used for introspection.|System.Reflection.PropertyInfo|
|MethodInfo|A set of information describing a method. Used for introspection.|System.Reflection.MethodInfo|
|EventInfo|A set of information describing an event. Used for introspection.|System.Reflection.EventInfo|
|ParameterInfo|A set of information describing a parameter. Used for introspection.|System.Reflection.ParameterInfo|

### Literal values

Depending on their type, literal values have the following forms:

* **Null pointer**: the **null** keyword.

* **Boolean**: the **true** or **false** keyword.

* **Integer**: a sequence of decimal digits (i.e. 0 to 9) or a sequence of hexadecimal digits (i.e. 0 to 9, 'A' to 'F' or 'a' to 'f') prefixed with "0X" or "0x" (**e.g.**: `154`, `0XF5D4`).

* **Long integer**: a literal integer with the 'l' or 'L' suffix or simply a very large literal integer.

* **Floating-point number**: a sequence of decimal digits optionally followed by a dot and another sequence of decimal digits, optionally followed again by 'e' or 'E' plus a '+' or '-' sign plus another sequence of decimal digits and optionally terminated by a 'f' or 'F' suffix. (**e.g.**: `0.5`, `1F`, `14.5e33`, `735e-3`, `88.33f`, `1.5e+6F`).

* **Decimal number**: just like floating-point numbers with a mandatory 'd' or 'D' suffix.

* **Date**: any valid date between backticks (**e.g.**: \``2008-04-11`\`, \``2:30 PM`\`, \``05/18/2009 13:04`\`).

* **String**: a sequence of unicode characters between single or double quotes. When a backslash (\\) appears in a string, it alters the meaning of the following characters. Combinations of backslash and its followers are called **escape sequences** and have the following meaning:

    * **\\\\**: a literal backslash
    * **\\'**: a single quote (not needed in strings wrapped double quotes)
    * **\\"**: a double quote (not needed in strings wrapped in single quotes)
    * **\\t**: a horizontal tabulation
    * **\\v**: a vertical tabulation
    * **\\r**: a carriage return
    * **\\n**: a line break
    * **\\f**: a page break
    * **\\b**: the backspace
    * **\\a**: a beep
    * **\\xnn** (where each **n** is an hexadecimal digit): any ascii character
    * **\\unnnn** (where each **n** is an hexadecimal digit): any unicode character

    **e.g.**: `'Hello World!'`, `"Joe's dog's bell"`, `"C:\\Documents and Settings\\Addy"`, `'Living\r\nLa vida\tloca'`.

    **Notes**:

    1. In literal numeric values represented with decimal digits, underscores (_) can be inserted between the digits to group them (in thousands for example) and make the number more human readable. There is no particular rule on how to group them but it will typically be 3-by-3 (**e.g.**: `21_345_986`, `9_876_544_785`, `6_438.59e+33`).

    2. Basically in AddyScript literal string values are not allowed to span over multiple lines of code. However it can be necessary in certain circumstances to define a literal string constants that wraps all its content in the source code, including line-breaks and tabulations. That kind of literal string value is called a **verbatim string** and is declared in AddyScript using the '@' prefix. In a **verbatim string**, **escape sequences** are not needed at all (even if they are still recognized). The only character that needs to be escaped is the string wrapper itself. This is done by doubling it (**e.g.**: `@'Say ''Hello'' to my friend Jonathan'`, `@"C:\MyMovies\""My Bad Movie.mp4"""`).

    3. Some literal string values embed expressions between curved braces into them. Those expressions are to be evaluated and replaced within the string by their value at runtime. The rendered string will be made of the static parts of its initial form concatenated with the results of the evaluation of embedded expressions. That kind of literal string value is called a **mutable string** and has to be prefixed with a dollar-sign (\$). Each embedded expression in a **mutable string** can be followed by a format or length specification within the same pair of curved braces. The overall protocol to follow is exactly the same than for a call to the builtin **format** function (in fact a mutable string is translated at runtime to a call to **format**). The process of rendering **mutable strings** is called **string interpolation**. **mutable strings** can also be verbatim; in that case they start with both dollar (\$) and at (@) signs  (**e.g.**: `$'item number {i}'`, `$"sine of PI is: {sin(PI)}"`, `$@'{emp.name} is a ''{emp.jobTitle}'' since {emp.hireDate:d}'`, `$@"movie ""D:\{movieDir}\{movieFile.Name}""" is {movieLen,3} minutes long`).

### Initializers

Initializers are like literal values for composite types: they provide initial value to them in a single step. AddyScript provides initializers for 4 data types: complex numbers, lists, maps and sets. Depending on their type, initializers have the following forms:

* **Complex number**: a couple of floating-point numbers between parenthesis; the first number in the couple represents the real part of the complex number while the second represents the imaginary part. **e.g.**: `(2, -1)` stands for 2-i; `(1, 3)` stands for 1+3i.

* **List**: a sequence of expressions (literal or not) between square braces separated by commas (**e.g.**: `[4, 5, 'joe', 'adam', true, 0.5]`).

* **Map**: a sequence of key-value couples between curved braces separated by commas. Each couple has the form: key =&gt; value where key and value are both expressions. **e.g.**: `{'name' =&gt; 'joe', 'age' =&gt; 18, 'job' =&gt; 'student'}`.

* **Set**: a sequence of expressions between curved braces separated by commas. **e.g.**: `{'one', 'two', 'three'}`.

    **Note**: An empty map initializer must have this form: `{=&gt;}`. This helps to make a difference between an empty map initializer and an empty set initializer.

### Type checking

You can check the type of an expression by using the **is** operator. Here is an illustration:

```Cpp
lst = [5, 'andy', now(), PI, new Exception("")];

foreach (item in lst)
    if (item is int)
        println('int');
    else if (item is float)
       println('float');
    else if (item is date)
       println('date');
    else if (item is Exception)
       println('Exception');
    else
       println('something else');
```

This also works with user-defined classes and takes inheritance into account: if B inherits A then _'instance_of_B is A'_ returns **true**.

### Conversion

For some operations, data are automatically converted to the right type. But this is not always the case. In the case where you have to manage conversion by yourself, use the C language conversion syntax like in the following example:

```Cpp
d = (date)'5/12/1980'; // d is a date
n = (decimal)'9876543210'; // n is a decimal
```

Remember that this doesn't work for custom classes. But most of the time, it will not be required for them since AddyScript uses duck-typing.

### Operators

Below are listed AddyScript's operators with their meaning:

|Operator|Desciption|
|:-:|-|
|+ (unary)|Identity, does nothing|
|- (unary)|Opposite; transforms negative values to positive and vice-versa|
|!|In the prefix form, it's the logical negation (it returns **true** if the operand evaluates to **false** and vice-versa). In the suffix form, it checks for non-emptiness: it throws an exception if its operand is **null** or an empty collection or string|
|~|Bitwise complement|
|++|Increment; can be prefix or postfix|
|--|Decrement; can be prefix or postfix|
|\+ (binary)|Addition|
|- (binary)|Subtraction|
|*|Multiplication|
|/|Division|
|%|Remainder of a division|
|**|Exponentiation|
|&lt;&lt;|Bitwise shift left|
|&gt;&gt;|Bitwise shift right|
|==|Equality test: tries to convert both operands to the same type before comparing them. Returns **true** anytime both operands can be considered as representing the same value. Returns **false** when both operands cannot be converted to the same type.|
|!=|Difference test: tries to convert both operands to the same type before comparing them. Returns **false** anytime both operands can be considered as representing the same value. Returns **true** when both operands cannot be converted to the same type.|
|===|Equality test: returns **true** if both operands are of the same type and have the same value. Returns **false** otherwise.|
|!==|Difference test: returns **true** if both operands are of the different types or have different values. Returns **false** otherwise.|
|&lt;|... is less than ...|
|&lt;=|... is less than or equal to ...|
|&gt;|... is greater than ...|
|&gt;=|... is greater than or equal to ...|
|&|Logical 'AND'|
|\||Inclusive logical 'OR' (returns **true** whenever one of its operands evaluates to **true**)|
|^|Exclusive logical 'OR' (only returns **true** when both operands have different logical values)|
|&&|Logical short-circuiting 'AND'; the second operand is not evaluated if the first is **false**|
|\|\||Logical short-circuiting 'OR'; the second operand is not evaluated if the first is **true**|
|**startswith**|String comparison operator, checks that the first operand starts with the second|
|**endswith**|String comparison operator, checks that the first operand ends with the second|
|**contains**|For strings, it checks that the second operand is part of the first. For collections, it checks that the collection in the left contains the item in the right|
|**matches**|String comparison operator, checks that the first operand is a match of the regular expression represented by the second|
|=|Simple assignment; can be combined with a binary operator like in `x += y` (a shortcut for `x = x + y`) or in `x \|= y` (a shortcut for `x = x \| y`)|
|?:|A C-like conditional ternary operator: `x ? y : z` is a shortcut for `if (x) y else z`|
|??|returns the first operand if it's not empty (**i.e.** **null**, an empty collection or an empty string); returns the second otherwise|
|()|Parenthesis are used to break precedence rules and force an expression to be evaluated in a certain way|
|[]|Square braces are used to access lists and maps items. They can also be used to extract a single character from a string. If a native type exposes an indexer, objects of this type will support the [] operator too|
|**switch**|Pattern matching operator. We'll look closer at this in the next sections|
|**with**|Copy with modified properties operator. We'll look closer at this in the next sections|

### Operator precedence

Operator precedence in AddyScript can be summarized in the following terms:

From the lowest to the highest priority, we have:

1. Assignment: =, +=, -=, *=, /=, %=, **=, &=, |=, ^=, &lt;&lt;=, &gt;&gt;=, ??=

2. The conditional ternary operator: ?:

3. Conditional binary operators: &, |, ^, &&, ||, ??

4. Relational operators: ==, !=, ===, !===, &lt;, &lt;=, &gt;, &gt;=, **startswith**, **endswith**, **contains**, **matches**, **is**

5. Addition and subtraction: +, -

6. Multiplication, division and bitwise shift operators: *, /, %, &lt;&lt;, &gt;&gt;

7. Exponentiation: **

8. Postfix unary operators: ++, --, !

9. Prefix unary operators: +, -, !, ~, ++, --

10. Wrapping and special binary operators: (), [], **switch**, **with**

### Expressions

An expression is any combination of operands and operators that can produce a value. An operand can be a literal value, a named constant, a reference to a variable, a reference to a list item, a reference to a field or property, a function or method call, the keyword **this**, an assignment, a unary expression, a binary expression, a ternary expression or any of these into parenthesis.

### Reading and printing values from/to the console

As you may have already guessed, you typically read a value from the console in AddyScript by using the **readln** function. It lets the user type a string and returns that string once the user presses on the [Return] key. The returned string can be converted to any type following your needs and expectations. **readln** accepts an optional parameter, a string that would be displayed as a prompt if given.

Similarly, you print values to the console in AddyScript by using the **print** and **println** functions. Both functions are similar except that **print** remains on the same line after printing a value while **println** automatically skips to the next line. Both functions accept a variable number of arguments. The first argument, if present, represents a format string; any _{n}_ sequence (n being an integral number) in that string will be replaced with one of the corresponding following argument (0 for the second, 1 for the third and so on...). **print** requires at least on argument while **println** can be used without any argument. When used without an argument, **println** simply skips to the next line.

If you are integrating AddyScript in your own system, you could change the meaning of those statements and make **print** and **println** display a popup window for example.

[Home](README.md) | [Previous](anatomy.md) | [Next](flow-control.md)
# Special types

AddyScript supports a number of special data types that can be used for various purposes. These are structured data types that behave like primitive types. They are described below:

### Rational Numbers

A rational number is a pair of integers resulting from their division. The first member of the pair called **numerator** can be of any sign while the second member called **denominator** is always positive. The main purpose of defining a rational number type in AddyScript is to provide better handling of integer division. Thus, rational numbers are represented in AddyScript by the **rational** data type. Variables of this type can be used in arithmetic operations like other numeric data. However, there is no literal or initializer for the **rational** type. The only way to get a rational number is to divide two integers. Also note that some operations on rational numbers can return an integer. Here is an example of a script using rational numbers:

Example:

```JS
a = 3/4;
b = 1/4;

println('the numerator of {0} is {1}', a, a.num);
println('the denominator of {0} is {1}', a, a.den);
println('the inverse of {0} is {1}', a, a.inverse());
println('the inverse of {0} is {1}', b, b.inverse());
println('{0} + {1} = {2}', a, b, a + b);
println('{0} - {1} = {2}', a, b, a - b);
println('{0} * {1} = {2}', a, b, a * b);
println('{0} / {1} = {2}', a, b, a / b);
println('{0} ** 2 = {1}', a, a ** 2);
println('sign({0}) = {1}', a, sign(a));
println('abs({0}) = {1}', a, abs(a));
```

#### Rational Number API

The following table summarizes the members of the **rational** type and their usage:

|Member|Nature|Description|
|-|-|-|
|`int num { read; }`|property|Gets the numerator of the target rational number.|
|`int den { read; }`|property|Gets the denominator of the target rational number.|
|`rational\|int inverse()`|method|Gets the inverse of the target rational number.<br>This can be another rational number or an integer.|

### Complex Numbers

A complex number is a pair of floating-point numbers treated as a single unit. The first member of the pair is called the **real part** while the second is called the **imaginary part**. AddyScript supports complex numbers as a primitive type. This type is represented by the **complex** class. Variables of this type can be used in arithmetic operations like other numeric data types (of course, **complex** is a numeric data type). There is also a predefined constant in AddyScript called **I** that is equal to the complex number that has 0 as its real part and 1 as its imaginary part. The _sqrt_ (square root) function always returns a complex number when its argument is negative (for example, it returns **I** for -1). There is no literal for the **complex** type but there are complex initializers. A complex initializer is a pair of expressions in parentheses separated by a comma. Here is an example script using complex numbers:

Example:

```JS
a = (2, -1); // 2-i
b = (1, 2); // 1+2i

println('the real part of {0} is {1}', a, a.real);
println('the imaginary part of {0} is {1}', a, a.imag);
println('the conjugate of {0} is {1}', a, a.conjugate());
println('{0} + {1} = {2}', a, b, a + b);
println('{0} - {1} = {2}', a, b, a - b);
println('{0} * {1} = {2}', a, b, a * b);
println('{0} / {1} = {2}', a, b, a / b);
println('{0} ** 2 = {1}', a, a ** 2);
println('abs({0}) = {1}', a, abs(a));
```

#### Complex Number API

The following table summarizes the members of the **complex** class and their usage:

|Member|Nature|Description|
|-|-|-|
|`float real { read; }`|property|Gets the real part of a complex number.|
|`float imag { read; }`|property|Gets the imaginary part of a complex number.|
|`complex conjugate()`|property|Gets the conjugate of a complex number.|

### Dates

Date/time values ​​are instances of the **date** class. You will typically create **date** instances either by calling the global **now** function (which returns the current date and time) or by using a date literal. A date literal is anything enclosed in backticks (\`) that can be translated by the .Net runtime to a value of type _System.DateTime_. They typically conform to the date format of the local culture (**e.g.**: \``03/03/1980`\`, \``nov, 16 2002`\`). Here is an example of a script that manipulates dates:

Example:

```JS
println("hello! it's {0:t} o'clock", now());
d = (date)readln("what's your birth date? ");
println("it was a " + d["weekday"]);
today = now().$date;
println("you are {0} years old now", today.subtract(d, "year"));
d = d.add(1, "year");
println("your first birthday was on {0:d}", d);
println("it's happened since {0} days", today - d);
```

#### Date API

The **date** class supports the following operators:

|Operator|Operands|Description|
|:-:|-|-|
|\+|A date to the left and a float to the right|Adds a number of days to a date.<br>This number of days may not necessarily be integral.|
|\-|Two dates|Computes the difference in days of two dates.<br>The result may have a decimal part representing hours, minutes, seconds and milliseconds.|

In addition to those operators, the **date** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`date of(params int[] values)`|static method|A static factory method for creating dates.<br>Accepts 3, 4, 6 or 7 arguments (all of the **int** type).<br>The arguments are interpreted like this:<ul><li>3 arguments: year, month, day.</li><li>4 arguments: hour, minute, second, millisecond.</li><li>6 arguments: year, month, day, hour, minute, second.</li><li>7 arguments: year, month, day, hour, minute, second, millisecond.</li></ul>|
|`int\|string [string part]`|indexer|Extracts some part from the target date object. Known parts are: "year", "month", "yearday", "weekday", "day", "hour", "minute", "second" and "millisecond".|
|`date $date { read; }`|property|Extracts the "date" component (in the strict sense of the term) of a date object.|
|`date time { read; }`|property|Extracts the "time" component of a date object.|
|`long ticks { read; }`|property|Gets the number of ticks stored in the target date's instance.|
|`date add(int amount, string unit)`|method|Adds some amount of the given unit to the target date object and returns an altered copy of it. The target itself remains unchanged. Accepted units are: "year", "month", "day", "hour", "minute", "second" and "millisecond".|
|`date addTicks(long ticks)`|method|Adds some ticks to the target date object and returns an altered copy of it. The target itself remains unchanged.|
|`int subtract(date d, string unit)`|method|Computes the difference in the given unit between the target date object and its first argument. Accepted units are: "year", "month", "day","hour", "minute", "second" and "millisecond".|

### Strings

In AddyScript, sequences of characters more commonly called _strings_ are instances of the **string** class. You can obtain a string in various ways such as using a literal string value, invoking the global **format** or **readln** functions, invoking the "toString" method of any object and so on. In fact, the **string** class is one of the more commonly used data type in AddyScript (and I think, in any scripting language). This is why it exposes a wide range of methods. Here is an example of a script that uses strings:

Example:

```JS
s = readln("Type some text: ");

println("Lower case: " + s.toLower());
println("Upper case: " + s.toUpper());
println("Words: " + s.split(@"\s+").join(", "));

w = readln("Type some word: ");

if (s startswith w)
    println(w + " is at the beginning of the text");
else if (s endswith w)
    println(w + " is at the end of the text");
else if (s contains w)
    println(w + " is contained in the text at position " + s.indexOf(w));

println("Replacing " + w + " by hello gives: " + s.replace(w, "hello"));

readln("Press [Return]");

println("Those are substrings of " + s);

for (i = 0; i < s.length - 1; ++i)
    for (j = 1; j <= s.length - i; ++j)
       println(s.substring(i, j));
```

#### String API

The **string** class supports the following operators:

|Operator|Operands|Description|
|:-:|-|-|
|\+|Two string<br>or<br>A string on one side and anything else on the other side|Concatenates two strings.<br>Automatically casts to string any argument that's not a string by invoking its "toString" method.|
|\*|A string on one side and an integer on the other side|Concatenates a string to itself the given number of times.|
|startswith|Two strings|Returns **true** if the left string starts with the right string.<br>Returns **false** otherwise.|
|endswith|Two strings|Returns **true** if the left string ends with the right string.<br>Returns **false** otherwise.|
|contains|Two strings|Returns **true** if the right string is a substring of the left string.<br>Returns **false** otherwise.|
|matches|Two strings|Returns **true** if the left string is a match for the regular expression contained in the right string.<br>Returns false otherwise.|
|\[int\]|A string to the left and an integer between brackets (called the **index**)|Gets a single character from the target string (the character is returned as a one-character string).<br>A negative index indicates that the character should be searched from the end of the string back to the beginning.|
|\[int..int\]|A string to the left and a pair of integers separated by a double-dot (..) between brackets (representing a range, the first integer is the range **lower bound**, the second is the range **upper bound**)|Gets a slice (or substring) of the target string.<br>Negative bounds are evaluated relative to the length of the string.<br>Both bounds are optional.<br>If the lower bound is omitted, it's automatically replaced with 0.<br>A missing upper bound will be replaced by the length of the string.<br>If both bounds are omitted, the entire target string is returned.|

In addition to those operators, the **string** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int length { read; }`|property|Gets the length of the string.|
|`int indexOf(string value, int start = 0, int length = 0)`|method|Searches for a substring and returns its position in the target string if found or -1 otherwise. The optional "start" and "length" parameters tell which part of the string to search. if "start" is negative, it will be evaluated modulo the total length of the target string. if "length" is negative or zero, it will be ignored.|
|`int lastIndexOf(string value, int start = -1, int length = 0)`|method|Searches for a substring backward and returns its position in the target string if found or -1 otherwise. The optional "start" and "length" parameters tell which part of the string to search. if "start" is negative, it will be evaluated modulo the total length of the target string. if "length" is negative or zero, it will be ignored.|
|`string toLower()`|method|Converts the target instance to lowercase.|
|`string toUpper()`|method|Converts the target instance to uppercase.|
|`string capitalize()`|method|Converts the first character of the target instance to uppercase.|
|`string uncapitalize()`|method|Converts the first character of the target instance to lowercase.|
|`string substring(int start, int length = 0)`|method|Extracts a substring (or slice) of the given length from the target string at the given position. If "position" is negative, it is evaluated modulo the length of the target string. If "length" is omitted (or 0 or negative), the part of the target string that's to the right of the given position is returned.|
|`string insert(int index, string value)`|method|Inserts a string into the target instance at the given position. If "position" is negative, it is evaluated modulo the length of the target string.|
|`string remove(int index, int count = 0)`|method|Removes "count" characters from the target string starting at the position indicated by "index" or simply truncates the target string at that position if "count" is omitted (or negative or 0). If "index" is negative, it is evaluated modulo the length of the target string.|
|`string replace(string pattern, string value)`|method|Replaces each occurrence of the given pattern (a regular expression) with "value" in the target string.|
|`string ltrim(string chars = " ")`|method|Removes each of the given characters from the left of the target string.|
|`string rtrim(string chars = " ")`|method|Removes each of the given characters from the right of the target string.|
|`string trim(string chars = " ")`|method|Removes any of the given characters from both ends of the target string.|
|`string lpad(int width, string padding = " ")`|method|Repeatedly adds the given character to the left of the target string until it reaches the length specified by "width".|
|`string rpad(int width, string padding = " ")`|method|Repeatedly adds the given character to the right of the target string until it reaches the length specified by "width".|
|`list split(string pattern = @"\s+")`|method|Creates a list of substrings of the target string separated by the given separator. The separator must be a regular expression.|

**Note**: none of the above methods alters the target string. They simply create a modified copy of it return that copy. The original string remains unchanged.

### Blobs

A **blob** is AddyScript's abstraction of a byte array. Blobs are especially useful when it comes to using methods in .Net classes that take a byte array as an argument (such as the _Read_ and _Write_ methods of the _System.IO.Stream_ class). Blobs have a lot in common with strings, but unlike strings, they are not immutable: their contents are meant to be changed.

There are several ways to get blobs, such as using a blob literal value (a string literal preceded by a "b" or "B"), or invoking the static method _blob::of_ (which expects the desired length in bytes as an argument), or invoking one of the other static methods "fromHexString" and "fromBase64String" of the blob class which as their name suggests, convert strings to blobs using base-16 or base-64 encoding. You can also convert a string to a blob, which will convert each of its characters to a byte.

Once a blob is created, you can access each of its bytes individually for reading or writing, you can get its length by reading the "length" property, you can fill it partially or completely with a byte of your choice, you can resize it or copy it to another blob at a particular position. You can also create slices of blobs like you do with strings.

Here is an example script that manipulates blobs:

```JS
b1 = b'Hello friends!';
b2 = blob::of(24);
println($'b1 = {b1}, b1.length = {b1.length}');
println($'b2 = {b2}, b2.length = {b2.length}');
println($'b2[0] = {b2[0]}, b2[-1] = {b2[-1]}');
println();

b2.fill(ord('a'), 0, 8);
b2.fill(ord('b'), 8, 8);
b2.fill(ord('c'), 16, 8);
println($'b2 = {b2}, b2[0] = {b2[0]}, b2[-1] = {b2[-1]}');
println();

b1.copyTo(b2);
println($'b2 = {b2}, b1 == b2 ? {b1 == b2}');
println();

b2 = b2[..b1.length];
println($'b2 = {b2}, b2.length = {b2.length}, b1 == b2 ? {b1 == b2}');
println();

b2 = blob::fromHexString('48656C6C6F20667269656E647321');
println($'b2 in base-16 = {b2.toHexString()}, b2.length = {b2.length}, b1 == b2 ? {b1 == b2}');
println();

b1 = blob::fromBase64String('SGVsbG8gZnJpZW5kcyE=');
println($'b1 in base-64 = {b1.toBase64String()}, b1.length = {b1.length}, b1 == b2? {b1 == b2}');
println();
```

#### Blob API

The **blob** class supports the following operators:

|Operator|Operands|Description|
|:-:|-|-|
|\+|Two blobs|Concatenates two blobs.|
|\*|A blob on one side and an integer on the other side|Concatenates a blob to itself the given number of times.|
|contains|A blob to the left and an integer to the right|Returns **true** if the blob contains the given byte at any index.<br>Returns **false** otherwise.|
|\[int\]|A blob to the left and an integer between brackets (called the **index**)|Gets a single byte from the target blob (the byte is returned as an integer).<br>A negative index indicates that the byte should be searched from the end of the blob back to the beginning.|
|\[int..int\]|A blob to the left and a pair of integers separated by a double-dot (..) between brackets (representing a range, the first integer is the range **lower bound**, the second is the range **upper bound**)|Gets a slice of the target blob.<br>Negative bounds are evaluated relative to the length of the blob.<br>Both bounds are optional.<br>If the lower bound is omitted, it's automatically replaced with 0.<br>A missing upper bound will be replaced by the length of the blob.<br>If both bounds are omitted, the entire target blob is returned.|

In addition to those operators, the **blob** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int length { read; }`|property|Gets the length of the blob.|
|`blob of(int length)`|static method|Creates a blob with the desired length. The returned blob is initially filled with zeros.|
|`blob fromHexString(string hexString)`|static method|Creates a blob by converting the given string to a byte array. The string should be made of hexadecimal digits in odd number|
|`string toHexString()`|method|Gets a string that represents the target blob as a large hexadecimal integer|
|`blob fromBase64String(string base64String)`|static method|Creates a blob by converting the given string to a byte array. The string should be made of base 64 digits|
|`string toBase64String()`|method|Gets a string that represents the target blob as a large base 64 integer|
|`int indexOf(int byteValue, int start = 0, int length = 0)`|method|Searches for a byte and returns its position in the target blob if found or -1 otherwise. The optional "start" and "length" parameters tell which part of the blob to search. if "start" is negative, it will be evaluated modulo the total length of the target blob. if "length" is negative or zero, it will be ignored.|
|`int lastIndexOf(int byteValue, int start = -1, int length = 0)`|method|Searches for a byte backward and returns its position in the target blob if found or -1 otherwise. The optional "start" and "length" parameters tell which part of the blob to search. if "start" is negative, it will be evaluated modulo the total length of the target blob. if "length" is negative or zero, it will be ignored.|
|`void fill(int byteVale, int start = 0, int length = 0)`|method|Fills a blob with the given byte starting at position "start" and stopping at position "start" + "length". Both "start" and "length" are evaluated modulo the length of the blob. If "length" is negative, it is replaced with target.length - "start", where target is the blob on which the method is invoked|
|`void copyTo(blob other, int srcIndex = 0, int destIndex = 0, int length = 0)`|method|Copies one blob to another. The portion of the source blob to be copied is between the indices "srcIndex" and "srcIndex" + "length". The portion of the destination blob that will be affected is between the indices "destIndex" and "destIndex" + "length". Both blobs must be sufficiently long, otherwise an exception will be thrown.|
|`void resize(int newLength)`|method|Resizes a blob preserving its current content (as much as possible).|


[Home](README.md) | [Previous](flow-control.md) | [Next](col-obj.md)
# Inner functions

At the time of writing this manual page, AddyScript has a set of 37 predefined functions. We have already discovered some of them in the previous sections. Here is a more general overview of AddyScript's built-in functions:

### Utility functions

* `any eval(string expression)` : evaluates the expression contained in the given string and returns its value.

### Conversion functions

* `string chr(int ascii)` : gets the unicode character corresponding to the given code. The returned value is of type string.
* `int ord(string char)` : gets the unicode code of the first character of the given string. In fact, the string must be one character long.
* `blob pack(string fmt, ..values)` : packs several values in a binary string (a **blob**): a way to create structured data items in preparation for a call to a .Net method or a native function that expects a structured argument.
* `list unpack(string fmt, blob structure)` : unpacks the combined values ​​into a binary string following the given format.

### Math functions

* `float rand()` : generates a random float between 0.0 and 1.0.
* `int randint(int min, int max = 0)` : generates a random integer between min and max. If max is omitted, the random value will be generated between 0 and min. In all cases, the limits are always sorted so that max is greater than or equal to min.
* `float sin(float x)` : computes the sine of x.
* `float cos(float x)` : computes the cosine of x.
* `float tan(float x)` : computes the tangent of x.
* `float asin(float x)` : computes the arc sine of x.
* `float acos(float x)` : computes the arc cosine of x.
* `float atan(float x)` : computes the arc tangent of x.
* `float atan2(float y, float x)` : computes the arc tangent of y/x.
* `float sinh(float x)` : computes the hyperbolic sine of x.
* `float cosh(float x)` : computes the hyperbolic cosine of x.
* `float tanh(float x)` : computes the hyperbolic tangent of x.
* `float deg2rad(float x)` : converts x from degrees to radians.
* `float rad2deg(float x)` : converts x from radians to degrees.
* `float log(float x)` : computes the natural logarithm of x.
* `float log10(float x)` : computes the base-10 logarithm of x.
* `float log2(float x, float base = 2)` : computes the logarithm of x to the given base. base-2 is assumed by default.
* `float exp(float x)` : computes the exponential of x.
* `float|complex sqrt(any x)` : calculates the square root of x. Returns a complex number if x is negative. Returns a **float** value otherwise.
* `int sign(any x)` : determines the sign of x. returns -1 for negative, 0 for 0, and 1 for positive.
* `any abs(any x)` : determines the absolute value of x.
* `any min(..values)` : determines the minimum of several values.
* `any max(..values)` : determines the maximum of several values.
* `any trunc(any x)` : truncates x to its integer part.
* `any floor(any x)` : determines the floor of x (i.e.: the largest integer that's less than or equal to x).
* `any ceil(any x)` : determines the ceiling of x (i.e.: the smallest integer that is greater than or equal to x).
* `any round(any value, int precision = 0)` : gets value rounded to precision decimal digits.

### Factory functions

* `date now()` : gets the current date and time.
* `string format(string pattern, ..values)` : interpolates a string with the given pattern and values. using a mutable string is equivalent to invoking this function.

### I/O functions

* `void print(string pattern, ..values)` : prints a formatted message to standard output and stays on the same line.
* `void println(string pattern = '', ..values)` : prints a formatted message to standard output and moves to the next line.
* `string readln(string prompt = '')` : reads a string from the standard input device and returns it to the script. An optional prompt can be displayed.

[Home](README.md) | [Previous](col-obj.md) | [Next](userfunc.md)
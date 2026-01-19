# Inner functions

At the time of writing this manual page, AddyScript has a set of 42 predefined functions. We have already discovered some of them in the previous sections. Here is a more general overview of AddyScript's built-in functions:

### Utility functions

* `any eval(string expression)` : evaluates the expression contained in the given string and returns its value.
* `int hash(any first, ..more)` : combines the provided values to calculate their hash code. It accepts a maximum of 9 arguments. Only the first argument is required; the others are optional.

### Conversion functions

* `string chr(int ascii)` : gets the Unicode character corresponding to the given code. The returned value is of type string.
* `int ord(string chr)` : gets the Unicode order of the first character of the given string. In fact, the string must be one character long.
* `blob pack(string format, ..values)` : packs several values in a binary string (a **blob**): a way to create structured data items in preparation for a call to a .NET method or a native function that expects a structured argument.
* `blob unpack(string format, blob bytes)` : unpacks the combined values into a binary string following the given format.

### Math functions

* `float rand()` : generates a random float between 0.0 and 1.0.
* `int randint(int min, int max = 0)` : generates a random integer between _min_ (included) and _max_ (excluded). If _max_ is omitted, the random value will be generated between 0 and _min_. In all cases, the limits are always sorted so that _max_ is greater than or equal to _min_.
* `float|complex sin(float|complex x)` : computes the sine of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float|complex cos(float|complex x)` : computes the cosine of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float|complex tan(float|complex x)` : computes the tangent of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float|complex asin(float|complex x)` : computes the arc sine of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float|complex acos(float|complex x)` : computes the arc cosine of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float|complex atan(float|complex x)` : computes the arc tangent of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float atan2(float y, float x)` : computes the arc tangent of y/x.
* `float|complex sinh(float|complex x)` : computes the hyperbolic sine of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float|complex cosh(float|complex x)` : computes the hyperbolic cosine of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float|complex tanh(float|complex x)` : computes the hyperbolic tangent of x. Returns a **complex** if x is a **complex**, a **float** otherwise.
* `float deg2rad(float x)` : converts x from degrees to radians.
* `float rad2deg(float x)` : converts x from radians to degrees.
* `float|complex log(float|complex x)` : computes the natural logarithm of x.
* `float|complex log10(float|complex x)` : computes the base-10 logarithm of x.
* `float|complex log2(float|complex x, float base = 2)` : computes the logarithm of x to the given base. Returns a **complex** if x is a **complex**, a **float** otherwise. Base-2 is assumed by default.
* `float|complex exp(float|complex x)` : computes the exponential of x.
* `float|complex sqrt(any x)` : calculates the square root of x. Returns a **complex** if x is a **complex** or a negative value. Returns a **float** otherwise.
* `int sign(any x)` : determines the sign of x. Returns -1 for negative, 0 for 0, and 1 for positive.
* `any abs(any x)` : determines the absolute value of x. Returns a value of the same type than the argument.
* `any min(first, second, ..more)` : determines the minimum of two or more values.
* `any max(first, second, ..more)` : determines the maximum of two or more values.
* `float|decimal trunc(float|decimal x)` : truncates x to its integer part. Returns a value of the same type than the argument.
* `float|decimal floor(float|decimal x)` : determines the floor of x (i.e.: the largest integer that's less than or equal to x). Returns a value of the same type than the argument.
* `float|decimal ceil(float|decimal x)` : determines the ceiling of x (i.e.: the smallest integer that is greater than or equal to x). Returns a value of the same type than the argument.
* `float|decimal round(float|decimal value, int precision = 0)` : gets _value_ rounded to _precision_ decimal digits. The result is of the same type than the first argument.

### Factory functions

* `date now()` : gets the current date and time.
* `duration days(float value)` : creates a duration with the given number of days.
* `duration hours(float value)` : creates a duration with the given number of hours.
* `duration minutes(float value)` : creates a duration with the given number of minutes. 
* `duration seconds(float value)` : creates a duration with the given number of seconds.
* `duration milliseconds(float value)` : creates a duration with the given number of milliseconds.
* `string format(string pattern, ..substitutions)` : interpolates a string with the given pattern and values. using a mutable string is equivalent to invoking this function.

### I/O functions

* `void print(string pattern, ..substitutions)` : prints a formatted message to standard output and stays on the same line.
* `void println(string pattern = '', ..substitutions)` : prints a formatted message to standard output and moves to the next line.
* `string readln(string prompt = '')` : reads a string from the standard input device and returns it to the script. An optional prompt can be displayed.

<div class="web-only">

[Home](README.md) | [Previous](col-obj.md) | [Next](userfunc.md)

</div>
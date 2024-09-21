# Controlling the program flow

Like any other language, AddyScript provides means to control the program flow. The following sections describe those means.

### The If statement

It is used to execute a statement only if a certain condition is satisfied.
It has the following forms:

#### Form 1:

`if (expression) statement`

Example:

```Cpp
n = randint(20); // n is a randomly generated number between 0 and 20

if (n > 10)
    println('n is greater than 10');
```

#### Form 2:

`if (expression) statement1 else statement2`

Example:

```Cpp
i = randint(100); // i is a randomly generated number between 0 and 100

if (i % 2 == 0)
    println('even');
else
    println('odd');
```

**Note:** In all cases, embedded statements can also be if statements.

### The Switch statement

It is used to choose which statement to execute depending on the value of an expression. It has the following syntax:

```
switch (expression)
{
    case value1:
       statements;
       break;
    case value2:
    case value3:
    ...
    case valueN:
       statements;
       break;
    ...
    default:
       statements;
       break
}
```

The breaks are not mandatory but if a **break** is missing, the execution will resume starting from the following **case**. The default section is also optional but at least one **case** (or the *default*) must be handled.

Example:

```Cpp
result = (int)readln("What's your result? ");

switch (result)
{
    case 0:
       println('Null!');
       break;
    case 1:
    case 2:
    case 3:
       print('Very ');
    case 4:
       println('Bad!');
       break;
    case 5:
       println('Averrage!');
       break;
    case 6:
       println('Good!');
       break;
    case 7:
    case 8:
    case 9:
       println('Very Good!');
       break;
    case 10:
       println('Perfect!');
       break;
    default:
       println('Out of range');
       break;
}
```

### Pattern matching

So far we've seen the **switch** statement. It helps to choose which statement to execute according to the value of an expression. But the syntax is somehow cumbersome: we have to repeat the **case** keyword all the time. We also have to use **break**s to prevent the flow from resuming to the next **case** label. It also leaves us a very poor choice on the kind of operation that should be used to compare the value of our expression with that of each **case** label. That's the kind of problem that pattern matching solves. It associates a reacher syntax to the **switch** keyword which this time is used as an operator, thus helping us to build expressions that not only choose which action to do according to the value of an expression, but also returns a value. A pattern matching expression typically looks like this:

```
expression switch {
    pattern1 => result1,
    pattern2 => result2,
    ...
    patternN => resultN
};
```

**Notes**:

1. Each result in that syntax is an expression.
2. Patterns are of different kinds (see the table bellow).
3. There is no default pattern at all, there is just a special kind of pattern that matches everything thus acting like a default (or fallback) pattern.
4. A result can be produced in a block.
5. Throwing an exception is also a result.

Example 1:

```Cpp
n = (int)readln('Please type a number: ');

res = n switch {
    ..-1 => {
        println('below zero');
        return 'negative';
    },
    0..9 => 'from 0 to 9',
    10, 11, 12 => '10, 11 or 12',
    13, 14..17, 18 => '13 or between 14 and 17 or 18',
    x: 20 <= x && x < 30 => '20 to 29',
    30.. => '30 and above',
    _ => throw 'an exception for 19'
};

println(res);
```

Example 2:

```Cpp
o = new {name = 'my object', size = 18, color = 'blue'};

res = o switch {
	null => 'absolutely null',
	float => 'i\'m floating',
	Exception {message = 'something went wrong'} => __value.message,
	{name = 'my object', size = 18} => 'my object of size 18'
};

println($'o is {o}');
println($'the result with o is {res}');
```
The above examples showcase the different kinds of patterns. They are explained in the table below:

|Pattern|Syntax|Description|
|-|:-:|-|
|always pattern|an underscore (_)|Matches everything; usually acts as a default case|
|null pattern|the **null** keyword|Only matches null references|
|single value pattern|a literal **bool**, **int**, **long**, **float**, **decimal**, **date** or **string** value|Only matches the given value|
|range pattern|two literal **bool**, **int**, **long**, **float**, **decimal**, **date** or **string** values with a double-dot (..) between them; one of the bounds can be omitted; **e.g.**: `0..9` or `..-1` or `30..`|Matches values that are in the given range; if the lower bound is missing, the pattern matches everything that less than or equal to the upper bound; when it's the upper bound that's omitted, the pattern matches everything that greater than or equal to the lower bound|
|type pattern|any type name including user-defined classes|Matches objects of the given type; also matches instances of subclasses if the given type is a class|
|object pattern|a list of property initializers between curved braces optionally preceded by a type name; **e.g.**: `Exception {message = 'something went wrong'}`|Matches object of the given type (if a type name is given) that have same values for the listed properties|
|predicate pattern|an identifier followed by a colon (:) then by a logical expression; **e.g.**: `x: 20 <= x && x < 30`|That kind of pattern is a function with a single parameter; the initial identifier is the name you want to give to the parameter; it will hold the value of the expression that's being matched against the pattern; the following expression is just the function's body; The pattern is matched if the expression returns **true**|
|composite pattern|several pattern separated with commas (,); **e.g.**:`13, 14..17, 18`|Tries to match the expression with one of its components|

**Note**: at the left side of the arrow, the value of the expression that's being matched can be referred to as **__value**.

### Loops

The loops are used to repeat an action until a condition is satisfied.
In AddyScript, we have 4 different kinds of loop. Those are:

#### The While loop:

`while (expression) statement`

Example:

```Cpp
i = 1;
while (i <= 12)
{
    println('2 x {0} = {1}', i, 2*i);
    ++i;
}
```

#### The For loop:

`for (initialization; condition; increment) statement`

Example:

```Cpp
for (i = 1; i <= 12; ++i)
   println('2 x {0} = {1}', i, 2*i);
```

**Notes**:

1. In the above example _initialization_ and _increment_ are comma-separated lists of expressions; condition is a single logical expression.

2. The given example is equivalent to that of the while-loop.

#### The For-Each loop:

It has the following forms:

Form 1:

`foreach (identifier in expression) statement`

Example:

```Cpp
words = ['john', 'paul', 'second', 'the', 'pope'];

foreach (word in words)
   print(word + ' ');

println();
```

Form 2:

`foreach (key => value in expression) statement`

Example:

```Cpp
jobs = {'paul' => 'general manager', 'roland' => 'accountant', 'david' => 'driver'};

foreach (name => job in jobs)
   println(name + ' is a ' + job);
```

Even in the first form, there is an implicit key named **__key**. In both forms, the expression must return a **list**, a **map**, a **set**, a native object implementing the **IEnumerable** interface or an AddyScript object implementing the **iterator protocole**.

#### The iterator protocole:

For a class to implement the iterator protocole, it must expose 3 methods named **moveFirst**, **hasNext** and **moveNext** respectively. The role of **moveFirst** is to position the internal cursor on the first logical item of the collection (supposing that your class is a custom collection). **hasNext** is expected to return a boolean indicating whether or not iteration may continue. Finally, **moveNext** is responsible of moving the internal cursor forward, returning the value pointed by the cursor at each step. The _xrange.add_ sample script provided with the demo illustrates this feature.

#### The Do-While loop:

`do statement while (expression);`

Example:

```Cpp
i = 1;

do
{
   println('2 x {0} = {1}', i, 2*i);
   ++i;
} while (i <= 12);
```

### Jumps

Jumps are used to resume execution from elsewhere than the following statement. They are described in the table below:

|Jump statement|Syntax|Effect|
|-|-|-|
|continue|`continue;`|Skips to the next iteration of a loop before the end of the current iteration|
|break|`break;`|Exits a loop before the last iteration is reached.
Exits a switch block before the last statement is reached.|
|goto|`goto label;`|Unconditionnaly skips to the specified location|
|return|`return;` or `return expression;`|Exits a function returning a value or not.|
|throw|``throw exception;``|Raises an error and stops the execution of the script until the raised error is caught by a try-catch-finally statement.|

### Iteration methods

Some data types in AddyScript own methods that can be used to iterate on variables of those types. Such a method is generally equivalent to a loop except that it is more compact and can figure anywhere an expression is expected (something that loops cannot do). The following table summarizes AddyScript's iteration methods and the classes to which they belong.

|Method|Description|Example|
|-|-|-|
|`int int::times(closure action)`<br>`long long::times(closure action)`|Accomplishes the given action n times where n is the integer value on which the method is invoked.|`4.times(\|i\| => println('hello!'));`|
|`string string::each(closure action)`<br>`list list::each(closure action)`<br>`map map::each(closure action)`<br>`set set::each(closure action)`<br>`set queue::each(closure action)`<br>`set stack::each(closure action)`|Repeats the given action on each item (each character for strings or each key-value pair for maps) of the target object. When the target object is a map, the closure expects two arguments. In any other case, it expects a single argument.|`l = [4, 2, 5, 3, 8, 6];`<br>`sum = 0;`<br>`l.each(\|x\| => sum += x);`<br>`println(sum);`|
|`list list::eachIndex(closure action)`|Repeats the given action on each index of the target list.|`l = [4, 2, 5, 3, 8, 6];`<br>`l.eachIndex(\|i\| => l[i] *= 2);`<br>`prinln(l.join(', '));`|
|`map map::eachKey(closure action)`|Repeats the given action on each key of the target map.|`m = {'age' => 30, 'weight' => 80, 'height' => 170};`<br>`m.eachKey(\|k\| => println(k + ': ' + m[k]));`|
|`map map::eachValue(closure action)`|Repeats the given action on each distinct value of the calling map.|`m = {'age' => 70, 'weight' => 70, 'height' => 180};`<br>`m.eachValue(\|v\| => println(v + ': ' + m.keysOf(v)));`|
|`bool list::all(closure predicate)`<br>`bool set::all(closure predicate)`|Evaluates a predicate on each the items of a list or a set. Returns true if the predicate is true for all; returns false otherwise.|`s = {4, 2, 0, 8, 6};`<br>`b = s.all(\|e\| => e % 2 == 0);`<br>`if (b) println('all them are even');`|
|`bool list::any(closure predicate)`<br>`bool set::any(closure predicate)`|Evaluates a predicate on each the items of a list or a set. Returns true if the predicate is true for at least one of them; returns false otherwise.|`s = {4, 1, 2, 0, 3, 8, 6};`<br>`b = s.any(\|e\| => e % 2 == 1);`<br>`if (b) println('there is an odd one');`|
|`any list::first(closure predicate)`<br>`any set::first(closure predicate)`|Successively evaluates a predicate on every item of a list or a set, returning the first item on which the predicate evaluates to true.|`l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.first(\|x\| => x % 2 == 1);`<br>will return 5|
|`any list::last(closure predicate)`|Successively evaluates a predicate on every item of a list, returning the last item on which the predicate evaluates to true.|`l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.last(\|x\| => x % 2 == 1);`<br>will return 1|
|`int list::findIndex(closure predicate)`|Finds the index of the first item of a list that satisfies the given predicate.|`l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.findIndex(\|x\| => x % 2 == 1);`<br>will return 3|
|`any list::findLastIndex(closure predicate)`|Finds the index of the last item of a list that satisfies the given predicate.|`l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.findLastIndex(\|x\| => x % 2 == 0);`<br>will return 8|
|`list list::where(closure predicate)`<br>`set set::where(closure predicate)`|Successively evaluates a predicate on every item of a list (or a set), returning a list (or a set) of items on which the predicate evaluates to true.|`l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.where(\|x\| => x % 2 == 1);`<br>will return [5, 3, 7, 1]|
|`list list::select(closure transform)`<br>`set set::select(closure transform)`|Successively evaluates a predicate on every item of a list (or a set), returning a list (or a set) of items created from the original list's (or set's) items by its argument.|`s = {'nadia', 'dave', 'roland', 'rick', 'john'};`<br>`t = s.select(\|x\| => x.toUpper());`<br>returns {NADIA, DAVE, ROLAND, RICK, JOHN}|
|`any list::aggregate(any seed, closure aggregator)`<br>`any list::aggregate(any seed, closure aggregator)`|Generate a single value by aggregating the items of list; the _aggregator_ is a function that takes 2 arguments: an _accumulator_ and the current item; it generates the next value of the _accumulator_; parameter _seed_ is the initial value of the _accumulator_; **aggregate** returns the last value of the _accumulator_.|`l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`sum = l.aggregate(0, \|acc, val\| => acc + val);`<br>will return 36|
|`map list::groupBy(closure criterion)`|Groups the items of a list according to the given criterion and returns a map of sub-lists identified by the distinct group identifiers that were produced by the criterion.|`l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`g = l.groupBy(\|x\| => x % 2);`<br>will return {0 => [4, 0, 2, 8, 6], 1 => [5, 3, 7, 1]}|

**Note**: each of these methods always returns the object on which it's called, thus allowing chaining. Chaining is one of the principal interests of using iteration methods instead of simple loops.

[Home](README.md) | [Previous](expressions.md) | [Next](spec-types.md)
# Controlling the program flow

Like any other language, AddyScript provides means to control the program flow.
The following sections describe these means.

## The If-Else statement

The if-else statement is used to perform an action only if a certain condition is met.
An alternative action can be defined to be executed in case the condition is not met.
An if-else therefore has the following 2 possible forms:

### Form 1:

`if (condition) action`

```addyscript title="Example" linenums="1"
n = randint(20); // n is a randomly generated number between 0 and 20

if (n > 10)
    println('n is greater than 10');
```

### Form 2:

`if (condition) action else alternative_action`

```addyscript title="Example" linenums="1"
i = randint(100); // i is a randomly generated number between 0 and 100

if (i % 2 == 0)
    println('even');
else
    println('odd');
```

**Note**: In all cases, actions can be statement blocks or other if-else statements.
A block is a series of statements enclosed in curly braces.
It is treated by the interpreter as a single composite statement.
Use a block if you want your **if** or **else** section to perform multiple actions.

## The Switch statement

Like the if-else statement, the switch statement is used to choose the action to perform based on the value of an expression.
The main difference is that unlike the if-else statement, the switch statement is not limited to 2 alternatives.
Its syntax is as follows:

``` { .text .no-copy }
switch (expression)
{
    case value1:
       action1;
       break;
    case value2:
    case value3:
      action2;
      break;
    //...
    case valueN:
       actionN;
       break;
    //...
    default:
       defaultAction;
       break;
}
```

Breaks are not required but if a **break** is missing, execution will continue at the next **case** section.
The **default** section is also optional but at least one **case** section (or *default* section) must be present in the switch block.

```addyscript title="Example" linenums="1"
result = int(readln("What's your result? "));

switch (result)
{
    case 0:
       println('Null!');
       break;
    case 1:
    case 2:
    case 3:
       print('Very '); // This will finally print 'Very Bad!' as there is no break!
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

## Loops

Loops are used to repeat an action until a condition is met.
In AddyScript, we have four different kinds of loops. These are:

### The While loop:

It performs an action as long as a condition is satisfied. If the condition is initially unsatisfied, the action will never be performed.

Syntax:

`while (condition) action`

```addyscript title="Example" linenums="1"
i = 1;
while (i <= 12)
{
    println('2 x {0} = {1}', i, 2*i);
    ++i;
}
```

```text title="Output"
2 x 1 = 2
2 x 2 = 4
2 x 3 = 6
2 x 4 = 8
2 x 5 = 10
2 x 6 = 12
2 x 7 = 14
2 x 8 = 16
2 x 9 = 18
2 x 10 = 20
2 x 11 = 22
2 x 12 = 24
```

### The Do-While loop:

Just like the While loop, it performs an action as long as a condition is satisfied.
The action is performed before the condition is checked thus leading to the action always being performed at least once.

Syntax:

`do action while (condition);`

```addyscript title="Example" linenums="1"
i = 1;

do
{
   println('2 x {0} = {1}', i, 2*i);
   ++i;
} while (i <= 12);
```

### The For loop:

It's mainly used to perform an action for each value of a counter.

Syntax:

`for (counter_initialization; counter_limit_test; counter_increment) action`

```addyscript title="Example" linenums="1"
for (i = 1; i <= 12; ++i)
   println('2 x {0} = {1}', i, 2*i);
```

**Notes**:

1. In the above example, _counter_initialization_ and _counter_increment_ are single statements, but they could also be comma-separated lists of statements; _counter_limit_test_ on the other hand is always a single logical expression.
2. The example given is equivalent to the while loop example.

### The For-Each loop:

It's used to iterate over a collection of items, performing an action on each of them.

It has the following forms:

Form 1:

`foreach (item in collection) action`

```addyscript title="Example" linenums="1"
words = ['john', 'paul', 'second', 'the', 'pope'];

foreach (word in words)
   print(word + ' ');

println();
```

```text title="Output"
john paul second the pope
```

Form 2:

`foreach (key => value in collection) action`

```addyscript title="Example" linenums="1"
jobs = {'paul' => 'general manager', 'roland' => 'accountant', 'david' => 'driver'};

foreach (name => job in jobs)
   println(name + ' is a ' + job);
```

```text title="Output"
paul is a general manager
roland is a accountant
david is a driver
```

**Notes**:

* Even in the first form, there is an implicit key named **__key**.
* In both forms, the _collection_ must be a **string**, a **blob**, a **tuple**, a **list**, a **map**, a **set**, or an instance of a .NET class that implements the **IEnumerable** interface.
* It is possible to use the foreach loop to iterate over instances of user-defined classes that implement the _interator protocol_. We'll come back on this in the section dedicated to [inheritance and polymorphism](inheritance.md).

## Jumps

Jumps allow execution to resume from a location other than the next instruction. They are generally very useful for prematurely exiting from functions or loops. The table below describes them:

|Jump statement|Syntax|Effect|
|-|-|-|
|continue|`continue;`|Skips to the next iteration of a loop before the end of the current iteration|
|break|`break;`|Exits a loop before the last iteration is reached.<br>Exits a switch block before the last statement is reached.|
|goto|`goto label;`<br>or _(in a switch block)_<br>`goto case X;`<br>`goto default;`|Unconditionally jumps to the specified location.<br>In a switch block can also jump to one of the case labels.<br><sub>**Note**: A label is any identifier that's appears at the beginning of an instruction and is followed by a colon. **e.g.**: in `printout: println('hello');` _printout_ is the label.</sub>|
|return|`return;`<br>or<br>`return expression;`|Returns from a function with or without a value.|
|throw|`throw exception;`<br>or<br>`throw "some error message";`|Raises an error and stops the execution of the script until the raised error is caught by a **try-catch-finally** statement.|

## Pattern matching

So far, we've seen the **switch** statement and the possibilities it offers.
But its syntax is somewhat cumbersome: we have to repeat the **case** keyword a lot of times.
We also have to use **break** to prevent the flow of execution from continuing to the next **case** section.
This also leaves us with a very poor choice about what kind of operation to use to compare the value of our expression
with the value of each **case** label.  This is the kind of problem that pattern matching solves.
It combines a better matching syntax with the **switch** keyword, which this time is used as an operator,
helping us create expressions that not only choose what action to perform based on the value of an expression,
but also return a value. A pattern matching expression typically looks like this:

``` { .text .no-copy }
expression switch {
    pattern1 => result1,
    pattern2 => result2,
    ...
    patternN => resultN
};
```

**Notes**:

1. Each _result_ in this syntax is an expression.
2. Patterns come in different types (see the table below).
3. There is no default pattern at all, there is just a special type of pattern that matches everything, thus acting as a default (or fallback) pattern.
4. A result can be produced in a block, in which case the block must end with a **yield** statement which acts as a **return** statement in a function body. Using a **return** statement here will either fail or interrupt the flow of execution without setting the value returned by the block.
5. Throwing an exception is also a valid result.
6. A pattern can be supplemented with a **guard** which is a logical expression linked to the pattern by a **when** keyword.
7. The **guard** restricts the cases that should match by adding a condition to check.
8. During pattern evaluation the value of the tested expression is represented by the **__value** automatic variable.

Example 1:

```addyscript linenums="1"
n = (int)readln('Please type a number: ');

res = n switch {
    not >= 0 => {
        println('this is a block');
        yield 'negative';
    },
    >= 0 and <= 9 => 'from 0 to 9',
    10 or 11 or 12 => '10, 11, or 12',
    (>= 14 and <= 17) or 13 or 18 => '13 to 18',
    int when 20 <= __value && __value < 30 => '20 to 29',
    >= 30 => '30 and above',
    _ => throw 'an exception for 19'
};

println(res);
```

Example 2:

```addyscript linenums="1"
l = [
    new { name = 'cube', size = 18, color = 'blue' },
    'hello funny people, how funny are you?',
    new Exception('something went wrong'),
    null,
    PI,
    (8, 4, 6),
    now()
];

foreach (o in l) {
	res = o switch {
	    null => 'absolutely null',
	    float => "i'm floating",
	    Exception(message) => message,
	    object { name : 'cube', color : 'blue' } => 'a blue cube',
	    $'hello {quality} {target}, how {quality} are you?' => $'very {quality} {target} indeed!',
	    (8, _, 6) => 'a triplet that starts with 8 and ends with 6',
	    _ => 'did not match any pattern'
	};
	
	println($'o is: {o}');
	println($'result with o: "{res}"');
	println();
}
```

```text title="Output"
o is: <object {name = cube, size = 18, color = blue}>
result with o: "a blue cube"

o is: hello funny people, how funny are you?
result with o: "very funny people indeed!"

o is: Exception
result with o: "something went wrong"

o is:
result with o: "absolutely null"

o is: 3,141592653589793
result with o: "i'm floating"

o is: (8, 4, 6)
result with o: "a triplet that starts with 8 and ends with 6"

o is: 2026-01-26 15:11:01
result with o: "did not match any pattern"

```

The above examples showcase the different kinds of patterns. They are explained in the table below:

| Pattern                      | Syntax                                                                                                                                                                                                      | Description                                                                                                                                                                                                                                                                                                 |
|------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| always true pattern          | an underscore ( _ )                                                                                                                                                                                         | Matches everything; usually acts as the default. Should normally appear at the end of the list to avoid short-circuiting the flow of comparisons                                                                                                                                                            |
| null checking pattern        | the keyword **null**                                                                                                                                                                                        | Matches only expressions that evaluate to the null reference                                                                                                                                                                                                                                                |
| literal value pattern        | a literal **bool**, **int**, **long**, **float**, **decimal**, **date**, or **string**                                                                                                                      | Matches only expressions whose value is equal to its own                                                                                                                                                                                                                                                    |
| relational pattern           | a relational binary operator (one of: &lt;, &gt;, &lt;=, or &gt;=) followed by a literal **bool**, **int**, **long**, **float**, **decimal**, **date**, or **string**                                       | Matches expressions whose value have the relation indicated by its operator with its own value                                                                                                                                                                                                              |
| regex pattern                | the **matches** operator followed by a literal **string**                                                                                                                                                   | A particular kind of relational pattern that checks that the expression evaluates to a string that matches the given regular expression                                                                                                                                                                     |
| type pattern                 | any type name, including user-defined classes                                                                                                                                                               | Matches objects of the given type; also matches instances of subclasses if the given type is a class                                                                                                                                                                                                        |
| object pattern               | a curly-braced list of _property : pattern_ pairs optionally preceded by a type name; **e.g.**: `Exception { message : 'something went wrong'}` _(An Exception with "something went wrong" as its message)_ | Matches an object of the given type (if a type name is specified) with properties of the given names that match the corresponding patterns                                                                                                                                                                  |
| destructuring pattern        | a type name followed by a list of property names between parentheses; **e.g.**: `Exception(name, message)}`                                                                                                 | Matches the expression to be evaluated to an object of the specified type with properties defined by the given names. Any matching object is automatically destructured, and a variable with the same name as each of the specified properties is initialized with the value of the corresponding property. |
| negative pattern             | the keyword **not** followed by a child pattern; **e.g.**:`not int` _(anything but an integer)_                                                                                                             | Negates its child pattern.                                                                                                                                                                                                                                                                                  |
| logical pattern              | two elementary patterns joined with the **and**/**or** keywords; **e.g.**:`10 or 11`, `int and >= 0`                                                                                                        | Whith the **or** operator, tries to match the expression against at least one of its components. Whith the **and** operator, tries to match the expression against both components.                                                                                                                         |
| grouping pattern             | another pattern between parentheses; **e.g.**:`(>= 12 and <= 18) or ((>= 22 and <= 30) and not 25)` _(in the range 12 to 18 or in the range 22 to 30 except 25)_                                            | Changes the priority in which chained logical patterns are evaluated. By default they are evaluated left to right. Any pattern between parentheses in a chain is evaluated first.                                                                                                                           |
| positional pattern           | a coma seperated list of elementary patterns between parentheses (i.e.: a tuple of patterns).                                                                                                               | Matches expressions whose value is an iterable data item (one that can be used in a **foreach** loop) of the same length than the given list of patterns with items that match the elementary patterns at corresponding positions.                                                                          |
| string destructuring pattern | a mutable string in which all substitutions are variable references.                                                                                                                                        | Matches expressions whose value is a string that matches the regular expression generated from the given mutable string. In this regular expression, a capture group is generated from each substitution. If the same variable appears multiple times among the substitutions, all its occurrences will reference the same capture group, thus requiring the same substring to appear in different places within the input string.                                                                             |

<div markdown class="web-only">

[Home](README.md) | [Previous](expressions.md) | [Next](spec-types.md)

</div>
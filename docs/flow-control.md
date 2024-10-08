# Controlling the program flow

Like any other language, AddyScript provides means to control the program flow. The following sections describe these means.

### The If-Else statement

The if-else statement is used to perform an action only if a certain condition is met. An alternative action can be defined to be executed in case the condition is not met. An if-else therefore has the following 2 possible forms:

#### Form 1:

`if (condition) action`

Example:

```JS
n = randint(20); // n is a randomly generated number between 0 and 20

if (n > 10)
    println('n is greater than 10');
```

#### Form 2:

`if (condition) action else alternative_action`

Example:

```JS
i = randint(100); // i is a randomly generated number between 0 and 100

if (i % 2 == 0)
    println('even');
else
    println('odd');
```

**Note**: In all cases, actions can be statement blocks or other if-else statements. A block is a series of statements enclosed in curly braces. It is treated by the interpreter as a single composite statement. Use a block if you want your **if** or **else** section to perform multiple actions.

### The Switch statement

Like the if-else statement, the switch statement is used to choose the action to perform based on the value of an expression. The main difference is that unlike the if-else statement, the switch statement is not limited to 2 alternatives. Its syntax is as follows:

```
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

Breaks are not required but if a **break** is missing, execution will continue at the next **case** section. The **default** section is also optional but at least one **case** section (or *default* section) must be present in the switch block.

Example:

```JS
result = (int)readln("What's your result? ");

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
       println('Average!');
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

So far, we've seen the **switch** statement and the possibilities it offers. But its syntax is somewhat cumbersome: we have to repeat the **case** keyword a lot of times. We also have to use **break** to prevent the flow of execution from continuing to the next **case** section. This also leaves us with a very poor choice about what kind of operation to use to compare the value of our expression with the value of each **case** label. This is the kind of problem that pattern matching solves. It combines a better matching syntax with the **switch** keyword, which this time is used as an operator, helping us create expressions that not only choose what action to perform based on the value of an expression, but also return a value. A pattern matching expression typically looks like this:

```
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
4. A result can be produced in a block, in which case the block must end with a return statement as in a function body (blocks in pattern matching expressions are actually anonymous functions that are declared and used in place).
5. Throwing an exception is also a valid result.

Example 1:

```JS
n = (int)readln('Please type a number: ');

res = n switch {
    ..-1 => {
        println('this is a block');
        return 'negative';
    },
    0..9 => 'from 0 to 9',
    10, 11, 12 => '10, 11, or 12',
    13, 14..17, 18 => '13 or between 14 and 17 or 18',
    x: 20 <= x && x < 30 => '20 to 29',
    30.. => '30 and above',
    _ => throw 'an exception for 19'
};

println(res);
```

Example 2:

```JS
o = new {name = 'my object', size = 18, color = 'blue'};

res = o switch {
    null => 'absolutely null',
    float => "i'm floating",
    Exception {message = 'something went wrong'} => __value.message,
    {name = 'my object', size = 18} => 'my object of size 18'
};

println($'o is {o}');
println($'the result with o is {res}');
```

The above examples showcase the different kinds of patterns. They are explained in the table below:

|Pattern|Syntax|Description|
|-|-|-|
|always pattern|an underscore ( _ )|Matches everything; usually acts as the default. Should normally appear at the end of the list to avoid short-circuiting the flow of comparisons|
|null pattern|the keyword **null**|Matches only expressions that evaluate to the null reference|
|single value pattern|a literal value of type **bool**, **int**, **long**, **float**, **decimal**, **date**, or **string**|Matches only expressions whose value is equal to its own|
|range pattern|two literal values ​​of type **bool**, **int**, **long**, **float**, **decimal**, **date**, or **string** separated by two dots ( .. ).<br>One of the limits can be omitted.<br>**e.g.**: `0..9` or `..-1` or `30..`|Matches values ​​that are in the given range; if the lower bound is omitted, the pattern matches anything less than or equal to the upper bound; when the upper bound is omitted, the pattern matches anything greater than or equal to the lower bound |
|type pattern|any type name, including user-defined classes|Matches objects of the given type; also matches instances of subclasses if the given type is a class |
|object pattern|a curly-braced list of property initializers optionally preceded by a type name; **e.g.**: `Exception {message = 'something went wrong'}`|Matches an object of the given type (if a type name is given) that has the same values ​​for the listed properties |
|predicate pattern|an identifier followed by a colon ( : ) and then a logical expression; **e.g.**: `x: 20 <= x && x < 30`|This type of pattern is a function with a single parameter; the initial identifier is the name you want to give to the parameter; it will contain the value of the expression that is compared to the pattern; the expression that follows the colon is the body of the function; The pattern matches expressions for which its function returns **true**|
|composite pattern|multiple elementary patterns separated by commas ( , ); **e.g.**:`13, 14..17, 18`|Tries to match the expression with one of its components|

**Note**: On the right side of the arrow, the value of the expression that is compared can be referenced as **__value**. **e.g.**: in `=> __value.message` we are returning the message of the matched Exception if any.

### Loops

Loops are used to repeat an action until a condition is met.
In AddyScript, we have 4 different kinds of loops. These are:

#### The While loop:

It performs an action as long as a condition is satisfied. If the condition is initially unsatisfied, the action will never be performed.

Syntax:

`while (condition) action`

Example:

```JS
i = 1;
while (i <= 12)
{
    println('2 x {0} = {1}', i, 2*i);
    ++i;
}
```

#### The Do-While loop:

Just like the While loop, it performs an action as long as a condition is satisfied. The action is performed before the condition is checked thus leading to the action always being performed at least once.

Syntax:

`do action while (condition);`

Example:

```JS
i = 1;

do
{
   println('2 x {0} = {1}', i, 2*i);
   ++i;
} while (i <= 12);
```

#### The For loop:

It's mainly used to perform an action for each value of a counter.

Syntax:

`for (counter_initialization; counter_limit_test; counter_increment) action`

Example:

```JS
for (i = 1; i <= 12; ++i)
   println('2 x {0} = {1}', i, 2*i);
```

**Notes**:

1. In the above example, _counter_initialization_ and _counter_increment_ are single statements, but they could also be comma-separated lists of statements; _counter_limit_test_ on the other hand is always a single logical expression.

2. The example given is equivalent to the while loop example.

#### The For-Each loop:

It's used to iterate over a collection of items, performing an action on each of them.

It has the following forms:

Form 1:

`foreach (item in collection) action`

Example:

```JS
words = ['john', 'paul', 'second', 'the', 'pope'];

foreach (word in words)
   print(word + ' ');

println();
```

Form 2:

`foreach (key => value in collection) action`

Example:

```JS
jobs = {'paul' => 'general manager', 'roland' => 'accountant', 'david' => 'driver'};

foreach (name => job in jobs)
   println(name + ' is a ' + job);
```

**Notes**:

* Even in the first form, there is an implicit key named **__key**. In both forms, the _collection_ must be a **string**, a **blob**, a **tuple**, a **list**, a **map**, a **set**, a native object implementing the **IEnumerable** interface or an AddyScript object implementing the **iterator protocol**.

* It is possible to use the foreach loop to iterate over instances of user-defined classes if their class implements the _interator protocol_. We'll come back on this in the section dedicated to [inheritance and polymorphism](inheritance.md).

### Jumps

Jumps allow execution to resume from a location other than the next instruction. They are generally very useful for prematurely exiting from functions or loops. The table below describes them:

|Jump statement|Syntax|Effect|
|-|-|-|
|continue|`continue;`|Skips to the next iteration of a loop before the end of the current iteration|
|break|`break;`|Exits a loop before the last iteration is reached.<br>Exits a switch block before the last statement is reached.|
|goto|`goto label;`<br>or _(in a switch block)_<br>`goto case X;`<br>`goto default;`|Unconditionally jumps to the specified location.<br>In a switch block can also jump to one of the case labels.<br><sub>**Note**: A label is any identifier that's appears at the beginning of an instruction and is followed by a colon. **e.g.**: in `printout: println('hello');` _printout_ is the label.</sub>|
|return|`return;`<br>or<br>`return expression;`|Returns from a function with or without a value.|
|throw|`throw exception;`<br>or<br>`throw "some error message";`|Raises an error and stops the execution of the script until the raised error is caught by a **try-catch-finally** statement.|

[Home](README.md) | [Previous](expressions.md) | [Next](spec-types.md)
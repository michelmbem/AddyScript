# Anatomy of a script

A script in AddyScript is a sequence of statements in any order. Even an empty file is a valid script. Statements come in many different forms: **import** directives, class or function definitions, tests, loops, blocks, assignments, **try-catch-finally** structures, etc.... There is no particular order in which you should organize your statements. For example, you might assign a value to a variable, then declare a class, and then call a function. Some statements are elementary, while others may contain child statements. We will learn about the different types of statements and the proper syntax for each as we progress through this manual. Here are some sample scripts to get you started:

### Simply printing _"Hello World!"_ to the standard output:

```JS
println('Hello World!');
```

### Reading _n_ from the standard input and computing the sum and average of n numbers:

```JS
n = (int)readln('How many numbers? ');
sum = 0;

for (i = 0; i < n; ++i)
{
   print($'Item number {i + 1}: ');
   sum += (float)readln();
   // Could also be: sum += (float)readln($'Item number {i + 1}: ');
}

println($'The sum is {sum}');
println($'The averrage is {sum / n}');
```

### Declaring a function to say _"Hello"_ to each name in a list:

```JS
function hello(name)
{
   println($'Hello {name}');
   if (name === 'roger')
      println('Have you been a football player?');
}

names = ['john', 'mike', 'bill', 'david', 'mark', 'roger'];

// Hello to everyone:
foreach (name in names)
   hello(name);

// Another way to do the same stuff:
names.each(hello);

// Or without declaring the 'hello' function at all:
names.each(function (x)
           {
              println($'Hello {x}');
              if (x !== 'bill') return;
              println('Have you been a CEO somewhere?');
           });
```

Well, now you can try your own.

<div markdown class="web-only">

[Home](README.md) | [Previous](asgui-asis.md) | [Next](expressions.md)

</div>
# Inheritance and polymorphism

## Inheritance

Inheritance is an object-oriented programming concept that allows you to define new classes that extend existing ones, that is, the new class inherits all the members of its ancestor and adds some new members. Instances of the child class are also considered by AddyScript to be instances of the parent class (meaning that if B inherits from A, where b is an instance of B, then the expression `b is A` evaluates to **true**). In the example below, we will illustrate how inheritance is handled in AddyScript by creating a subclass of the Person class introduced in the previous section. The example also shows how to invoke the parent class's constructor (to initialize inherited fields).

```JS
class Employee : Person
{
    public constructor(name, sex, age, hireDate, department, jobTitle)
         : super(name, sex, age)
    {
        this.hireDate = hireDate;
        this.department = department;
        this.jobTitle = jobTitle;
    }
    
    public property hireDate;
    
    public property department;
    
    public property jobTitle;
}

// Usage:
steve = new Employee('Steve', 'Male', 32, `2002-04-18`, 'IT', 'Senior Analyst');
println('{0} works for us since {1} years', steve.summary(), now().subtract(steve.hireDate, 'year'));
println('He is currently a {0} at the {1} department', steve.jobTitle, steve.department);
```

Output:

```
Mr. Steve, Male person aged 32 works for us since 23 years
He is currently a Senior Analyst at the IT department
```


The so defined _Employee_ class inherits all the members of the _Person_ class in addition to those that it declares itself.

## Polymorphism

Polymorphism is nothing more than the ability to override inherited members in derived classes. Since AddyScript uses duck typing (i.e. objects are generally taken for what they appear to be without further checking), it handles polymorphism without any additional semantics. Any class method is overridable unless it is marked as **private**, **final**, or **static** in the parent class. Also, when the parent class is **abstract**, any subclass that is not abstract must override its abstract properties and methods.

Example:

```JS
// A base abstract class with a single abstract method
abstract class Pet
{
    public abstract function cry();
}

// A subclass with a custom cry
class Cat : Pet
{
    public function cry() => println('As a cat, I meow');
}

// Another subclass with a custom cry
class Dog : Pet
{
    public function cry() => println('As a dog, I bark');
}

// Another subclass with a custom cry
class Pig : Pet
{
    public function cry() => println('As a pig, I grunt');
}

// a set of pets
pets = {new Dog(), new Cat(), new Pig()};

// making them all cry
foreach (pet in pets)
    pet.cry();
```

Output:

```
As a dog, I bark
As a cat, I meow
As a pig, I grunt
```

**Note**: the above example would work even if _Cat_, _Dog_ and _Pig_ were not declared as subclasses of _Pet_. That is a consequence of AddyScript's **duck typing** philosophy: an instance of any class that exposes a public instance _cry_ method without any parameters can be used where a Pet is expected.

## The super keyword

Even after you have overridden a method, you still can refer to its original implementation as `super::methodName` (methodName being the name of that method). The same rule applies for properties and indexers.

## Protocols

AddyScript has a number of protocols that, if implemented by a user-defined class allow instances of that class to behave like some of the predefined data types in certain aspects.

### The iterator protocol:

#### First approach: moveFirst, hasNext, moveNext

The iterator protocol makes it possible to iterate over instances of a user-defined class with a foreach loop. For a class to implement this protocol, it must expose 3 methods named **moveFirst**, **hasNext**, and **moveNext** respectively. The role of **moveFirst** is to position the internal cursor on the first logical element of the collection (assuming your class is a custom collection). **hasNext** is supposed to return a boolean indicating whether the iteration can continue or not. Finally, **moveNext** is responsible for moving the internal cursor forward, returning the value pointed to by the cursor at each step.

Example:

```JS
class Range
{
   private start;
   private end;
   private step;
   private current;
   
   public constructor(start, end = 0, step = 1)
   {
      // Swap start and end to be in the correct order
      if (start > end) (start, end) = (end, start);
      // Ensure that step is always positive
      if (step <= 0) throw 'step has to be positive';
      
      this.start = start;
      this.end = end;
      this.step = step;
   }
   
   public function moveFirst() => this.current = this.start;
   
   public function hasNext() => this.current < this.end;
   
   public function moveNext()
   {
      var current = this.current;
      this.current += this.step;
      return current;
   }
}

foreach (item in new Range(5, 25, 5))
   println(item);
```

Output:

```
5
10
15
20
```

#### Second approach: iterator method with yield statements

Alternatively, a class can implement the iterator protocol simply by exposing an **iterator** method whose body contains a succession of **yield** statements (presumably in a loop). The **yield** statement has a syntax similar to that of a **throw** statement or a parameterized **return** statement, but it is not actually a jump. It only tells AddyScript what value the iterator being constructed should return on each iteration.

Example:

```JS
class Range
{
   private start;
   private end;
   private step;
   
   public constructor(start, end = 0, step = 1)
   {
      // Swap start and end to be in the correct order
      if (start > end) (start, end) = (end, start);
      // Ensure that step is always positive
      if (step <= 0) throw 'step has to be positive';
      
      this.start = start;
      this.end = end;
      this.step = step;
   }
   
   public function iterator()
   {
      for (var current = this.start; current < this.end; current += this.step)
         yield current;
   }
}

foreach (item in new Range(5, 25, 5))
   println(item);
```

Output:

```
5
10
15
20
```

<div markdown class="web-only">

[Home](README.md) | [Previous](classes.md) | [Next](introspection.md)

</div>
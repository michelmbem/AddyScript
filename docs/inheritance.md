# Inheritance and polymorphism

### Inheritance

In the example below, we will illustrate how inheritance is handled in AddyScript by creating a subclass of the Person class introduced in the previous section. The example also shows how to invoke the constructor of the parent class (to initialize inherited fields).

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

The so defined _Employee_ class inherits all the members of the _Person_ class in addition to those that it declares itself.

### Polymorphism

Polymorphism is nothing more than the ability to override inherited members in derived classes. Since AddyScript uses duck typing (i.e. objects are generally taken for what they appear to be without further checking), it handles polymorphism without any additional semantics. Any class method is overridable unless it is marked as **private**, **final**, or **static** in the parent class. Also, when the parent class is **abstract**, any subclass that is not abstract must override its abstract methods.

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
    public function cry() => println('meow');
}

// Another subclass with a custom cry
class Dog : Pet
{
    public function cry() => println('woof');
}

// Another subclass with a custom cry
class Pig : Pet
{
    public function cry() => println('gruing');
}

// a set of pets
pets = {new Dog(), new Cat(), new Pig()};

// making them all cry
foreach (pet in pets)
    pet.cry();
```

### The super keyword

Even after you have overridden a method, you still can refer to its original implementation as `super::methodName` (methodName being the name of that method). The same rule applies for properties and indexers.

[Home](README.md) | [Previous](classes.md) | [Next](introspection.md)
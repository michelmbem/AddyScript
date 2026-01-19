# Classes

### Declaring a class

Declaring a class in AddyScript is straightforward. Simply use this syntax:

```
[modifier] class className [: superClassName]
{
    classMembers
}
```

Where

* _modifier_ is one of the keywords **final**, **static** and **abstract**.
* _className_ is the name you want to give your class.
* _superClassName_ is the name of a previously defined class.
* _classMembers_ is a series of field, property, method, operator, and/or event definitions.

**Note**: The parts in brackets are optional, so you can omit them.

### Defining class members

Generally speaking the definition of a class member follows this syntax:

`[scope] [modifier] specification`

Where

* _scope_ is one of the keywords **private**, **protected** and **public**. If no scope is defined, **private** is applied by default.
* _modifier_ has the same meaning as for a class. It must therefore be **final**, **static** or **abstract**. However, the **abstract** modifier is reserved for properties and methods, it cannot be applied to fields or events. A field can be both **static** and **final**, such a field is a class constant whose default value must be specified.
* The _specification_ varies depending on the type of member defined.

#### Specifying a field:

A field specification is simply the name of the field, optionally followed by an equal sign, and its initial value.

**Note**: You do not have to explicitly declare the fields of a class. Objects are dynamic in AddyScript, so you can add fields to an object as you wish. However, declaring fields allows you to control how they are shared between instances of the class. It also allows you to control their visibility: whether they are accessible outside the class or not. It also ensures that these fields are discovered by any code that introspects the class.

#### Specifying a method:

The specification of a method is just a function declaration embedded into the class body.

#### Specifying a property:

A property is a pair of methods used to read and/or write the value of a field.
AddyScript provides a dedicated syntax for defining properties of a class.
The specification of a property follows this syntax:

```
property property_name
{
    [scope] read
    {
       // statements
    }
    [scope] write
    {
       // statements
    }
 }
```

Where

* _property_name_ is the name you want to give to the property.
* The block prefixed by **read** is called the **read accessor** (or simply **reader**). This is actually the body of the method that will be used to read the value of the backing field. It usually ends with a parameterized **return** statement.
* The block prefixed by **write** is called the **write accessor** (or simply **writer**). This is the body of the method that will be used to write (update) the value of the backing field. In this block, the script can refer to a special variable called **__value**. This is the value assigned to the property.
* Each of these accessors can be omitted, but not both at the same time.
* One of the accessors can be defined on a different scope than the property itself: for example, a public property can have a private writer. Such a property can only be updated by the class itself.
* Neither **read**, nor **write**, nor **__value** are AddyScript keywords. They are simply identifiers that have a special meaning in a particular context (let's say they are **contextual keywords**).
* Since both reader and writer are functions, they can also be reduced to an arrow followed by an expression when they consist of just an expression or a parameterized **return** statement.
* When the property is read-only (i.e. it has no writer), and that its reader does nothing more than return a value, the entire property can be declared with the following syntax: `property property_name => returned_value;`. That syntax is equivalent to  `property property_name { read => returned_value; }`.

#### Automatic properties:

Since defining a property consists of declaring a backing field, then defining the property's read and write accessors,
AddyScript can help the programmer save time by doing most of the work automatically.
To this end, the scripting language supports a shorter syntax for the definition of a property:

```
property property_name { [scope] read; [scope] write; }
```

As you can see, with this syntax, accessors are reduced to the contextual keywords **read** and **write** optionally preceded by a scope.
For such a property, the scripting engine automatically generates a backing field as well as the accessors logic.
The content between braces can even be omitted if no accessors have a different scope than the property itself,
resulting in something as short as: `property property_name;` which is equivalent to `property property_name { read; write; }`.

**Remark**: The specification of an automatic property is identical to that of an abstract property except that the scripting engine doesn't generate any logic for an abstract property. It expects concrete subclasses to do so.

#### Indexers:

Indexers are a special type of property that allows instances of user-defined classes to behave like collections.
A class can have only one indexer, and it cannot be static.
An indexer is always declared as a property with an empty pair of square brackets ([ ]) as its name.
It cannot be automatic: you must provide logic for its accessors.
An indexer's accessors take an implicit parameter called **__key** that contains the value of
the expression enclosed in the square brackets (i.e., the index or key) at the time the indexer is invoked.
An indexer's writer also takes the implicit parameter **__value** like any other writer.

#### Specifying an event:

An event specification consists of the **event** keyword followed by the prototype of the event.
The prototype is a name (an identifier) followed by a comma-separated list of parameters in parentheses.
Once an _foo_ event is defined in a class, this automatically adds three methods to the class:

* A method to add handlers to the _foo_ event: `void add_foo(closure handler)`
* A method to remove handlers from the _foo_ event: `void remove_foo(closure handler)`
* A method to trigger the _foo_ event: `void trigger_foo(...)`.     

    _trigger_foo_ is always private and always has the same parameters as the event itself.

#### Specifying an operator overload:

An operator overload specification consists of the **operator** keyword followed by the operator itself and its parameters. Depending on the operator being overloaded, the list may be empty or contain a single parameter. The complete list of operators that can be overloaded is as follows:

* Unary operators: +, -, ++, --, ~
* Binary operators: +, -, \*, /, %, &amp;, |, ^, &lt;&lt;, &gt;&gt;, ==, !=, &lt;, &gt;, &lt;=, &gt;=, **startswith**, **endswith**, **contains**, **matches**

**Remarks**:

1. No modifiers should be specified when overloading an operator. This means that an operator cannot be **abstract**, **static**, or **final**.
2. In the case of increment (++) or decrement (--) operators, the difference between overloading the prefix form and overloading the postfix form is that the postfix form expects an unused parameter while the prefix form expects no parameter.
3. Operators are not really a special type of class member in AddyScript. Each operator is actually a method with a special name. This can be verified by introspecting a class in which an operator is overloaded. AddyScript looks for such a method whenever it encounters a unary or binary operation involving an instance of a user-defined class and an overloadable operator.

### Example of a class

In this example, we will define a Person class with 3 fields, 4 properties (3 of them mapping the 3 fields plus an automatic one), a method and an event.

```JS
class Person
{
    // Fields
    private _name;
    private _sex = 'Male';
    private _courtesy = 'Mr.';
    
    // A property with the 'compact' syntax
    public property name
    {
        read => this._name;
        write => this._name = __value;
    }
    
    // A property with the typical syntax
    public property sex
    {
        read { return this._sex; }
        write
        {
            // Updating sex will also update courtesy and raise the sex_changed event
            var oldSex = this._sex;
            this._sex = __value;
            this._courtesy = __value.toLower() switch {
                'male' => 'Mr.',
                'female' => 'Mrs.',
                _ => 'Dear'
            };
            this.trigger_sex_changed(oldSex, __value);
        }
    }
    
    // A read-only property encapsulating the _courtesy field
    public property courtesy => this._courtesy;
    
    // A fully automatic property
    public property age;
    
    // An event
    public event sex_changed(oldSex, newSex);
    
    // A method
    public function summary()
    {
        return $'{this.courtesy} {this.name}, {this.sex} person aged {this.age}';
    }
}

// Usage:
john = new Person();
john.name = 'John';
john.age = 42;
println(john.summary());

john.add_sex_changed(|old, _new| => println($'Sex changed from {old} to {_new}'));
john.add_sex_changed(|o, n| => println('Maybe the name should change too'));
john.sex = 'Female';
println(john.summary());
```

### The **this** keyword

In the body of a method, the **this** keyword can be used to refer to the current instance of the class (the one on which the method is invoked).
Most of the time, you will use this feature to access other members of a class from the body of one of its methods.

### Constructors

A **constructor** is a special method that is automatically invoked by the scripting engine when an instance of a class is created to initialize that instance.
In AddyScript, a class has only one constructor since the language does not support method overloading.
The definition of a constructor follows this special syntax:

```
 [scope] constructor (list_of_parameters) [: super(list_of_arguments)]
 {
    statements;
 }
```

Where

* _scope_ is one of the **private**, **protected** and **public** keywords. If no scope is provided, **private** is assumed by default.

    _scope_ has the following effects on instance creation:

    * **private**: Only the class will be able to create instances of itself.
    * **protected**: Only the class and its derived classes will be able to create instances of itself.
    * **public**: Instances of the class can be created anywhere in the code.

* The optional 'colon-super' part is used to invoke the constructor of the parent class (if any) prior to any other statement.
* A constructor is not allowed to return a value.

Example: let's add a constructor in the Person class

```JS
class Person
{
    // Fields: we don't need default values anymore as the constructor is supplying them
    private _name;
    private _sex;
    private _courtesy;
    
    // Here is the constructor, all parameters are made optional to allow the user to omit them
    public constructor(name = "", sex = "Male", age = 0)
    {
       this.name = name;
       this.sex = sex;
       this.age = age;
    }
    
    // A property with the 'compact' syntax
    public property name
    {
        read => this._name;
        write => this._name = __value;
    }
    
    // A property with the typical syntax
    public property sex
    {
        read { return this._sex; }
        write
        {
            // Updating sex will also update courtesy and raise the sex_changed event
            var oldSex = this._sex;
            this._sex = __value;
            this._courtesy = __value.toLower() switch {
                'male' => 'Mr.',
                'female' => 'Mrs.',
                _ => 'Dear'
            };
            this.trigger_sex_changed(oldSex, __value);
        }
    }
    
    // A read-only property encapsulating the _courtesy field
    public property courtesy => this._courtesy;
    
    // A fully automatic property
    public property age;
    
    // An event
    public event sex_changed(oldSex, newSex);
    
    // A method
    public function summary()
    {
        return $'{this.courtesy} {this.name}, {this.sex} person aged {this.age}';
    }
}

// Usage:
jane = new Person("Jane", "Female", 30);
println(jane.summary());
jane.add_sex_changed(|old, _new| => println("Sex changed from {0} to {1}", old, _new));
jane.add_sex_changed(|o, n| => println("Why not call him John?"));
jane.sex = "Male";
println(jane.summary());
```

#### Constructors and property initializers

A constructor call can be followed by a set of property initializers.
This allows you to quickly initialize fields or properties that are not initialized by the constructor.
It can also be used to add undeclared fields to a specific instance of a class.
When using property initializers, if the constructor has no parameters, there is no need to add parentheses to it.

Example:

```JS
class Point
{
    public property x;
    
    public property y;
    
    public function toString(format = 'g')
    {
        return '(' + this.x + ', ' + this.y + ')';
    }
    
}

pt = new Point {x = 10, y = -5};
println(pt.toString());
```

### Modifiers

When working with classes in AddyScript, you will always come across keywords like **private**, **protected**, **public**, **static**, **abstract**, and **final**. These are modifiers (in the broad sense of the word). The first three (**private**, **protected**, and **public**) are used to control the scope of a class member. The other three are used to manage the behavior of a class or one of its members. The table below details the meaning of each modifier depending on where it is used.

|Modifier|Effect on a class|Effect on a class member|
|:-:|-|-|
|**private**|Not applicable|Makes the member inaccessible out of its declaring class|
|**protected**|Not applicable|Makes the member accessible only to its declaring class and subclasses of its declaring class|
|**public**|Not applicable|Makes a member accessible to any part of the script|
|**static**|Forces all members of the class to be static.<br>Disallows creation of instances of the class.|Indicates that the member only accesses other static members of the class.<br>Such a member can be referenced without having to create an instance of the class.<br>Can be combined with **final** on fields.<br>Not applicable to constructors and indexers.|
|**final**|Forbids that another class inherits from this one|Indicates that the property or method cannot be overridden in a subclass.<br>Can be combined with **static** on fields.<br>Not applicable to constructors and events.|
|**abstract**|Allows the declaration of abstract properties and methods within the class.<br>Disallow the creation of instances of the class|Indicates that the property or method is purely virtual, thus having no default implementation.<br>Not applicable to constructors, fields and events.|

### Common members

There is a set of members that any class in AddyScript (including the void type) exposes. The table below presents those members and describes them.

|Member|Nature|Description|
|-|-|-|
|`ClassInfo type { read; }`|final property|Gets the description of the class to which the target object belongs.|
|`bool equals(any other)`|method|Compare two objects and returns true if they're equal. Returns false otherwise.|
|`int hashCode()`|method|Gets the hash code of an object.|
|`int compareTo(any other)`|method|Compares the target object to the given one. Returns -1, 0, 1 depending on whether the target object is less than, equal or greater than the given value.|
|`string toString(string format = 'g')`|method|Gets the (eventually formatted) textual representation of an object.|
|`any clone()`|method|Gets a deep copy of an object. For most types, this simply returns the target object itself. _clone_ is specially useful with collections and objects (native or not). For a native object, clone tests if the object's type implements the _ICloneable_ interface and if so, invokes its _Clone_ method.|
|`void dispose()`|method|Tries to release the unmanaged resources held by the target object. Does nothing for most types. For a collection or an object, recursively calls itself on each item or field. For a native object, tests if the object's type implements the _IDisposable_ interface and if so, invokes its _Dispose_ method.|

Apart from the _type_ property, any of these members can be overridden in your custom classes. AddyScript internally uses them to compare data items and to identify them in collections.

<div class="web-only" markdown="1">

[Home](README.md) | [Previous](userfunc.md) | [Next](inheritance.md)

</div>
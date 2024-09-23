# Collections and objects

### Collections

Collections are ways to store multiple values ​​in a single variable. AddyScript offers 5 different types of collections: lists, maps, sets, queues, and stacks. Each of these types has a unique set of features and is suited for a particular usage scenario.

### Lists

A list is a collection in which items are accessed by index. It is a kind of dynamically sized array. You typically create a list using a list initializer. After creating a list, you can add new items to it using the "add" or "insert" methods. You can remove existing items from it using the "remove" and "removeAt" methods. A list is searched using the **contains** operator or the "indexOf" and "lastIndexOf" methods. The contents of the list can be sorted using the "sort" method. The "inverse" method returns a list with the same contents but with the items ordered in reverse order. To get the number of items currently stored in the list, simply read the "count" property and finally, to empty the list, call the "clear" method. Here is an example of using a list in AddyScript.

Example:

```Cpp
n = (int) readln('How many names? ');
names = [];

for (i = 0; i < n; i++)
{
    print('Name number {0}: ', i + 1);
    names.add(readln());
}

println('You entered the following names: ' + names);

names = names.sort();
println('After sorting: ' + names);

someName = readln('Type a name: ');

if (names contains someName)
{
    i = names.indexOf(someName);
    println('{0} was found in the list at position {1}', someName, i);
    names.removeAt(i);
    println('After removal of ' + someName + ': ' + names);
}
else
{
    i = (int) readln('Enter a position in the list: ');
    
    if (i < names.count)
    {
        names.insert(i, someName);
        println('After insertion of {0} at position {1}: {2}', someName, i, names);
    }
    else
        println('Out of list boundaries!');
}

someName = readln('Type another name: ');

if (names.remove(someName))
    println('After removal of ' + someName + ': ' + names);
else
    println('Not found!');

names.clear();
println('After clearing the list: ' + names);
```

#### List API

The following tables summarize all the operators, properties and methods provided by the AddyScript list API.

The list class supports the following operators:

|Operator|Operands|Description|
|:-:|-|-|
|[index]|an integer|Gets or sets the value of an item in the list. Negative indices are processed modulo the length of the list.|
|[lbound..ubound]|2 integers|Gets a slice (i.e. a sub-list) of the target list. Either "lbound" or "ubound" can be omitted. When "lbound" is omitted it's replaced with 0, a missing "ubound" will be replaced with the length of the list.|
|\+|2 lists|Concatenates two lists.|
|\*|a list to the left and an integer to the right|Concatenates a list with itself the given number of times.|
|**contains**|a list to the left and anything to the right|Checks if the list contains the given value.|
|==|2 lists|Checks that both lists have the same length and contain equal items at each position.|
|!=|2 lists|Checks that both lists have different lengths or contain different items at least at one position.|

In addition to the above operators, the **list** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int count { read; }`|property|Gets the number of items currently stored in the list.|
|`void add(any value)`|method|Appends an item to the list.|
|`void insert(int position, any value)`|method|Inserts an item at the given position into the list. Negative values of the optional start parameter are processed modulo the length of the list.|
|`void insertAll(int position, list values)`|method|Inserts the items of the given collection at the given position into the list. Negative values of the optional start parameter are processed modulo the length of the list.|
|`int indexOf(any value, int start = 0, int count = 0)`|method|Gets the position of the first occurrence of an item in the given range of a list. Negative values of the optional start parameter are processed modulo the length of the list. Parameter count is ignored if it's negative or zero (that's the default). Returns -1 if the item is not found.|
|`int lastIndexOf(any value, int start = -1, int count = 0)`|method|Gets the position of the last occurrence of an item in the given range of a list. Negative values of the optional start parameter are processed modulo the length of the list. Parameter count is ignored if it's negative or zero (that's the default). Returns -1 if the item is not found.|
|`int bsearch(any value)`|method|Operates a binary search of the given value on a sorted list.|
|`bool remove(any value)`|method|Tries to remove an item from a list. Returns true on success, false otherwise.|
|`void removeAt(int position, int count = 1)`|method|Removes a range of items from the calling list. If count is negative or zero, it's automatically replaced by 1. An exception is thrown if either position or position + count is beyond the boundaries of the list.|
|`void clear()`|method|Empties a list.|
|`int frequencyOf(any value, int start = 0, int count = 0)`|method|Gets the number of occurrences of the a value in the given range of a list. Negative values of the optional start parameter are processed modulo the length of the list. Parameter count is ignored if it's negative or zero (that's the default)|
|`list sort(closure comparator = null)`|method|Returns a sorted clone of the target list using the given closure to compare items. If no parameter (or null) is passed, sorts the list following the logic of the "compareTo" method of each item.|
|`list inverse()`|method|Returns a list with the same content than the target list by with items sorted in reverse order.|
|`list sublist(int start, int count)`|method|Extracts a subset of the list delimited by the given boundaries.|
|`list unique()`|method|Gets a clone of the calling list in which each item is unique.|
|`map mapTo(list other)`|method|Creates and returns a map using the items of the calling list as keys and those of the other list as values. Both lists must have the same length.|
|`string join(string separator = ' ')`|method|Creates a string by concatenating the list items. An optional separator can be provided; by default, the whitespace is used as a separator.|

### Maps

A map is a collection of key-value pairs. The key is used as an index to add, retrieve, and update values ​​in the map. So, a map can be thought of as a list where the indexes are neither necessarily integers nor necessarily contiguous. Similar to lists, you typically create a map using a map initializer. After that, you can get the number of key-value pairs stored in the map by reading the "count" property. The "containsKey" and "containsValue" methods are used to check the existence of a particular pair in the map. To remove a pair, simply call the "remove" method. The "keys" and "values" properties are used to retrieve all the keys and all the values ​in a map, respectively. Note that both methods return sets. So, if a value appears twice in a map, the collection returned by "getValues" will contain only one copy of it. To get all the keys related to a particular value in a map, call its "keysOf" method with that value as a parameter. The "frequencyOf" method on the other hand simply tells how many distinct keys a value is related to. So `someMap.frequencyOf(someValue)` is equivalent to `someMap.keysOf(someValue).count`. The "inverse" method is used to create a map in which the key-value pairs are inverse to those in the calling map. Finally, to make a map empty, simply call its "clear" method. Here is an example of using the map in AddyScript.

Example:

```Cpp
tom = {'name' => 'Tom Berenger', 'job' => 'Lawyer', 'age' => 38};
tom['company'] = 'Holy Lawyers & co.';
tom['hire date'] = `2004-05-18`;
tom['salary'] = 3600D;

foreach (prop => value in tom)
    println('{0} : {1}', prop, value);

someProperty = readln('Type the name of a property: ');

if (tom.containsKey(someProperty))
    println('Property ' + someProperty + ' is already defined for tom!');
else
{
    tom[someProperty] = readln('Enter a value for ' + someProperty + ': ');
    println('After adding that property:');
    foreach (prop in tom.keys)
        println('{0} : {1}', prop, tom[prop]);
}

someProperty = readln('Type the name of another property: ');

if (tom.remove(someProperty))
{
    println('After removal of ' + someProperty + ':');
    foreach (value in tom)
        println('{0} : {1}', __key, value);
}
else
    println('Property ' + someProperty + ' is not defined for tom!');

someValue = readln('Type any value: ');

if (tom.containsValue(someValue))
{
    println(someValue + ' is the value of ' + tom.frequencyOf(someValue) + ' properties of tom');
    print('Those are :');
    foreach (prop in tom.keysOf(someValue))
        print(prop);
    println();
}
else
    println('No property of tom has value ' + someValue);
```

#### Map API

The following tables summarize all the operators, properties and methods provided by the AddyScript map API.

The map class supports the following operators:

|Operator|Operands|Description|
|:-:|-|-|
|[key]|a value of any type|Gets or sets the value attached to the given key in the map.|
|\+|2 maps|Merges both map in a single one. It fails if both maps have a key in common.|
|==|2 maps|Checks that both maps contain equal key-value pairs.|
|!=|2 lists|Checks that one of the maps has at least one key-value pairs that the other map does not have.|

In addition to the above operators, the **map** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int count { read; }`|property|Gets the number of key-value pairs currently stored in the map.|
|`set keys { read; }`|property|Gets a set of all the keys of a map.|
|`set values { read; }`|property|Gets a set of all the distinct values of a map.|
|`bool containsKey(any key)`|method|Checks if the map contains the given key.|
|`bool containsValue(any value)`|method|Checks if the map contains the given value.|
|`set keysOf(any value)`|method|Gets a set of all the keys of a map that are bound to a particular value.|
|`int frequencyOf(any value)`|method|Gets the number of all the keys of a map that are bound to a particular value.<br>This is also the number of occurrences of a value in the map.|
|`bool remove(any key)`|method|Tries to remove a key-value pair from a map (the one that has the given key if any). Returns true on success, false otherwise.|
|`void clear()`|method|Empties a map.|
|`map inverse()`|method|Creates and returns a map in which key-value pairs are inverse of those of the calling one.|

### Sets

A set is an emulation of the mathematical concept of a set. It is a kind of map in which we are only interested in the keys. Like lists and maps, you typically create a set using a set initializer. After that, you can add elements to it using the "add" method or get the number of elements stored by reading the "count" property. The "contains" operator can be used to check the existence of a particular element in a set. To remove an element from a set, simply call the "remove" method. Finally, to empty a set, simply call its "clear" method. Here is an example of using a set in AddyScript.

Example:

```Cpp
t = {'john', 'mike', 'bob'};
u = {'steve', 'mike', 'john'};

println('t = ' + t);
println('u = ' + u);
println('t + u = ' + (t + u));
println('t - u = ' + (t - u));
println('t & u = ' + (t & u));
println('t | u = ' + (t | u));
println('t ^ u = ' + (t ^ u));
println('(t + u) === (t | u) : ' + ((t + u) === (t | u)));
println();

v = {};
v.add('steve');

println('v = ' + v);
println('v < u : ' + (v < u));
println('v <= u : ' + (v <= u));
println('u < u : ' + (u < u));
println('t > v : ' + (t > v));
```

#### Set API

The following tables summarize all the operators, properties and methods provided by the AddyScript set API.

The set class supports the following operators:

|Operator|Operands|Description|
|:-:|-|-|
|\+|2 sets|Computes and returns the union of both sets. Identical to operator \|.|
|\-|2 sets|Computes and returns the difference of both sets.|
|\||2 sets|Computes and returns the union of both sets. Identical to operator \+.|
|&|2 sets|Computes and returns the intersection of both sets.|
|^|2 sets|Computes and returns the symmetric difference of both sets (i.e.: a and b being sets, a ^ b is equal to (a - b) + (b - a)).|
|==|2 sets|Verifies that both sets contains exactly the same elements, that is their symmetric difference is empty.|
|!=|2 sets|Verifies that there is at least one element in one set that does not belong to the other.|
|\<|2 sets|Verifies that left operand is a proper subset of the right operand.|
|\<=|2 sets|Verifies that left operand is a subset of the right operand.|
|\>|2 sets|Verifies that left operand is a proper superset of the right operand.|
|\>=|2 sets|Verifies that left operand is a superset of the right operand.|
|**contains**|a set to the left and anything to the right|Checks if the set contains the given value.|

In addition to the above operators, the **set** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int count { read; }`|property|Gets the number of elements currently stored in the set.|
|`void add(any value)`|method|Adds a value to the set. If the same value if already contained in the set, an exception will be thrown.|
|`bool remove(any value)`|method|Tries to remove a value pair from a set. Returns true on success, false otherwise.|
|`void clear()`|method|Empties a set.|

### Queues

A queue is a collection that implements the first-in, first-out design pattern. You will typically get a queue from a call to **queue**::of. After that, you can add elements to it using the "enqueue" method or get the number of elements stored in it by reading the "count" property. To extract an element from a queue, use the "dequeue" method. The "peek" method does the same thing, except that it does not remove the element from the queue. Finally, to empty a queue, simply call its "clear" method.

#### Queue API

The following table summarizes all the properties and methods provided by the AddyScript queue API.

|Member|Nature|Description|
|-|-|-|
|`int count { read; }`|property|Gets the number of elements currently stored in the queue.|
|`void enqueue(any value)`|method|Adds a value to the queue.|
|`any dequeue()`|method|Extracts and returns the oldest value in the queue.|
|`any peek()`|method|Gets the oldest value in the queue without removing it.|
|`void clear()`|method|Empties a queue.|

### Stacks

A stack is a collection that implements the last-in, first-out design pattern. You will typically get a stack as a result of a call to **stack**::of. After that, you can add elements to it using the "push" method or get the number of elements stored in it by reading the "count" property. To pop an element off the stack, use the "pop" method. The "peek" method does the same thing, but it does not remove the element from the stack. Finally, to empty a stack, simply call its "clear" method.

#### Stack API

The following table summarizes all the properties and methods provided by the AddyScript stack API.

|Member|Nature|Description|
|-|-|-|
|`int count { read; }`|property|Gets the number of elements currently stored in the stack.|
|`void push(any value)`|method|Adds a value to the stack.|
|`any pop()`|method|Pops a value from the stack.|
|`any peek()`|method|Peeks the value that's on top the stack without removing it.|
|`void clear()`|method|Empties a stack.|

### Objects

Just like a collection, an object is another way to store multiple values ​​in a single variable. These values ​​are then called the fields of the object. The fields of the object are accessed by their name, using the dot syntax: objectName.fieldName.

#### Creating an object

There are 2 ways to create objects in AddyScript: by using an initializer or by invoking a constructor. We will talk about constructors in the chapter dedicated to object-oriented programming. For now, let's just talk about object initializers.

To create an object using an initializer, simply use this syntax:

`varname = object_initializer;`

An object initializer is a comma-separated list of field initializers enclosed in curly braces and preceded by the keyword **new**. A field initializer consists of an identifier (the field name) followed by an equal sign (=) and then an expression (the initial value of the field).

Example:

```Cpp
student = new {firstName = 'John', lastName = 'Alberti', age = 12};
println('{0} {1} is aged {2}', student.firstName, student.lastName, student.age);
```

Sometimes it is better to initialize the object with an empty initializer and then add fields to it as needed.

Example:

```Cpp
student = new {};
student.firstName = 'André';
student.lastName = 'Dikos';
student.age = 23;

println('{0} {1} is aged {2}', student.firstName, student.lastName, student.age);
```

Finally, it is worth mentioning that AddyScript has the ability to convert a map into an object and vice versa. In this case, if one of the keys in the map is not a valid identifier, the resulting field (in the outgoing object) will be accessible using a special identifier of the form $1, $if or for\x20each (obtained from 1, if and for each respectively).

Example:

```Cpp
hash = {'long' => 120, 'large' => 80, 'depth' => 20};
shape = (object) hash;
println('Shape size: {0} x {1} x {2}', shape.$long, shape.large, shape.depth);
```

#### Manipulating objects

Well, there is no special manipulation on objects, other than storing values ​​in their fields and retrieving them later. Advanced object manipulation is a concern of object-oriented programming.

[Home](README.md) | [Previous](spec-types.md) | [Next](innerfunc.md)
# Collections and objects

### Collections

Collections are ways to store multiple values ​​in a single variable. AddyScript offers 6 different types of collections: tuples, lists, sets, queues, stacks, and , maps. Each of these types has a unique set of features and is suited for a particular usage scenario.

### Tuples

A tuple is a collection in which items are accessed by index. You typically create a tuple using a tuple initializer. Once created, the contents of the tuple do not change, meaning that tuples are immutable. Items cannot be added, updated, or deleted, and the size of the tuple does not change during its lifetime. You can search for an item in a tuple using the **contains** operator or the "indexOf" and "lastIndexOf" methods. You can read the "size" property to get the number of items stored in the tuple. Here is an example of using a tuple in AddyScript.

Example:

```JS
t = ('John Doe', 19, 'New York');

println($'t.size = {t.size}');
println('contents:');

for (var i = 0; i < t.size; ++i)
    println($'t[{i}] = {t[i]}');

println($'19 is at index {t.indexOf(19)}');
println($'Does t contain "New York"? {t contains "New York"}');
println($'Does t contain "Tokyo"? {t contains "Tokyo"}');
```

#### Tuple API

The following tables summarize all the operators, properties and methods provided by the AddyScript tuple API.

The **tuple** class supports the following operators:

|Operator|Operands|Description|
|:-:|-|-|
|[index]|an integer|Gets the value of an item in the tuple. Negative indices are processed modulo the length of the tuple.|
|[lbound..ubound]|2 integers|Gets a slice (i.e. a sub-tuple) of the target tuple. Either "lbound" or "ubound" can be omitted. When "lbound" is omitted it's replaced with 0, a missing "ubound" will be replaced with the length of the tuple.|
|\+|2 tuples|Concatenates two tuples.|
|\*|A tuple on one side and an integer on the other side|Concatenates a tuple with itself the given number of times.|
|**contains**|a tuple to the left and anything to the right|Checks if the tuple contains the given value.|
|==|2 tuples|Checks that both tuples have the same length and contain equal items at each position.|
|!=|2 tuples|Checks that both tuples have different lengths or contain different items at least at one position.|

In addition to the above operators, the **tuple** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int size { read; }`|property|Gets the number of items currently stored in the tuple.|
|`any front { read; }`|property|Gets the first item of a tuple.|
|`any back { read; }`|property|Gets the last item of a tuple.|
|`int indexOf(any value, int start = 0, int count = 0)`|method|Gets the position of the first occurrence of an item in the given range of a tuple. Negative values of the optional start parameter are processed modulo the length of the tuple. Parameter count is ignored if it's negative or zero (that's the default). Returns -1 if the item is not found.|
|`int lastIndexOf(any value, int start = -1, int count = 0)`|method|Gets the position of the last occurrence of an item in the given range of a tuple. Negative values of the optional start parameter are processed modulo the length of the tuple. Parameter count is ignored if it's negative or zero (that's the default). Returns -1 if the item is not found.|

### Lists

Like a tuple, a list is a collection in which items are accessed by index. But on contrary of tuples, lists are not immutable. A list is a kind of dynamically sized array. You typically create a list using a list initializer. After creating a list, you can add new items to it using the "add" or "insert" methods. You can remove existing items from it using the "remove" and "removeAt" methods. You can search for an item in a list using the **contains** operator or the "indexOf" and "lastIndexOf" methods. The contents of the list can be sorted using the "sort" method. The "inverse" method returns a list with the same contents but with the items ordered in reverse order. To get the number of items currently stored in the list, simply read the "size" property and finally, to empty the list, call the "clear" method. Here is an example of using a list in AddyScript.

Example:

```JS
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
    
    if (i < names.size)
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
|\*|A list on one side and an integer on the other side|Concatenates a list with itself the given number of times.|
|**contains**|a list to the left and anything to the right|Checks if the list contains the given value.|
|==|2 lists|Checks that both lists have the same length and contain equal items at each position.|
|!=|2 lists|Checks that both lists have different lengths or contain different items at least at one position.|

In addition to the above operators, the **list** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int size { read; }`|property|Gets the number of items currently stored in the list.|
|`bool empty { read; }`|property|Returns **true** if the list has no item, returns **false** otherwise.|
|`any front { read; }`|property|Gets the first item of a list. Returns **null** if the list is empty.|
|`any back { read; }`|property|Gets the last item of a list. Returns **null** if the list is empty.|
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

### Sets

A set is an emulation of the mathematical concept of a set. It is a kind of map in which we are only interested in the keys. Like lists and maps, you typically create a set using a set initializer. After that, you can add elements to it using the "add" method or get the number of elements stored by reading the "size" property. The "contains" operator can be used to check the existence of a particular element in a set. To remove an element from a set, simply call the "remove" method. Finally, to empty a set, simply call its "clear" method. Here is an example of using a set in AddyScript.

Example:

```JS
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
|`int size { read; }`|property|Gets the number of elements currently stored in the set.|
|`bool empty { read; }`|property|Returns **true** if the set has no item, returns **false** otherwise.|
|`void add(any value)`|method|Adds a value to the set. If the same value if already contained in the set, an exception will be thrown.|
|`bool remove(any value)`|method|Tries to remove a value pair from a set. Returns true on success, false otherwise.|
|`void clear()`|method|Empties a set.|

### Queues

A queue is a collection that implements the first-in, first-out design pattern. You will typically get a queue from a call to **queue**::of. After that, you can add elements to it using the "enqueue" method or get the number of elements stored in it by reading the "size" property. To extract an element from a queue, use the "dequeue" method. The "peek" method does the same thing, except that it does not remove the element from the queue. Finally, to empty a queue, simply call its "clear" method.

#### Queue API

The following table summarizes all the properties and methods provided by the AddyScript queue API.

|Member|Nature|Description|
|-|-|-|
|`int size { read; }`|property|Gets the number of elements currently stored in the queue.|
|`bool empty { read; }`|property|Returns **true** if the queue has no item, returns **false** otherwise.|
|`any front { read; }`|property|Gets the oldest value of a queue without removing it. Throws an exception if the queue is empty.|
|`void enqueue(any value)`|method|Adds a value to the queue.|
|`any dequeue()`|method|Extracts and returns the oldest value in the queue.|
|`void clear()`|method|Empties a queue.|

### Stacks

A stack is a collection that implements the last-in, first-out design pattern. You will typically get a stack as a result of a call to **stack**::of. After that, you can add elements to it using the "push" method or get the number of elements stored in it by reading the "size" property. To pop an element off the stack, use the "pop" method. The "peek" method does the same thing, but it does not remove the element from the stack. Finally, to empty a stack, simply call its "clear" method.

#### Stack API

The following table summarizes all the properties and methods provided by the AddyScript stack API.

|Member|Nature|Description|
|-|-|-|
|`int size { read; }`|property|Gets the number of elements currently stored in the stack.|
|`bool empty { read; }`|property|Returns **true** if the stack has no item, returns **false** otherwise.|
|`any top { read; }`|property|Gets the value on top of a stack without removing it. Throws an exception if the stack is empty.|
|`void push(any value)`|method|Adds a value to the stack.|
|`any pop()`|method|Pops a value from the stack.|
|`void clear()`|method|Empties a stack.|

### Maps

A map is a collection of key-value pairs. The key is used as an index to add, retrieve, and update values ​​in the map. So, a map can be thought of as a list where the indices are neither necessarily integers nor necessarily contiguous. Similar to lists, you typically create a map using a map initializer. After that, you can get the number of key-value pairs stored in the map by reading the "size" property. The **contains** operator can be used to check the presence of a particular key in the map. The "containsValue" method on the other hand is used to check the existence of a particular value in the map. To remove a pair, simply call the "remove" method. The "keys" and "values" properties are used to retrieve all the keys and all the values ​in a map, respectively. Note that both methods return sets. So, if a value appears twice in a map. The "entries" property returns a set of all the key-value pairs of a map, each pair being represented as a tuple. To get all the keys associated with a particular value in a map, call its "keysOf" method with that value as a parameter. The "frequencyOf" method on the other hand simply tells how many distinct keys a value is associated with. So `someMap.frequencyOf(someValue)` is equivalent to `someMap.keysOf(someValue).size`. The "inverse" method is used to create a map in which the key-value pairs are inverse to those in the calling map. Finally, to make a map empty, simply call its "clear" method. Here is an example of using the map in AddyScript.

Example:

```JS
tom = {'name' => 'Tom Berenger', 'job' => 'Lawyer', 'age' => 38};
tom['company'] = 'Holy Lawyers & co.';
tom['hire date'] = `2004-05-18`;
tom['salary'] = 3600D;

foreach (prop => value in tom)
    println('{0} : {1}', prop, value);

someProperty = readln('Type the name of a property: ');

if (tom contains someProperty)
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
|**contains**|a map to the left and anything to the right|Checks if the map contains a pair with a key equal to the given value.|

In addition to the above operators, the **map** class exposes the following members:

|Member|Nature|Description|
|-|-|-|
|`int size { read; }`|property|Gets the number of key-value pairs currently stored in the map.|
|`bool empty { read; }`|property|Returns **true** if the map has no key-value pair, returns **false** otherwise.|
|`set keys { read; }`|property|Gets a set of all the keys of a map.|
|`set values { read; }`|property|Gets a set of all the distinct values of a map.|
|`set entries { read; }`|property|Gets a set of tuples representing all the key-value pairs of a map.|
|`bool containsValue(any value)`|method|Checks if the map contains the given value.|
|`any get(any key, any defaultValue)`|method|Tries to get the value that's associated with the given _key_ from the map. Returns the supplied _defaultValue_ if no value is associated with the given _key_ in the map.|
|`bool update(any key, any value)`|method|Updates the value that's associated with the given _key_ in the map. Returns **true** if a value was effectively updated, returns **false** otherwise.|
|`void add(any key, any value)`|method|Adds a new key-value pair to the map. Throws an exception if the supplied key was already present in the map.|
|`map apply(any key, closure action)`|method|Invokes the given _action_ on the value that's associated with the given _key_ in a map if any.|
|`set keysOf(any value)`|method|Gets a set of all the keys of a map that are bound to a particular value.|
|`int frequencyOf(any value)`|method|Gets the number of all the keys of a map that are bound to a particular value.<br>This is also the number of occurrences of a value in the map.|
|`bool remove(any key)`|method|Tries to remove a key-value pair from a map (the one that has the given key if any). Returns true on success, false otherwise.|
|`void clear()`|method|Empties a map.|
|`map inverse()`|method|Creates and returns a map in which key-value pairs are inverse of those of the calling one.|

### Iteration Methods

In addition to the members listed in the tables above, collection classes also have methods that can be used to iterate over their instances while performing an action, evaluating a predicate, or collecting a summary value. Such a method is generally equivalent to a loop, except that it has a more compact syntax and can appear anywhere an expression is expected, which loops cannot do. Most of these methods return the instance on which they are invoked, which allows for call chaining. The following table summarizes AddyScript iteration methods and the classes they belong to. One of them, the "times" method of the **int** and **long** types, does not belong to a collection class but has similar behavior.

|Method|Description| Example                                                                                                                                  |
|-|-|------------------------------------------------------------------------------------------------------------------------------------------|
|`int int::times(closure action)`<br>`long long::times(closure action)`|Performs the given action n times where n is the integer value on which the method is invoked.| `4.times(|i| => println('hello!'));`                                                                                                   |
|`string string::each(closure action)`<br>`blob blob::each(closure action)`<br>`tuple tuple::each(closure action)`<br>`list list::each(closure action)`<br>`set set::each(closure action)`<br>`queue queue::each(closure action)`<br>`stack stack::each(closure action)`<br>`map map::each(closure action)`|Performs the given action on each item (each character for strings, each byte for blobs, or each key-value pair for maps) of the target object. When the target object is a map, the closure expects two arguments. In any other case, it expects a single argument.| `l = [4, 2, 5, 3, 8, 6];`<br>`sum = 0;`<br>`l.each(|x| => sum += x);`<br>`println(sum);`                                               |
|`tuple tuple::eachIndex(closure action)`<br>`list list::eachIndex(closure action)`|Performs the given action on each index of the target tuple or list.| `l = [4, 2, 5, 3, 8, 6];`<br>`l.eachIndex(|i| => l[i] *= 2);`<br>`println(', '.join(..l));`                                             |
|`map map::eachKey(closure action)`|Performs the given action on each key of the target map.| `m = {'age' => 30, 'weight' => 80, 'height' => 170};`<br>`m.eachKey(|k| => println(k + ': ' + m[k]));`                                 |
|`map map::eachValue(closure action)`|Performs the given action on each distinct value of the calling map.| `m = {'age' => 70, 'weight' => 70, 'height' => 180};`<br>`m.eachValue(|v| => println(v + ': ' + m.keysOf(v)));`                        |
|`bool tuple::all(closure predicate)`<br>`bool list::all(closure predicate)`<br>`bool set::all(closure predicate)`|Evaluates a predicate on each item of a tuple, list, or set. Returns **true** if the predicate is true for all items; returns **false** otherwise.| `s = {4, 2, 0, 8, 6};`<br>`b = s.all(|e| => e % 2 == 0);`<br>`if (b) println('all them are even');`                                    |
|`bool tuple::any(closure predicate)`<br>`bool list::any(closure predicate)`<br>`bool set::any(closure predicate)`|Evaluates a predicate on each item of a tuple, list, or set. Returns **true** if the predicate is true for at least one of them; returns **false** otherwise.| `s = {4, 1, 2, 0, 3, 8, 6};`<br>`b = s.any(|e| => e % 2 == 1);`<br>`if (b) println('there is an odd one');`                            |
|`any tuple::first(closure predicate)`<br>`any list::first(closure predicate)`<br>`any set::first(closure predicate)`|Successively evaluates a predicate on each item of a tuple, list, or set, returning the first item for which the predicate evaluates to true.| `l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.first(|x| => x % 2 == 1);`<br>will return 5                                               |
|`any tuple::last(closure predicate)`<br>`any list::last(closure predicate)`|Traverse a tuple or a list backwards by successively evaluating a predicate on each of its items, returning the first item for which the predicate evaluates to true.| `l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.last(|x| => x % 2 == 1);`<br>will return 1                                                |
|`int tuple::findIndex(closure predicate)`<br>`int list::findIndex(closure predicate)`|Finds the index of the first item of a tuple or list that satisfies the given predicate.| `l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.findIndex(|x| => x % 2 == 1);`<br>will return 3                                           |
|`any tuple::findLastIndex(closure predicate)`<br>`any list::findLastIndex(closure predicate)`|Finds the index of the last item of a list that satisfies the given predicate.| `l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.findLastIndex(|x| => x % 2 == 0);`<br>will return 8                                       |
|`list list::where(closure predicate)`<br>`set set::where(closure predicate)`|Filters the contents of a tuple, list, or set based on the given predicate.| `l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`a = l.where(|x| => x % 2 == 1);`<br>will return [5, 3, 7, 1]                                    |
|`list list::select(closure transform)`<br>`set set::select(closure transform)`|Maps each item of a tuple, list, or set to the value returned by the given closure.| `s = {'nadia', 'dave', 'roland', 'rick', 'john'};`<br>`t = s.select(|x| => x.toUpper());`<br>returns {NADIA, DAVE, ROLAND, RICK, JOHN} |
|`any tuple::aggregate(any seed, closure aggregator)`<br>`any list::aggregate(any seed, closure aggregator)`<br>`any list::aggregate(any seed, closure aggregator)`|Generate a single value by aggregating the items of a tuple, list or set; the _aggregator_ is a function that takes 2 arguments: an _accumulator_ and the current item; it generates the next value of the _accumulator_; parameter _seed_ is the initial value of the _accumulator_; **aggregate** returns the last value of the _accumulator_.| `l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`sum = l.aggregate(0, |acc, val| => acc + val);`<br>will return 36                               |
|`map list::groupBy(closure criterion)`|Groups the items of a list according to the given criterion and returns a map of sub-lists identified by the distinct group identifiers that were produced by the criterion.| `l = [4, 0, 2, 5, 3, 7, 1, 8, 6];`<br>`g = l.groupBy(|x| => x % 2);`<br>will return {0 => [4, 0, 2, 8, 6], 1 => [5, 3, 7, 1]}          |

**Note**:

For iteration methods that are invoked on strings, blobs, tuples, or lists, the closure argument can optionally take a second parameter that will receive the value of the index of the current element on each iteration.

Example:

```JS
l = ['one', 'two', 'three', 'four', 'five'];
l.each(|x, i| => println('Item #' + i + ' : ' + x));
ranks = l.groupBy(|x, i| => i % 2 ? 'odd rank' : 'even rank');
println('Items at even or odd ranks: ' + ranks);
evenItems = l.where(|x, i| => i % 2 == 0);
println('Items at even ranks: ' + evenItems);
concatOddItems = l.aggregate('', |acc, val, ind| => ind % 2 == 1 ? acc + val : acc);
println('Items at odd ranks concatenated: ' + concatOddItems);
```

### Objects

Just like a collection, an object is another way to store multiple values in a single variable. These values are then called the fields of the object. The fields of the object are accessed by their name, using the dot syntax: _objectName.fieldName_.

#### Creating an object

There are 2 ways to create objects in AddyScript: by using an initializer or by invoking a constructor. We will talk about constructors in the chapter dedicated to object-oriented programming. For now, let's just talk about object initializers.

To create an object using an initializer, simply use this syntax:

`varname = object_initializer;`

An object initializer is a comma-separated list of field initializers enclosed in curly braces and preceded by the keyword **new**. A field initializer consists of an identifier (the field name) followed by an equal sign (=) and then an expression (the initial value of the field).

Example:

```JS
student = new { firstName = 'John', lastName = 'Alberti', age = 12 };
println('{0} {1} is aged {2}', student.firstName, student.lastName, student.age);
```

Sometimes it is better to initialize the object with an empty initializer and then add fields to it as needed.

Example:

```JS
student = new {};
student.firstName = 'André';
student.lastName = 'Dikos';
student.age = 23;

println('{0} {1} is aged {2}', student.firstName, student.lastName, student.age);
```

Finally, it is worth mentioning that AddyScript has the ability to convert a map into an object and vice versa. In this case, if one of the keys in the map is not a valid identifier, the resulting field (in the outgoing object) will be accessible using a special identifier of the form $1, $if or for\x20each (obtained from 1, if and for each respectively).

Example:

```JS
hash = { 'long' => 120, 'large' => 80, 'depth' => 20 };
shape = (object) hash;
println('Shape size: {0} x {1} x {2}', shape.$long, shape.large, shape.depth);
```

#### Object destructuring

In AddyScript one can initialize multiple variables at once by setting them values from the properties of an object. That mechanism is called object destructuring.
Object destructuring is achieved by writing an assignment where the _lvalue_ is a list of variable names enclosed in curly braces and the _rvalue_ an expression that evaluates to an object.
Such an assignment requires the usage of the **let** keyword (otherwise AddyScript will take the opening brace as the beginning of a block of statements).

Here is an example of object destructuring:

```JS
person = new { firstName = 'Mael', lastName = 'Jordano', age = 25 };
let { firstName, lastName, age } = person;
println($'{firstName} {lastName} is a {age} years old person');
```

In the above syntax each of the variables listed between curly braces must match a property in name in the source object.
To prevent the code from crashing in the case a property with some name is not found in the source object, a default value can be set to the target variable.
The default value is ignored when a matching property is found in the source object.

Illustration:

```JS
person = new { firstName = 'Mael', lastName = 'Jordano', age = 25 };
let { firstName, lastName, job = 'Journalist', age = 17 } = person;
println($'{firstName} {lastName} is a {age} years old {job}');
```

Objects can be destructured recursively: if the source object has a property that is also an objet it can be recursively destructured as illustrated below:

```JS
person = new { firstName = 'Mael', lastName = 'Jordano', age = 25, job = new { title = 'Accountant', company = 'Paradise Co.', since = `2018-08-12` } };
let { firstName, lastName, age, { title, company } = job } = person;
println($'{firstName} {lastName} is a {age} years old {title} at {company}');
```

If the name of a variable is preceded by the _spread_ operator (..) it will be used to collect the remaining properties of the source object (those that were not explicitly extracted).

```JS
person = new { firstName = 'Mael', lastName = 'Jordano', age = 25, job = new { title = 'Accountant', company = 'Paradise Co.', since = `2018-08-12` } };
let { firstName, lastName, age, { title, ..rest } = job } = person;
println($'{firstName} {lastName} is a {age} years old {title} at {rest.company} since {rest.since:d}');
```

#### Manipulating objects

Well, there is no special manipulation on objects, other than storing values in their fields and retrieving them later. Advanced object manipulation is a concern of object-oriented programming.

<div class="web-only" markdown="1">

[Home](README.md) | [Previous](spec-types.md) | [Next](innerfunc.md)

</div>
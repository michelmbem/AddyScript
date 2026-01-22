# Introspection

Introspection (also called reflection) allows you to discover the type of an object and its members at runtime and manipulate them dynamically.
This is useful for various scenarios, such as serialization, object mapping, dependency injection, and dynamic proxies.
AddyScript provides a comprehensive introspection API that enables you to inspect types, fields, properties, methods, and events.

## Type Information

You can obtain type information from a class using the `typeof` operator, which returns a **TypeInfo** object representing the specified type.
You can also get the type of an instance using the `type` property of the object.
The **TypeInfo** class provides various properties and methods to inspect the type.
They are listed in the table below:

| Member                                      | Nature   | Description                                                                                          |
|---------------------------------------------|----------|------------------------------------------------------------------------------------------------------|
| `string name { read; }`                     | Property | Gets the name of the type.                                                                           |
| `TypeInfo superType { read; }`              | Property | Gets the base type of the type.                                                                      |
| `bool isIntegral { read; }`                 | Property | Indicates whether the type is an integral type.                                                      |
| `bool isNumeric { read; }`                  | Property | Indicates whether the type is a numeric type.                                                        |
| `bool isTemporal { read; }`                 | Property | Indicates whether the type is a temporal type.                                                       |
| `bool isSequential { read; }`               | Property | Indicates whether the type is a sequential type.                                                     |
| `bool isCollection { read; }`               | Property | Indicates whether the type is a collection type.                                                     |
| `bool isSubclassOf(TypeInfo otherType)`     | Method   | Determines whether the current type is a subclass of the specified type.                             |
| `bool isAssignableTo(TypeInfo otherType)`   | Method   | Determines whether instances of the current type can be assigned to variables of the specified type. |
| `bool isAssignableFrom(TypeInfo otherType)` | Method   | Determines whether instances of the specified type can be assigned to variables of the current type. |
| `MethodInfo $constructor { read; }`         | Property | Gets the constructor method of the type.                                                             |
| `PropertyInfo indexer { read; }`            | Property | Gets the indexer property of the type, if any.                                                       |
| `list fields { read; }`                     | Property | Gets a list of fields defined in the type.                                                           |
| `list properties { read; }`                 | Property | Gets a list of properties defined in the type.                                                       |
| `list methods { read; }`                    | Property | Gets a list of methods defined in the type.                                                          |
| `list events { read; }`                     | Property | Gets a list of events defined in the type.                                                           |
| `list attributes { read; }`                 | Property | Gets a list of attributes applied to the type.                                                       |
| `any newInstance(..args)`                   | Method   | Creates a new instance of the type by invoking its constructor.                                      |

## Member Information

The members of a type can be inspected using various classes.
All are based on a common **MemberInfo** class that provides basic information about a member, such as its name, scope, and modifier.

### MemberInfo

The **MemberInfo** class provides the following members:

| Member                      | Nature   | Description                                                      |
|-----------------------------|----------|------------------------------------------------------------------|
| `string name { read; }`     | Property | Gets the name of the member.                                     |
| `string fullName { read; }` | Property | Gets the fully qualified name of the member.                     |
| `TypeInfo holder { read; }` | Property | Gets the type that declares the member.                          |
| `string scope { read; }`    | Property | Gets the scope of the member (Private, Protected, Public).       |
| `string modifier { read; }` | Property | Gets the modifier of the member (Static, Final, Abstract, etc.). |
| `list attributes { read; }` | Property | Gets a list of attributes applied to the member.                 |

The following subclasses of **MemberInfo** provide additional information specific to the type of member.

### FieldInfo

The **FieldInfo** class represents a field in a type and provides the following additional members:

| Member                                 | Nature   | Description                                                  |
|----------------------------------------|----------|--------------------------------------------------------------|
| `any sharedValue { read; }`            | Property | Gets the value of the static field.                          |
| `any getValue(any target)`             | Method   | Gets the value of the field for the specified target object. |
| `void setValue(any target, any value)` | Method   | Sets the value of the field for the specified target object. |

### PropertyInfo

The **PropertyInfo** class represents a property in a type and provides the following additional members:

| Member                                           | Nature   | Description                                                                        |
|--------------------------------------------------|----------|------------------------------------------------------------------------------------|
| `bool canRead { read; }`                         | Property | Indicates whether the property has a reader.                                       |
| `bool canWrite { read; }`                        | Property | Indicates whether the property has a writer.                                       |
| `MethodInfo reader { read; }`                    | Property | Gets the reader method of the property.                                            |
| `MethodInfo writer { read; }`                    | Property | Gets the writer method of the property.                                            |
| `any getValue(any target)`                       | Method   | Gets the value of the property for the specified target object.                    |
| `void setValue(any target, any value)`           | Method   | Sets the value of the property for the specified target object.                    |
| `any getItem(any target, any index)`             | Method   | Gets the value of the property at the given index for the specified target object. |
| `void setItem(any target, any index, any value)` | Method   | Sets the value of the property at the given index for the specified target object. |

### MethodInfo

The **MethodInfo** class represents a method in a type and provides the following additional members:

| Member                           | Nature   | Description                                                                 |
|----------------------------------|----------|-----------------------------------------------------------------------------|
| `list parameters { read; }`      | Property | Gets a list of parameters defined for the method.                           |
| `any invoke(any target, ..args)` | Method   | Invokes the method on the specified target object with the given arguments. |

### EventInfo

The **EventInfo** class represents an event in a type and provides the following additional members:

| Member                      | Nature   | Description                                      |
|-----------------------------|----------|--------------------------------------------------|
| `list parameters { read; }` | Property | Gets a list of parameters defined for the event. |

### ParameterInfo

The **ParameterInfo** class represents a parameter of a method or event and provides the following members:

| Member                       | Nature   | Description                                                                               |
|------------------------------|----------|-------------------------------------------------------------------------------------------|
| `string name { read; }`      | Property | Gets the name of the parameter.                                                           |
| `bool byRef { read; }`       | Property | Indicates whether the parameter is passed by reference.                                   |
| `bool vaList { read; }`      | Property | Indicates whether the parameter is a variadic parameter.                                  |
| `bool canBeEmpty { read; }`  | Property | Indicates whether the parameter can be a null reference or an empty string or collection. |
| `any defaultValue { read; }` | Property | Gets the default value of the parameter, if any.                                          |

## Introspection showcase

The following example shows how this functionality is handled in AddyScript.

```JS
function dumpScope(scope)
{
    switch (scope)
    {
        case "Private":
            print(" - ");
            break;
        case "Protected":
            print(" # ");
            break;
        case "Public":
            print(" + ");
            break;
    }
}

function dumpModifier(modifier)
{
    switch (modifier)
    {
        case "StaticFinal":
            println(" [SF]");
            break;
        case "Static":
            println(" [S]");
            break;
        case "Final":
            println(" [F]");
            break;
        case "Abstract":
            println(" [A]");
            break;
        default:
            println();
    }
}

function dumpField(field)
{
    dumpScope(field.scope);
    print(field.fullName);
    dumpModifier(field.modifier);
}

function dumpProperty($property)
{
    dumpScope($property.scope);
    print($property.fullName);

    print(" {");
    if ($property.canRead) {
        dumpScope($property.reader.scope);
        print("read;");
    }
    if ($property.canWrite) {
        dumpScope($property.writer.scope);
        print("write;");
    }
    print(" }");

    dumpModifier($property.modifier);
}

function dumpParameter(parameter)
{
    if (parameter.byRef)
        print("&");
    else if (parameter.vaList)
        print("..");

    print(parameter.name);

    if (!parameter.canBeEmpty)
        print("!");

    var defVal = parameter.defaultValue;
    if (defVal is string)
        print(" = '" + defVal.replace("'", "\\'") + "'");
    else if (defVal !== null)
        print(" = " + defVal);
}

function dumpParameters(parameters)
{
    print("(");
    var notFirst = false;
    foreach (parameter in parameters)
    {
        if (notFirst) print(", ");
        dumpParameter(parameter);
        notFirst = true;
    }
    print(")");
}

function dumpMethod(method)
{
    dumpScope(method.scope);
    print(method.fullName);
    dumpParameters(method.parameters);
    dumpModifier(method.modifier);
}

function dumpEvent(_event)
{
    dumpScope(_event.scope);
    print(_event.fullName);
    dumpParameters(_event.parameters);
    dumpModifier(_event.modifier);
}

function underline(msg)
{
    println(msg);
    println('-' * msg.length);
}

function doubleLine()
{
    println();
    println('=' * 40);
    println();
}

function reflect(type, otherType = null)
{
    var title = type.name;
    for (var t = type.superType; t !== null; t = t.superType)
        title += " < " + t.name;
    underline(title);

    println($"isIntegral: {type.isIntegral}");
    println($"isNumeric: {type.isNumeric}");
    println($"isTemporal: {type.isTemporal}");
    println($"isSequential: {type.isSequential}");
    println($"isCollection: {type.isCollection}");
    
    if (otherType is not null)
    {
        println($"isSubclassOf({otherType.name}): {type.isSubclassOf(otherType)}");
        println($"isAssignableTo({otherType.name}): {type.isAssignableTo(otherType)}");
        println($"isAssignableFrom({otherType.name}): {type.isAssignableFrom(otherType)}");
    }

    println();
    underline("contructor:");
    dumpMethod(type.$constructor);

    if (type.indexer !== null) {
        println();
        underline("indexer:");
        dumpProperty(type.indexer);
    }

    println();
    underline("fields:");
    foreach (field in type.fields)
        dumpField(field);

    println();
    underline("properties:");
    foreach ($property in type.properties)
        dumpProperty($property);

    println();
    underline("methods:");
    foreach (method in type.methods)
        dumpMethod(method);

    println();
    underline("events:");
    foreach (_event in type.events)
        dumpEvent(_event);
}

function manipulate()
{
    underline('Manipulating types by reflection:');

    type = typeof(Exception);
    inst = type.newInstance('MyException', 'An error has occurred.');
    (name, message) = (type.properties['name'].getValue(inst), type.properties['message'].getValue(inst));
    println($'Exception created: name = {name}, message = {message}');

    inst = typeof(string).methods['split'].invoke("2, 4, 6, 8, 10", ", ");
    println($'List created: {inst}');
    sum = typeof(list).methods['aggregate'].invoke(inst, 0, |a, b| => a + (int)b);
    println($'Sum of items in the list: {sum}');
}

reflect(typeof(Exception), typeof(object));
doubleLine();
reflect(typeof(int), typeof(long));
doubleLine();
reflect(typeof(tuple));
doubleLine();
reflect(typeof(map));
doubleLine();
manipulate();
readln();
```

The above code will output detailed information about the **Exception**, **int**, **tuple**, and **map** types, including their fields, properties, methods, and events.
It will also demonstrate how to create instances and invoke methods using reflection.

<div markdown class="web-only">

[Home](README.md) | [Previous](inheritance.md) | [Next](interop.md)

</div>
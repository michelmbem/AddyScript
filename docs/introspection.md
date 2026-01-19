# Introspection

Introspection (also called reflection) allows you to discover the type of an object and its members at runtime.
The following example shows how this functionality is handled in AddyScript.

### Example:

Introspection showcase

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

<div class="web-only" markdown="1">

[Home](README.md) | [Previous](inheritance.md) | [Next](interop.md)

</div>
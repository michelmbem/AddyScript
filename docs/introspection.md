# Introspection

Introspection (also called reflection) allows you to discover the type of an object and its members at runtime. The following example shows how this functionality is handled in AddyScript.

Example:

Introspecting the **Exception** class

```Cpp
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
        print("ref ");
    else if (parameter.vaArgs)
        print("params ");
    
    print(parameter.name);
    
    var defVal = parameter.defaultValue;
    if (defVal is string)
        print(" = '" + defVal.replace("'", "\\'") + "'");
    else if (defVal !== null)
        print(" = " + defVal);
}

function dumpMethod(method)
{
    dumpScope(method.scope);
    print(method.fullName);
    
    print("(");
    var comma = false;
    foreach (parameter in method.parameters)
    {
        if (comma) print(", ");
        dumpParameter(parameter);
        comma = true;
    }
    print(")");
    
    dumpModifier(method.modifier);
}

function dumpEvent(_event)
{
    dumpScope(_event.scope);
    print(_event.fullName);
    
    print("(");
    var comma = false;
    foreach (parameter in _event.parameters)
    {
        if (comma) print(", ");
        dumpParameter(parameter);
        comma = true;
    }
    print(")");
    
    dumpModifier(_event.modifier);
}

function underline(msg)
{
    println(msg);
    println('-' * msg.length);
}

function reflect(type)
{
    var title = type.name;
    for (var t = type.superType; t !== null; t = t.superType)
        title += " < " + t.name;
    underline(title);
    
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

reflect(typeof(Exception));
readln();
```

**Notes**: once you have discovered the members of a type by introspection, you can activate them using the eval function.

[Home](README.md) | [Previous](inheritance.md) | [Next](interop.md)
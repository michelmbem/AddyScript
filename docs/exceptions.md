# Exceptions handling

### Handling errors at runtime

AddyScript's **try-catch-finally** statement provides an elegant way to handle errors that occur during script execution. You'll typically use this statement when you want to execute a sequence of statements that are likely to generate errors and you don't want your script to be interrupted because of those errors. Here's how to use it:

#### Example:

Let the user enter an expression that we will evaluate. The expression entered by the user may be wrong.

```JS
retry:
f = readln('f(x) = ');
x = (float) readln('x = ');
done = false;

try
{
    y = eval(f);
    println($'f({x}) = {y}');
    done = true;
}
catch (e)
{
    println(e.name + ' : ' + e.message);
    goto retry;
}
finally
{
    if (done)
    {
        println('done!');
        readln();
    }
    else
        println('type another expression');
}
```

**Remarks**:

1. The **catch** and **finally** blocks are both optional, but you cannot omit them both at the same time. There must always be a **catch** or a **finally** block in a **try-catch-finally** statement.
2. If your **finally** block contains a **goto** statement, make sure that the statement does not attempt to jump out of the block (i.e., you are not allowed to jump out of a **finally** block).
3. A **return** statement cannot appear in a **finally** block.
4. The contents of the **finally** block are always executed, whether or not an error occurs.

### Raising errors

To raise an error, simply use the following syntax:

`throw new Exception('name', 'message');`

or simply

`throw new Exception('message');`

or more simply

`throw 'message';`

Of course, you don't have to throw an instance of the **Exception** class. But if you throw something else, it will be used as the exception message.

### Try-with-resource

There is a special variant of the **try-catch-finally** statement that has an argument associated with it. This argument is called a _resource_. The _resource_ is intended to be immediately released (meaning that its "dispose" method is automatically invoked) once the **try-catch-finally** statement completes. When a **try-catch-finally** statement owns a resource, it is called a **try-with-resource** statement. In a **try-with-resource** statement, the **catch** and **finally** blocks can be omitted.

#### Example

Let's copy a file from one place to another with a Stream that should be close at the end.

```JS
import System::Environment;
import System::IO;

const BLOCK_SIZE = 4096;
const EMPTY_BLOCK = '\0' * BLOCK_SIZE;

try (input = File::OpenRead(@"E:\Videos\TV Series\Breaking Bad\Saison 1\01x01.avi"))
{
    try (output = File::Create(Path::Combine(Environment::GetFolderPath('DesktopDirectory'), 'BBS1E01.avi')))
    {
        var block = EMPTY_BLOCK, m = 0, n;
        
        while (true) {
            n = input.Read(block, 0, BLOCK_SIZE);
            if (n <= 0) break;
            output.Write(block, 0, n);
            m += n;
            println($'{m} bytes written');
        }
    }
}
catch (e)
{
    println('Error (' + e.name + ') : ' + e.message);
    readln();
}
```

[Home](README.md) | [Previous](interop.md) | [Next](grammar.md)
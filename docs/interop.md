# Interacting with .Net and the host platform

### Creating instances of .Net types

AddyScript provides tight integration with the .Net platform. A script can create instances of virtually any .Net type, initialize its properties, and invoke its methods. The scripting engine has an intelligent mechanism to map .Net types to their AddyScript counterparts and vice versa. Even generic types are supported. It is also possible to use AddyScript functions as event handlers for instances of .Net classes. Just remember that for a type to be visible from the script, its assembly must be in the References property of the ScriptContext object passed to the constructor of the current instance of the ScriptEngine class when it is created. Here is an example of using the System.Collections.Generic.LinkedList<>, the System.Collections.Generic.SortedDictionary<> and the System.Tuple<> classes:

```JS
function underline(msg)
{
    println(msg);
    println('-' * msg.length);
}

// For generic .Net types, the number of type arguments should be specified in curly braces at the end of the name
l = new System::Collections::Generic::LinkedList{1}();
for (i = 1; i < 1000; i *= 2)
    l.AddLast(i);

underline('LinkedList{1}:');
foreach(item in l)
    print($'[{item}]->');
println('END');

println();

d = new System::Collections::Generic::SortedDictionary{2}();
names = ['john', 'audrey', 'kyle', 'phil', 'steve', 'hans'];
names.each(|name| => d[name] = randint(18, 40));

underline('SortedDictionary{2}:');
foreach(key => value in d)
    println($"{key}\t: {value}");

println();

underline('System::Tuple{3}:');
t = new System::Tuple{3}('Jason Donovan', 32, `2010-03-15`);
println($"{t.Item1} is {t.Item2} years old, he works for us since {t.Item3:d}.");
```

The integration of AddyScript into the .Net platform is well demonstrated in some of the provided example scripts like _datagrid.add_, _copy-stream.add_, _guibuilder.add_ or _dbo.add_ (and its dependencies).

### Accessing to .Net types static members

Well, you do this the same way you access static members of AddyScript types. Here's an example:

```JS
// Enumeration members can either be represented by their name or by their integer value.
path = System::IO::Path::Combine(System::Environment::GetFolderPath('DesktopDirectory'), 'Products.xls');

if (System::IO::File::Exists(path))
    System::IO::File::Delete(path);
```

### Avoiding to type fully qualified names

The **import** directive can be used to import .Net types and/or namespaces. Here is what the previous example would look like with an import directive:

```JS
// from the System namespace, import only the Environment type
import System::Environment;
// import the entire System.IO namespace
import System::IO;

path = Path::Combine(GetFolderPath('DesktopDirectory'), 'Products.xls');

if (File::Exists(path)) File::Delete(path);
```

An optional alias can be provided for the imported namespace or type to avoid possible naming conflicts. Illustration:

```JS
import System::Environment as Env;
import System::IO as IO;

path = IO::Path::Combine(Env::GetFolderPath('DesktopDirectory'), 'Products.xls');

if (IO::File::Exists(path)) IO::File::Delete(path);
```

### COM-Interop

The scripting engine supports COM interoperability over .Net. This allows you to create and manage instances of COM objects just like you do with regular .Net classes. Below is an example of a script that automates MS Word:

```JS
objWord = new Word::Application();
objWord.Visible = true;

objDocument = objWord.Documents.Add();

objParaHeader = objDocument.Paragraphs.Add();
objParaHeader.Range.Style = "Heading 1";
objParaHeader.Range.Text = "AddyScript In Action";
objParaHeader.Range.InsertParagraphAfter();

objParaText = objDocument.Paragraphs.Add();
objParaText.Range.Text = @"I will not say I have failed 1000 times.
I will say that I have discovered 1000 ways that can cause failure.
Thomas Edison.";
objParaText.Range.InsertParagraphAfter();

filename = System::IO::Path::Combine(System::Environment::GetFolderPath('DesktopDirectory'), "WordExample.doc");
objDocument.SaveAs(filename);
objDocument.Close();

objWord.Quit();
```

The interoperability of AddyScript and COM is well demonstrated in the _msxml.add_, _word.add_ (shown above), and _ado.add_ sample scripts. Note that the scripting engine still cannot attach handlers to a COM object's events.

[Home](README.md) | [Previous](introspection.md) | [Next](exceptions.md)
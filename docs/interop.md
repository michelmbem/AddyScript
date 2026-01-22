# Interacting with .NET and the host platform

## Creating instances of .NET types

AddyScript provides tight integration with the .NET platform.
A script can create instances of virtually any .NET type, initialize its properties, and invoke its methods.
The scripting engine has an intelligent mechanism to map .NET types to their AddyScript counterparts and vice versa.
Even generic types are supported. It is also possible to use AddyScript functions as event handlers for instances of .NET classes.
Just remember that for a type to be visible from the script, its assembly must be in the References property of
the ScriptContext object passed to the constructor of the current instance of the ScriptEngine class when it is created.
Here is an example of using the System.Collections.Generic.LinkedList&lt;T&gt;, the System.Collections.Generic.SortedDictionary&lt;TKey, TValue&gt;
and the System.Tuple&lt;TItem1, TItem2, TItem3&gt; classes:

```JS
import System;
import System::Collections::Generic;

l = new LinkedList{1}();
for (i = 1; i < 1000; i *= 2)
    l.AddLast(i);

println('LinkedList{1}:');
println('--------------');
println('[' + ']->['.join(..l) + ']');
println();

d = new SortedDictionary{2}();
names = ['john', 'audrey', 'kyle', 'phil', 'steve', 'hans'];
names.each(|name| => d[name] = randint(18, 40));

println('SortedDictionary{2}:');
println('--------------------');
foreach(key => value in d)
println($"{key, 6} : {value}");
println();

println('System::Tuple{3}:');
println('-----------------');
t = new Tuple{3}('Jason Donovan', 32, `2010-03-15`);
println($"{t[0]} is {t[1]} years old, he works for us since {t[2]:d}.");
```

Output:

```
LinkedList{1}:
--------------
[1]->[2]->[4]->[8]->[16]->[32]->[64]->[128]->[256]->[512]

SortedDictionary{2}:
--------------------
  audrey : 28
    hans : 24
    john : 35
    kyle : 22
    phil : 30
   steve : 27

System::Tuple{3}:
-----------------
Jason Donovan is 32 years old, he works for us since 2010-03-15.
```

The integration of AddyScript into the .NET platform is well demonstrated in some of the provided example scripts
like _datagrid.add_, _copy-stream.add_, _guibuilder.add_ or _dbo.add_ (and its dependencies).

## Accessing to .NET types static members

Well, you do this the same way you access static members of AddyScript types. Here's an example:

```JS
// Enumeration members can either be represented by their name or by their integer value.
path = System::IO::Path::Combine(System::Environment::GetFolderPath('DesktopDirectory'), 'Products.xls');

if (System::IO::File::Exists(path))
    System::IO::File::Delete(path);
```

## Avoiding to type fully qualified names

The **import** directive can be used to import .NET types and/or namespaces.
Here is what the previous example would look like with an import directive:

```JS
// from the System namespace, import only the Environment type
import System::Environment;
// import the entire System.IO namespace
import System::IO;

path = Path::Combine(Environment::GetFolderPath('DesktopDirectory'), 'Products.xls');

if (File::Exists(path)) File::Delete(path);
```

An optional alias can be provided for the imported namespace or type to avoid possible naming conflicts. Illustration:

```JS
import System::Environment as Env;
import System::IO as IO;

path = IO::Path::Combine(Env::GetFolderPath('DesktopDirectory'), 'Products.xls');

if (IO::File::Exists(path)) IO::File::Delete(path);
```

## COM-Interop

The scripting engine supports COM interoperability over .NET.
This allows you to create and manage instances of COM objects just like you do with regular .NET classes.
Below is an example of a script that automates MS Word:

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

The interoperability of AddyScript and COM is well demonstrated in the _msxml.add_, _word.add_ (shown above), and _ado.add_ sample scripts.
Note that the scripting engine still cannot attach handlers to a COM object's events.

<div markdown class="web-only">

[Home](README.md) | [Previous](introspection.md) | [Next](exceptions.md)

</div>
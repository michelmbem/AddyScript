using System;
using System.Collections.Generic;
using System.Linq;
using AddyScript.Gui.Extensions;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.Autocomplete;

internal class CodeSnippetCompletionData(string title, string snippet, string description) : ICompletionData
{
    private static readonly IImage SnippetIcon = ImageFactory.LoadFontIcon("fa-code");

    static CodeSnippetCompletionData()
    {
        string[][] snippets =
        [
            ["simple-if", "if (true) ^;"],
            ["simple-else", "else ^;"],
            ["block-if", "if (true) {\n\t^\n}"],
            ["block-else", "else {\n\t^\n}"],
            ["switch", "switch (0) {\n\tcase 0:\n\t\t^\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
            ["for", "for (;;) {\n\t^\n}"],
            ["foreach", "foreach (item in []) {\n\t^\n}"],
            ["foreach-kv", "foreach (key => value in {=>}) {\n\t^\n}"],
            ["while", "while (true) {\n\t^\n}"],
            ["do-while", "do {\n\t^\n} while (true);"],
            ["try-catch", "try {\n\t^\n} catch (e) {\n\tprintln(e);\n}"],
            ["try-finally", "try {\n\t^\n} finally {\n}"],
            ["try-catch-finally", "try {\n\t^\n} catch (e) {\n\tprintln(e);\n} finally {\n}"],
            ["try-with-resource", "try (res) {\n\t^\n}"],
            ["function", "function myFunc(arg1, arg2) {\n\t^\n}"],
            ["extern-function", "[LibImport(\"mylib\", returnType=\"Int32\")]\nextern function myFunc(\n\t^\n);"],
            ["class", "class MyClass {\n\t^\n}"],
            ["import", "import ^;"],
            ["import-as", "import ^ as alias;"],
        ];

        All = [.. from snippet in snippets
                  let description = $"Insert a code snippet like \"{snippet[1].Replace("^", "/*...*/")}\""
                  select new CodeSnippetCompletionData(snippet[0], snippet[1], description)];
    }

    public static List<CodeSnippetCompletionData> All { get; }

    public IImage Image => SnippetIcon;

    public string Text => snippet;

    public object Content => title;

    public object Description => description;

    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        string indentation = textArea.Document.GetIndentation(textArea.Caret.Line);
        string textToInsert = Text.IndentLines(indentation, true);
        int caretOffset = textToInsert.IndexOf('^');
        int segmentOffset = completionSegment.Offset;

        if (caretOffset < 0)
            caretOffset = textToInsert.Length;
        else
            textToInsert = textToInsert.Remove(caretOffset, 1);

        textArea.Document.Insert(segmentOffset, textToInsert);
        textArea.Caret.Offset = segmentOffset + caretOffset;
    }
}
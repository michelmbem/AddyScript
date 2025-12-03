using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;
using AddyScript.Gui.Extensions;

namespace AddyScript.Gui.Autocomplete;

internal class CodeSnippetData(string title, string snippet, string description) :
    AbstractCompletionData(SnippetIcon, title, snippet, description)
{
    private static readonly IImage SnippetIcon = ImageFactory.LoadFontIcon("fa-code");

    static CodeSnippetData()
    {
        string[][] snippets =
            [
                ["if", "if (true) ^;"],
                ["else", "else ^;"],
                ["if-block", "if (true) {\n\t^\n}"],
                ["else-block", "else {\n\t^\n}"],
                ["switch", "switch (0) {\n\tcase 0:\n\t\t^\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (;;) {\n\t^\n}"],
                ["foreach", "foreach (item in []) {\n\t^\n}"],
                ["while", "while (true) {\n\t^\n}"],
                ["do-while", "do {\n\t^\n} while (true);"],
                ["try-catch", "try {\n\t^\n} catch (e) {\n\tprintln(e);\n}"],
                ["try-finally", "try {\n\t^\n} finally {\n}"],
                ["try-catch-finally", "try {\n\t^\n} catch (e) {\n\tprintln(e);\n} finally {\n}"],
                ["try-resource", "try (res) {\n\t^\n}"],
                ["function", "function myFunc(arg1, arg2) {\n\t^\n}"],
                ["extern-function", "[LibImport(\"mylib\", returnType=\"Int32\")]\nextern function myFunc(\n\t^\n);"],
                ["class", "class MyClass {\n\t^\n}"],
                ["import", "import ^;"],
                ["import-as", "import ^ as alias;"],
            ];

        foreach (string[] snippet in snippets)
        {
            string description = $"Insert a code snippet like \"{snippet[1].Replace("^", "/*...*/")}\"";
            All.Add(new(snippet[0], snippet[1], description));
        }
    }

    public static List<CodeSnippetData> All { get; } = [];

    public override void Complete(TextArea textArea, ISegment segment, EventArgs args)
    {
        string indentation = textArea.Document.GetIndentation(textArea.Caret.Line);
        string textToInsert = Text.IndentLines(indentation, true);
        int caretOffset = textToInsert.IndexOf('^');
        int segmentOffset = segment.Offset;

        if (caretOffset < 0)
            caretOffset = textToInsert.Length;
        else
            textToInsert = textToInsert.Remove(caretOffset, 1);

        textArea.Document.Insert(segmentOffset, textToInsert);
        textArea.Caret.Offset = segmentOffset + caretOffset;
    }
}

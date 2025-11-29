using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;

namespace AddyScript.Gui.Autocomplete;

internal class CodeSnippetData(string title, string snippet, string description) :
    AbstractCompletionData(SNIPPET_ICON, title, snippet, description)
{
    private static readonly IImage SNIPPET_ICON = ImageFactory.LoadFontIcon("fa-code");

    static CodeSnippetData()
    {
        string[][] snippets =
            [
                ["if", "if (^) ;"],
                ["else", "else ^;"],
                ["ifb", "if (^) {\n}"],
                ["elseb", "else {\n\t^;\n}"],
                ["switch", "switch (^) {\n\tcase $label$:\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (^;;) {\n}"],
                ["foreach", "foreach (^ in $sequence$) {\n}"],
                ["while", "while (^) {\n}"],
                ["do", "do {\n\t^;\n} while ($condition$);"],
                ["try", "try {\n\t^;\n} catch (e) {\n}"],
                ["tryf", "try {\n\t^;\n} finally {\n}"],
                ["tcf", "try {\n\t^;\n} catch (e) {\n} finally {\n}"],
                ["tryres", "try (^) {\n\t;\n}"],
                ["function", "function $fname$(^) {\n}"],
                ["class", "class $cname$ {\n}"],
                ["import", "import ^;"],
                ["impas", "import ^ as $alias$;"],
            ];

        foreach (string[] snippet in snippets)
        {
            string description = $"Insert a code snippet like \"{snippet[1].Replace('^', '?')}\"";
            All.Add(new(snippet[0], snippet[1], description));
        }
    }

    public static List<CodeSnippetData> All { get; } = [];

    public override void Complete(TextArea textArea, ISegment segment, EventArgs args)
    {
        string textToInsert = Text;
        int segmentOffset = segment.Offset;
        int caretOffset = textToInsert.IndexOf('^');

        if (caretOffset < 0)
            caretOffset = textToInsert.Length;
        else
            textToInsert = textToInsert.Remove(caretOffset, 1);

        textArea.Document.Insert(segmentOffset, $"{textToInsert}\n");
        textArea.Caret.Offset = segmentOffset + caretOffset;
    }
}

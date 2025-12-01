using System;
using System.Collections.Generic;
using AddyScript.Gui.Extensions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.Autocomplete;

internal class SurroundCodeData(string title, string snippet, string description) :
    AbstractCompletionData(SurroundIcon, title, snippet, description)
{
    private const string SELECTION_PLACEHOLDER = "$selection$";
    private static readonly IImage SurroundIcon = ImageFactory.LoadFontIcon("mdi-code-braces");

    static SurroundCodeData()
    {
        string[][] templates =
            [
                ["block", "{\n$selection$\n}"],
                ["block-comment", "/*\n$selection$\n*/"],
                ["if", "if (true) {\n$selection$\n}"],
                ["else", "else {\n$selection$\n}"],
                ["switch", "switch (0) {\n\tcase 0:\n\t$selection$\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (;;) {\n$selection$\n}"],
                ["foreach", "foreach (item in []) {\n$selection$\n}"],
                ["while", "while (true) {\n$selection$\n}"],
                ["do-while", "do {\n$selection$\n} while (true);"],
                ["try-catch", "try {\n$selection$\n} catch (e) {\n\tprintln(e);\n}"],
                ["try-finally", "try {\n$selection$\n} finally {\n}"],
                ["try-catch-finally", "try {\n$selection$\n} catch (e) {\n\tprintln(e);\n} finally {\n}"],
                ["try-resource", "try (res) {\n$selection$\n}"],
                ["function", "function myFunc(arg1, arg2) {\n$selection$\n}"],
            ];

        foreach (string[] template in templates)
        {
            var kind = template[0] switch
            {
                "block" or "block-comment" or "function" => $"a {template[0]}",
                "if" or "else" => $"an {template[0]} statement",
                _ => $"a {template[0]} statement",
            };
            
            All.Add(new(template[0], template[1], $"Wrap selection in {kind}"));
        }
    }

    public static List<SurroundCodeData> All { get; } = [];

    public override void Complete(TextArea textArea, ISegment segment, EventArgs args)
    {
        Selection selection = textArea.Selection;
        string indentation = "\t" + textArea.Document.GetIndentation(selection.StartPosition.Line);
        string replacementText = Text.Replace(SELECTION_PLACEHOLDER, selection.GetText().IndentLines(indentation));
        selection.ReplaceSelectionWithText(replacementText);
    }
}

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
                ["block", "{\n\t$selection$\n}"],
                ["block-comment", "/*\n\t$selection$\n*/"],
                ["if", "if (true) {\n\t$selection$\n}"],
                ["else", "else {\n\t$selection$\n}"],
                ["switch", "switch (0) {\n\tcase 0:\n\t\t$selection$\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (;;) {\n\t$selection$\n}"],
                ["foreach", "foreach (item in []) {\n\t$selection$\n}"],
                ["while", "while (true) {\n\t$selection$\n}"],
                ["do-while", "do {\n\t$selection$\n} while (true);"],
                ["try-catch", "try {\n\t$selection$\n} catch (e) {\n\tprintln(e);\n}"],
                ["try-finally", "try {\n\t$selection$\n} finally {\n}"],
                ["try-catch-finally", "try {\n\t$selection$\n} catch (e) {\n\tprintln(e);\n} finally {\n}"],
                ["try-resource", "try (res) {\n\t$selection$\n}"],
                ["function", "function myFunc(arg1, arg2) {\n\t$selection$\n}"],
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
        string leadingSpace = Text.LeadingWhitespace(SELECTION_PLACEHOLDER);
        string indentation = textArea.Document.GetIndentation(selection.StartPosition.Line);
        string indentedSelection = selection.GetText().IndentLines(leadingSpace + indentation);
        string replacementText = Text.IndentNextLine(SELECTION_PLACEHOLDER, indentation)
            .Replace(leadingSpace + SELECTION_PLACEHOLDER, indentedSelection);
        selection.ReplaceSelectionWithText(replacementText);
    }
}

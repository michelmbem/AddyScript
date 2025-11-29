using System;
using System.Collections.Generic;
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
                ["if", "if ($condition$) {\n\t$selection$\n}"],
                ["else", "else {\n\t$selection$\n}"],
                ["switch", "switch ($condition$) {\n\tcase $label$:\n\t\t$selection$\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (;;) {\n\t$selection$\n}"],
                ["foreach", "foreach ($item$ in $sequence$) {\n\t$selection$\n}"],
                ["while", "while ($condition$) {\n\t$selection$\n}"],
                ["do-while", "do {\n\t$selection$\n} while ($condition$);"],
                ["try-catch", "try {\n\t$selection$\n} catch (e) {\n}"],
                ["try-finally", "try {\n\t$selection$\n} finally {\n}"],
                ["try-catch-finally", "try {\n\t$selection$\n} catch (e) {\n} finally {\n}"],
                ["try-resource", "try ($resource$) {\n\t$selection$\n}"],
                ["function", "function $fname$($args$) {\n\t$selection$\n}"],
            ];

        foreach (string[] template in templates)
        {
            string substitution = template[1].Replace(SELECTION_PLACEHOLDER, "\" and \"");
            All.Add(new(template[0], template[1], $"Wrap the selection between \"{substitution}\""));
        }
    }

    public static List<SurroundCodeData> All { get; } = [];

    public override void Complete(TextArea textArea, ISegment segment, EventArgs args)
    {
        Selection selection = textArea.Selection;
        string replacementText = Text.Replace(SELECTION_PLACEHOLDER, selection.GetText()) + "\n";
        selection.ReplaceSelectionWithText(replacementText);
    }
}

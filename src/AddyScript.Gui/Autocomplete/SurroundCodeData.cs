using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;

namespace AddyScript.Gui.Autocomplete;

internal class SurroundCodeData(string title, string snippet, string description) :
    AbstractCompletionData(SURROUND_ICON, title, snippet, description)
{
    private const string SelectionPlaceholder = "$selection$";
    private static readonly IImage SURROUND_ICON = ImageFactory.LoadFontIcon("mdi-code-braces");

    static SurroundCodeData()
    {
        string[][] templates =
            [
                ["ifb", "if ($condition$) {\n\t$selection$\n}"],
                ["elseb", "else {\n\t$selection$\n}"],
                ["switch", "switch ($condition$) {\n\tcase $label$:\n\t\t$selection$\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (;;) {\n\t$selection$\n}"],
                ["foreach", "foreach ($item$ in $sequence$) {\n\t$selection$\n}"],
                ["while", "while ($condition$) {\n\t$selection$\n}"],
                ["do", "do {\n\t$selection$\n} while ($condition$);"],
                ["try", "try {\n\t$selection$\n} catch (e) {\n}"],
                ["tryf", "try {\n\t$selection$\n} finally {\n}"],
                ["tcf", "try {\n\t$selection$\n} catch (e) {\n} finally {\n}"],
                ["tryres", "try ($resource$) {\n\t$selection$\n}"],
                ["function", "function $fname$() {\n\t$selection$\n}"],
            ];

        foreach (string[] template in templates)
        {
            string substitution = template[1].Replace(SelectionPlaceholder, "\" and \"");
            All.Add(new(template[0], template[1], $"Wrap the selection between \"{substitution}\""));
        }
    }

    public static List<SurroundCodeData> All { get; } = [];

    public override void Complete(TextArea textArea, ISegment segment, EventArgs args)
    {
        var selection = textArea.Selection;
        string replacement = Text.Replace(SelectionPlaceholder, selection.GetText());
        selection.ReplaceSelectionWithText(replacement);
    }
}

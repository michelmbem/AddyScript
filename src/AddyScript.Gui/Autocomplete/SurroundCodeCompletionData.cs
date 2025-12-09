using System;
using System.Collections.Generic;
using AddyScript.Gui.Extensions;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.Autocomplete;

internal class SurroundCodeCompletionData(string title, string template, string description) : ICompletionData
{
    private const string SELECTION_PLACEHOLDER = "$selection$";
    private static readonly IImage SurroundIcon = ImageFactory.LoadFontIcon("mdi-code-braces");

    static SurroundCodeCompletionData()
    {
        string[][] templates =
            [
                ["block", "{\n\t$selection$\n}"],
                ["block-comment", "/*\n\t$selection$\n*/"],
                ["if", "if (true) {\n\t$selection$\n}"],
                ["if-main", "if (__name == 'main') {\n\t$selection$\n}"],
                ["else", "else {\n\t$selection$\n}"],
                ["switch", "switch (0) {\n\tcase 0:\n\t\t$selection$\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (;;) {\n\t$selection$\n}"],
                ["foreach", "foreach (item in []) {\n\t$selection$\n}"],
                ["foreach-kv", "foreach (key => value in {=>}) {\n\t$selection$\n}"],
                ["while", "while (true) {\n\t$selection$\n}"],
                ["do-while", "do {\n\t$selection$\n} while (true);"],
                ["try-catch", "try {\n\t$selection$\n} catch (e) {\n\tprintln(e);\n}"],
                ["try-finally", "try {\n\t$selection$\n} finally {\n}"],
                ["try-catch-finally", "try {\n\t$selection$\n} catch (e) {\n\tprintln(e);\n} finally {\n}"],
                ["try-with-resource", "try (res) {\n\t$selection$\n}"],
                ["function", "function myFunc(arg1, arg2) {\n\t$selection$\n}"],
            ];

        List<SurroundCodeCompletionData> all = [];

        foreach (string[] template in templates)
        {
            var kind = template[0] switch
            {
                "block" or "block-comment" or "function" => $"a {template[0]}",
                "if" or "else" => $"an {template[0]} statement",
                _ => $"a {template[0]} statement",
            };
            
            all.Add(new (template[0], template[1], $"Wrap selection in {kind}"));
        }

        All = all;
    }

    public static List<SurroundCodeCompletionData> All { get; }

    public IImage Image => SurroundIcon;

    public string Text => template;

    public object Content => title;

    public object Description => description;

    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        Selection selection = textArea.Selection;
        string selIndentation = textArea.Document.GetIndentation(selection.StartPosition.Line);
        string leadingSpace = Text.LeadingWhitespace(SELECTION_PLACEHOLDER);
        string strippedFragment = leadingSpace + SELECTION_PLACEHOLDER;
        string insertedFragment = selection.GetText().IndentLines(leadingSpace);
        string replacementText = Text.IndentLines(selIndentation, true).Replace(strippedFragment, insertedFragment);
        selection.ReplaceSelectionWithText(replacementText);
    }
}

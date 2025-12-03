using System.Collections.Generic;
using System.Text.RegularExpressions;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace AddyScript.Gui;

public partial class FoldingStrategy
{
    private static readonly Regex ImportRegex = CreateImportRegex();

    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var newFoldings = CreateNewFoldings(document);
        manager.UpdateFoldings(newFoldings, -1);
    }

    private IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
    {
        List<NewFolding> newFoldings = [];

        // --- 1. Handle import directives block ---
        AddImportFoldings(document, newFoldings);

        // --- 2. Handle braces, brackets and parentheses ---
        AddBraceFoldings(document, newFoldings, '{', '}');
        AddBraceFoldings(document, newFoldings, '[', ']');
        AddBraceFoldings(document, newFoldings, '(', ')');

        // --- 3. Handle multiline comments ---
        AddCommentFoldings(document, newFoldings);

        newFoldings.Sort((a, b) =>
        {
            int cmp = a.StartOffset.CompareTo(b.StartOffset);
            return cmp == 0 ? a.EndOffset.CompareTo(b.EndOffset) : cmp;
        });

        return newFoldings;
    }

    private void AddImportFoldings(TextDocument document, List<NewFolding> newFoldings)
    {
        var matches = ImportRegex.Matches(document.Text);
        if (matches.Count < 2) return;

        var first = matches[0];
        var last = matches[^1];

        newFoldings.Add(new NewFolding(first.Index, last.Index + last.Length)
        {
            Name = "import ..."
        });
    }

    private void AddBraceFoldings(TextDocument document,
                                  List<NewFolding> newFoldings,
                                  char openingBrace,
                                  char closingBrace)
    {
        var startOffsets = new Stack<int>();
        var textLength = document.TextLength;
        var lastNewLineOffset = 0;

        for (var i = 0; i < textLength; ++i)
        {
            var c = document.GetCharAt(i);

            if (c == openingBrace)
            {
                startOffsets.Push(i);
            }
            else if (c == closingBrace && startOffsets.Count > 0)
            {
                var startOffset = startOffsets.Pop();
                // don't fold if opening and closing brace are on the same line
                if (startOffset < lastNewLineOffset)
                    newFoldings.Add(new NewFolding(startOffset, i));
            }
            else if (c is '\n' or '\r')
            {
                lastNewLineOffset = i + 1;
            }
        }
    }

    private void AddCommentFoldings(TextDocument document, List<NewFolding> newFoldings)
    {
        var startOffsets = new Stack<int>();
        var textLength = document.TextLength;
        var lastNewLineOffset = 0;
        var i = 0;

        while (i < textLength - 1)
        {
            var c = document.GetCharAt(i);
            var d = document.GetCharAt(i + 1);

            switch (c)
            {
                case '/' when d == '*':
                    startOffsets.Push(i);
                    i += 2; // skip *
                    continue;
                case '*' when d == '/' && startOffsets.Count > 0:
                {
                    var startOffset = startOffsets.Pop();
                    var endOffset = i + 2;

                    if (startOffset < lastNewLineOffset)
                    {
                        newFoldings.Add(new NewFolding(startOffset, endOffset)
                        {
                            Name = "/*...*/"
                        });

                        i = endOffset; // skip /
                        continue;
                    }
                    
                    break;
                }
                case '\n' or '\r':
                    lastNewLineOffset = i + 1;
                    break;
            }

            ++i;
        }
    }

    [GeneratedRegex(@"^\s*import\s+.*?;", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex CreateImportRegex();
}
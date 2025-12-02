using AvaloniaEdit.Document;

namespace AddyScript.Gui.Extensions;

public static class TextDocumentExtensions
{
    public static int GetIndentationChars(this TextDocument document, int lineNumber, int tabSize = 4)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);
        int indentChars = 0;

        foreach (char c in lineText)
        {
            if (c == ' ')
                ++indentChars;
            else if (c == '\t')
                indentChars += tabSize;
            else
                break;
        }

        return indentChars;
    }

    public static int GetIndentationLevel(this TextDocument document, int lineNumber, int tabSize = 4) =>
        document.GetIndentationChars(lineNumber, tabSize) / tabSize;

    public static string GetIndentation(this TextDocument document, int lineNumber)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);

        int i = 0;
        while (i < lineText.Length && lineText[i] is ' ' or '\t')
            ++i;

        return lineText[..i];
    }

    public static void IndentLine(this TextDocument document, int lineNumber, int tabSize = 4)
    {
        int lineOffset = document.GetLineByNumber(lineNumber).Offset;
        document.Insert(lineOffset, "\t");
    }

    public static void UnindentLine(this TextDocument document, int lineNumber, int tabSize = 4)
    {
        int lineOffset = document.GetLineByNumber(lineNumber).Offset;

        switch (document.GetCharAt(lineOffset))
        {
            case '\t':
                document.Remove(lineOffset, 1);
                break;
            case ' ':
            {
                int spaceCount = 0;

                while (spaceCount < tabSize && document.GetCharAt(lineOffset + spaceCount) == ' ')
                    ++spaceCount;

                if (spaceCount > 0)
                    document.Remove(lineOffset, spaceCount);

                break;
            }
        }
    }

    public static void CommentLine(this TextDocument document, int lineNumber)
    {
        int lineOffset = document.GetLineByNumber(lineNumber).Offset;
        document.Insert(lineOffset, "//");
    }

    public static void UncommentLine(this TextDocument document, int lineNumber)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);

        if (!lineText.TrimStart().StartsWith("//")) return;

        int slashIndex = lineText.IndexOf('/');
        document.Remove(line.Offset + slashIndex, 2);
    }
}
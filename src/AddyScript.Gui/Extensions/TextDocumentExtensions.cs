using AvaloniaEdit.Document;

namespace AddyScript.Gui.Extensions;

public static class TextDocumentExtensions
{
    public static int GetIndentationSize(this TextDocument document, int lineNumber, int tabSize = 4)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);
        int indentUnits = 0;

        foreach (char c in lineText)
        {
            if (c == ' ')
                ++indentUnits;
            else if (c == '\t')
                indentUnits += tabSize;
            else
                break;
        }

        return indentUnits;
    }

    public static int GetIndentationLevel(this TextDocument document, int lineNumber, int tabSize = 4) =>
        document.GetIndentationSize(lineNumber, tabSize) / tabSize;

    public static string GetIndentation(this TextDocument document, int lineNumber)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);
        int i = 0;
        
        while (i < lineText.Length && (lineText[i] == ' ' || lineText[i] == '\t'))
            ++i;

        return lineText[..i];
    }
}
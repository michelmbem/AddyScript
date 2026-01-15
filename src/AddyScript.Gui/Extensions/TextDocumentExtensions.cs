using AvaloniaEdit.Document;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides extension methods for manipulating and analyzing <see cref="TextDocument"/>s.
/// </summary>
internal static class TextDocumentExtensions
{
    /// <summary>
    /// Gets the indentation size of the given line.
    /// </summary>
    /// <param name="document">The target document</param>
    /// <param name="lineNumber">The line number</param>
    /// <param name="tabSize">The number of space chars per tab char</param>
    /// <returns>The number of individual space chars at the beginning of the line</returns>
    public static int GetIndentationSize(this TextDocument document, int lineNumber, int tabSize = 4)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);
        int indentationSize = 0;

        foreach (char c in lineText)
        {
            if (c == ' ')
                ++indentationSize;
            else if (c == '\t')
                indentationSize += tabSize;
            else
                break;
        }

        return indentationSize;
    }

    /// <summary>
    /// Gets the indentation level of a line.
    /// </summary>
    /// <param name="document">The target document</param>
    /// <param name="lineNumber">The line number</param>
    /// <param name="tabSize">The number of space chars per tab char</param>
    /// <returns>The number of tab chars at the beginning of the line</returns>
    public static int GetIndentationLevel(this TextDocument document, int lineNumber, int tabSize = 4) =>
        document.GetIndentationSize(lineNumber, tabSize) / tabSize;

    /// <summary>
    /// Get the part of line that reprensents indentation.
    /// </summary>
    /// <param name="document">The target document</param>
    /// <param name="lineNumber">The line number</param>
    /// <returns>A substring of all space and tab chars located at the beginning of the line</returns>
    public static string GetIndentation(this TextDocument document, int lineNumber)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);

        int i = 0;
        while (i < lineText.Length && lineText[i] is ' ' or '\t')
            ++i;

        return lineText[..i];
    }

    /// <summary>
    /// Increases the indentation level of a line.
    /// </summary>
    /// <param name="document">The target document</param>
    /// <param name="lineNumber">The line number</param>
    /// <param name="useSpaces">Determines wheter to use spaces instead of tabs or not</param>
    /// <param name="tabSize">The number of spaces per tab. Only used when <paramref name="useSpaces"/> is <b>true</b></param>
    public static void IndentLine(this TextDocument document, int lineNumber, bool useSpaces = false, int tabSize = 4)
    {
        int lineOffset = document.GetLineByNumber(lineNumber).Offset;
        string indentation = useSpaces ? new (' ', tabSize) : "\t";
        document.Insert(lineOffset, indentation);
    }

    /// <summary>
    /// Decreases the indentation level of a line.
    /// </summary>
    /// <param name="document">The target document</param>
    /// <param name="lineNumber">The line number</param>
    /// <param name="tabSize">The number of spaces per tab</param>
    public static void OutdentLine(this TextDocument document, int lineNumber, int tabSize = 4)
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

    /// <summary>
    /// Adds a double-slash sign at the beginning of a line.
    /// </summary>
    /// <param name="document">The target document</param>
    /// <param name="lineNumber">The line number</param>
    public static void CommentLine(this TextDocument document, int lineNumber)
    {
        int lineOffset = document.GetLineByNumber(lineNumber).Offset;
        document.Insert(lineOffset, "//");
    }

    /// <summary>
    /// Removes the first occurence of the double-slash sign from the beginning of a line.
    /// </summary>
    /// <param name="document">The target document</param>
    /// <param name="lineNumber">The line number</param>
    public static void UncommentLine(this TextDocument document, int lineNumber)
    {
        DocumentLine line = document.GetLineByNumber(lineNumber);
        string lineText = document.GetText(line);

        if (!lineText.TrimStart().StartsWith("//")) return;

        int slashIndex = lineText.IndexOf('/');
        document.Remove(line.Offset + slashIndex, 2);
    }
}
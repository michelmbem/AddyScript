using System;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.CodeCompletion;

public class KeywordData : ICompletionData
{
    private readonly string text;
    private readonly int imageIndex;
    private readonly string description;
    
    public KeywordData(string text, int imageIndex = -1, string description = null)
    {
        this.text = text;
        this.imageIndex = imageIndex;
        this.description = description ?? text;
    }

    public string Text => text;
    public object Content => Text;
    public IImage Image => null;
    public object Description => description;
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment segment, EventArgs args)
    {
        textArea.Document.Replace(segment, Text);
    }
}
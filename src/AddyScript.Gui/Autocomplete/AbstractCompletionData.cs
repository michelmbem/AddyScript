using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;

namespace AddyScript.Gui.Autocomplete;

internal abstract class AbstractCompletionData(IImage image, object content, string text, string description) :
    ICompletionData
{
    /// <summary>
    /// The image/icon associated with this completion data.
    /// </summary>
    public IImage Image => image;

    /// <summary>
    /// The content displayed in the completion list.
    /// </summary>
    public object Content => content;

    /// <summary>
    /// The text to be inserted when this completion is selected.
    /// </summary>
    public string Text => text;

    /// <summary>
    /// The description of this completion data.
    /// </summary>
    public object Description => description;

    /// <summary>
    /// The priority of this completion data (unused).
    /// </summary>
    public double Priority => 0;

    public abstract void Complete(TextArea textArea, ISegment segment, EventArgs args);
}

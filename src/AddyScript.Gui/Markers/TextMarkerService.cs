using System.Collections.Generic;
using System.Linq;
using AddyScript.Gui.Extensions;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace AddyScript.Gui.Markers;

internal class TextMarker(int offset, int endOffset, IBrush color = null) : ISegment
{
    public int Offset { get; } = offset;
    
    public int EndOffset { get; } = endOffset;
    
    public int Length => EndOffset - Offset;

    public IBrush Color { get; } = color ?? Brushes.Red ;
    
    public string ToolTip { get; set; }
}

internal class TextMarkerService(TextEditor editor) : IBackgroundRenderer
{
    private readonly List<TextMarker> markers = [];

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext context)
    {
        if (!textView.VisualLinesValid) return;

        foreach (var marker in markers)
        {
            foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
            {
                var underline = new Rect(r.X, r.Bottom - 2, r.Width, 2);
                context.DrawSquiggle(underline, marker.Color);
            }
        }
    }

    public void AddMarker(TextMarker marker) => markers.Add(marker);
    
    public void ClearMarkers() => markers.Clear();

    public TextMarker GetMarkerAt(TextViewPosition? position)
    {
        if (position == null) return null;
        int offset = editor.Document.GetOffset(position.Value.Location);
        return markers.FirstOrDefault(m => m.Contains(offset, 1));
    }
}
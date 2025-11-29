using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.Markers;

internal class MarkerMargin : AbstractMargin
{
    private readonly Dictionary<int, string> markerLines = [];

    public void AddMarker(int line, string tooltip)
    {
        markerLines[line] = tooltip;
        InvalidateVisual();
    }

    public void ClearMarkers()
    {
        markerLines.Clear();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var textView = TextView;
        if (textView is not { VisualLinesValid: true }) return;

        foreach (var vl in textView.VisualLines)
        {
            int line = vl.FirstDocumentLine.LineNumber;
            if (markerLines.ContainsKey(line))
            {
                var rect = new Rect(0, vl.VisualTop + 1, Bounds.Width, vl.Height - 2);
                context.DrawImage(ImageFactory.LoadFontIcon("fa-ban"), rect);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pos = e.GetPosition(this);
        var textView = TextView;
        
        if (textView is not { VisualLinesValid: true }) return;

        foreach (var vl in textView.VisualLines)
        {
            if (markerLines.TryGetValue(vl.FirstDocumentLine.LineNumber, out var tooltip))
            {
                var rect = new Rect(0, vl.VisualTop, Bounds.Width, vl.Height);

                if (rect.Contains(pos))
                {
                    ToolTip.SetTip(this, tooltip);
                    ToolTip.SetIsOpen(this, true);
                    return;
                }
            }
        }

        // mouse left marker → close tooltip
        ToolTip.SetIsOpen(this, false);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ToolTip.SetIsOpen(this, false);
    }

    protected override Size MeasureOverride(Size availableSize) => new(14, 0);
}
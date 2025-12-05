using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.Markers;

internal class MarkerMargin : AbstractMargin
{
    private const int WIDTH = 14;
    private static readonly IImage Bullet = ImageFactory.LoadFontIcon("fa-ban", WIDTH, Colors.Red);
    
    private readonly Dictionary<int, string> markers = [];

    public void AddMarker(int line, string tooltip)
    {
        markers[line] = tooltip;
        InvalidateVisual();
    }

    public void ClearMarkers()
    {
        markers.Clear();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var textView = TextView;
        if (textView is not { VisualLinesValid: true }) return;

        foreach (var vl in textView.VisualLines)
        {
            var line = vl.FirstDocumentLine.LineNumber;
            if (!markers.ContainsKey(line)) continue;
            
            var rect = new Rect(0, vl.VisualTop + (vl.Height - WIDTH) / 2, Bounds.Width, WIDTH);
            context.DrawImage(Bullet, rect);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var textView = TextView;
        if (textView is not { VisualLinesValid: true }) return;

        var pos = e.GetPosition(this);
        
        foreach (var vl in textView.VisualLines)
        {
            if (!markers.TryGetValue(vl.FirstDocumentLine.LineNumber, out var tooltip))
                continue;
            
            var rect = new Rect(0, vl.VisualTop, Bounds.Width, vl.Height);
            if (!rect.Contains(pos)) continue;
            
            ToolTip.SetTip(this, tooltip);
            ToolTip.SetIsOpen(this, true);
            return;
        }

        // mouse left marker → close tooltip
        ToolTip.SetIsOpen(this, false);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ToolTip.SetIsOpen(this, false);
    }

    protected override Size MeasureOverride(Size availableSize) => new (WIDTH, 0);
}
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.Markers;

internal class MarkerMargin : AbstractMargin
{
    private static readonly IImage Bullet = ImageFactory.LoadFontIcon("fa-bug", 14, Colors.Red);
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

            var rect = new Rect(0, vl.VisualTop - textView.VerticalOffset, Bounds.Width, vl.Height);
            var margin = new Thickness((Bullet.Size.Width - Bounds.Width) / 2,
                                       (Bullet.Size.Height - vl.Height) / 2);
            context.DrawImage(Bullet, rect.Inflate(margin));
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

            var rect = new Rect(0,  vl.VisualTop - textView.VerticalOffset, Bounds.Width, vl.Height);
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

    protected override Size MeasureOverride(Size availableSize) => Bullet.Size;
}
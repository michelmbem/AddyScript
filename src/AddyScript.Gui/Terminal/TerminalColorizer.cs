using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace AddyScript.Gui.Terminal;

internal class TerminalColorizer : DocumentColorizingTransformer
{
    public readonly List<ColoredSpan> Spans = [];

    private static bool IsOverlap(ColoredSpan span, DocumentLine line) =>
        (span.Offset >= line.Offset && span.EndOffset <= line.EndOffset) ||
        (span.Offset < line.Offset && span.EndOffset > line.EndOffset) ||
        (span.Offset < line.Offset && span.EndOffset > line.Offset && span.EndOffset <= line.EndOffset) ||
        (span.Offset > line.Offset && span.Offset < line.EndOffset && span.EndOffset > line.EndOffset);

    protected override void ColorizeLine(DocumentLine line)
    {
        Spans.Where(span => IsOverlap(span, line))
            .ToList()
            .ForEach(span =>
            {
                var start = Math.Max(span.Offset, line.Offset);
                var end = Math.Min(span.EndOffset, line.EndOffset);

                ChangeLinePart(start, end, element =>
                {
                    var props = element.TextRunProperties;
                    props.SetForegroundBrush(span.Foreground);
                    props.SetBackgroundBrush(span.Background);
                });
            });
    }
}
using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace AddyScript.Gui.Terminal;

public class TerminalColorizer : DocumentColorizingTransformer
{
    public readonly List<ColoredSpan> Spans = [];

    protected override void ColorizeLine(DocumentLine line)
    {
        int lineStart = line.Offset;

        foreach (var span in Spans)
        {
            if (span.StartOffset >= line.Offset + line.Length ||
                span.StartOffset + span.Length <= line.Offset) continue;

            int start = Math.Max(span.StartOffset, lineStart);
            int end = Math.Min(span.StartOffset + span.Length, lineStart + line.Length);

            ChangeLinePart(start, end, element =>
            {
                element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(span.Foreground));
                element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(span.Background));
            });
        }
    }
}
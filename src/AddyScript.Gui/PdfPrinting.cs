using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using AddyScript.Gui.Configuration;
using AddyScript.Gui.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Skia.Helpers;
using Avalonia.VisualTree;
using AvaloniaEdit;
using SkiaSharp;
using SR = AddyScript.Gui.Properties.Resources;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace AddyScript.Gui;

/// <summary>
/// Helper class for printing and/or exporting the content of a <see cref="TextEditor"/> to a PDF document.
/// </summary>
public class PdfPrinting
{
    private const double HEADER_FOOTER_FONT_SIZE = 10;
    private static readonly Vector StandardDpi = new (96, 96);

    private readonly Window window;
    private readonly Border border;
    private readonly TextEditor editor;
    private readonly TextBlock leftHeader;
    private readonly TextBlock rightHeader;
    private readonly TextBlock leftFooter;
    private readonly TextBlock rightFooter;

    /// <summary>
    /// Initializes a new instance of <see cref="PdfPrinting"/>.
    /// </summary>
    /// <param name="sourceEditor">The <see cref="TextEditor"/> whose content is to render as PDF</param>
    public PdfPrinting(TextEditor sourceEditor)
    {
        leftHeader = new TextBlock
        {
            FontSize = HEADER_FOOTER_FONT_SIZE,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        rightHeader = new TextBlock
        {
            FontSize = HEADER_FOOTER_FONT_SIZE,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };

        leftFooter = new TextBlock
        {
            FontSize = HEADER_FOOTER_FONT_SIZE,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
        };

        rightFooter = new TextBlock
        {
            FontSize = HEADER_FOOTER_FONT_SIZE,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };

        editor = new TextEditor
        {
            Foreground = Brushes.Black,
            Background = Brushes.White,
            FontFamily = sourceEditor.FontFamily,
            FontSize = sourceEditor.FontSize,
            ShowLineNumbers = sourceEditor.ShowLineNumbers,
            WordWrap = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            SyntaxHighlighting = sourceEditor.SyntaxHighlighting,
            Document = sourceEditor.Document,
            Options =
            {
                HighlightCurrentLine = false,
            },
            TextArea =
            {
                IndentationStrategy = sourceEditor.TextArea.IndentationStrategy,
                TextView =
                {
                    Margin = new Thickness(0.1 * StandardDpi.X, 0, 0, 0), // 0.25 cm on the left
                },
            }
        };

        var divider1 = new Rectangle
        {
            Fill = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 1,
        };

        var divider2 = new Rectangle
        {
            Fill = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 1,
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto, Auto, *, Auto, Auto"),
            ColumnDefinitions = new ColumnDefinitions("*, Auto"),
            RowSpacing = 0.08 * StandardDpi.Y, // about 2mm
            ColumnSpacing = 0.04 * StandardDpi.X, // about 1mm
        };

        grid.Children.Add(leftHeader);
        Grid.SetRow(leftHeader, 0);
        Grid.SetColumn(leftHeader, 0);

        grid.Children.Add(rightHeader);
        Grid.SetRow(rightHeader, 0);
        Grid.SetColumn(rightHeader, 1);

        grid.Children.Add(divider1);
        Grid.SetRow(divider1, 1);
        Grid.SetColumn(divider1, 0);
        Grid.SetColumnSpan(divider1, 2);

        grid.Children.Add(editor);
        Grid.SetRow(editor, 2);
        Grid.SetColumn(editor, 0);
        Grid.SetColumnSpan(editor, 2);

        grid.Children.Add(divider2);
        Grid.SetRow(divider2, 3);
        Grid.SetColumn(divider2, 0);
        Grid.SetColumnSpan(divider2, 2);

        grid.Children.Add(leftFooter);
        Grid.SetRow(leftFooter, 4);
        Grid.SetColumn(leftFooter, 0);

        grid.Children.Add(rightFooter);
        Grid.SetRow(rightFooter, 4);
        Grid.SetColumn(rightFooter, 1);

        var options = App.Options.PrintOptions ?? PrintOptions.Default;
        var effectivePageSize = options.PageFormat
                                       .PageSize
                                       .Rotate(options.Landscape)
                                       .Multiply(StandardDpi);
        
        border = new Border
        {
            Background = editor.Background,
            Width = effectivePageSize.Width,
            Height = effectivePageSize.Height,
            Padding = options.PageMargins.Multiply(StandardDpi),
            Child = grid,
        };

        // ReSharper disable once PossibleNullReferenceException
        var workingArea = App.ActiveWindow.Screens.Primary.WorkingArea;

        window = new Window
        {
            SystemDecorations = SystemDecorations.None,
            Position = workingArea.Position,
            Width = workingArea.Width,
            Height = workingArea.Height,
            CanResize = false,
            Content = new Viewbox
            {
                Stretch = Stretch.Uniform,
                Child = border,
            },
        };
    }

    /// <summary>
    /// Generates a PDF document from the source <see cref="TextEditor"/> and directly sends it to the printer.
    /// </summary>
    /// <param name="path">The path where to save the generated PDF document</param>
    /// <param name="title">The disired PDF document's title</param>
    /// <returns>A <see cref="Task"/></returns>
    public async Task PrintToPrinter(string path, string title = null)
    {
        await PrintToPdf(path, title);

        var psi = GetProcessStartInfo(path, App.Options.PrintOptions);
        var process = Process.Start(psi);
        if (process == null) return;

        await process.WaitForExitAsync();
    }

    /// <summary>
    /// Generates a PDF document from the source <see cref="TextEditor"/> and saves it to a file.
    /// </summary>
    /// <param name="path">The path where to save the generated PDF document</param>
    /// <param name="title">The disired PDF document's title</param>
    /// <returns>A <see cref="Task"/></returns>
    public async Task PrintToPdf(string path, string title = null)
    {
        leftHeader.Text = (title ?? path).Ellipsis(100, 0);
        rightHeader.Text = $"{AssemblyInfo.Title} v{AssemblyInfo.Version}";
        leftFooter.Text = string.Format(SR.PrintedOn, DateTime.Now);

        window.Pack();
        window.Show();

        var textView = editor.TextArea.TextView;
        var textLength = editor.Document.TextLength;
        var scrollViewer = textView.FindAncestorOfType<ScrollViewer>();
        var pageNumber = 1;

        try
        {
            using var document = SKDocument.CreatePdf(path);

            while (true)
            {
                rightFooter.Text = string.Format(SR.PageN, pageNumber);
                border.Repaint();
                await PrintPage(document, border);

                if (textView.VisualLines[^1].LastDocumentLine.EndOffset >= textLength)
                    break;

                // ReSharper disable once PossibleNullReferenceException
                scrollViewer.PageDown();
                ++pageNumber;
            }
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Renders a <see cref="Visual"/> as a page in PDF document as it appears on screen.
    /// </summary>
    /// <param name="document">The target PDF document</param>
    /// <param name="pageView">The <see cref="Visual"/> to render</param>
    /// <returns>A <see cref="Task"/></returns>
    private static async Task PrintPage(SKDocument document, Visual pageView)
    {
        var page = document.BeginPage((float)pageView.Bounds.Width, (float)pageView.Bounds.Height);
        await DrawingContextHelper.RenderAsync(page, pageView);
        document.EndPage();
    }
    
#if WINDOWS
    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> to invoke <em>ShellExecute</em> with arguments based on the user-defined print options.
    /// </summary>
    /// <param name="path">Path to the file to print</param>
    /// <param name="options">User-defined print options</param>
    /// <returns>A <see cref="ProcessStartInfo"/></returns>
    private static ProcessStartInfo GetProcessStartInfo(string path, PrintOptions options)
    {
        string printerName = options?.PrinterName;
        var psi = string.IsNullOrWhiteSpace(printerName)
                ? new ProcessStartInfo
                {
                    Verb = "Print",
                }
                : new ProcessStartInfo
                {
                    Verb = "PrintTo",
                    Arguments = printerName.EscapeAsCmdLineArg(),
                };

        psi.FileName = path;
        psi.UseShellExecute = true;
        psi.CreateNoWindow = true;
        psi.WindowStyle = ProcessWindowStyle.Hidden;

        return psi;
    }
#else
    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> to launch the <em>lp</em> command based on the user-defined print options.
    /// </summary>
    /// <param name="path">Path to the file to print</param>
    /// <param name="options">User-defined print options</param>
    /// <returns>A <see cref="ProcessStartInfo"/></returns>
    private static ProcessStartInfo GetProcessStartInfo(string path, PrintOptions options)
    {
        StringBuilder sb = new ();
        
        if (options != null)
        {
            if (!string.IsNullOrWhiteSpace(options.PrinterName)) sb.Append($"-d {options.PrinterName} ");
            sb.Append($"-o media={options.PageFormat.Name} -o orientation-requested=");
            sb.Append(options.Landscape ? '4' : '3').Append(' ');
        }
        
        sb.Append("-o fit-to-page ").Append(path.EscapeAsCmdLineArg());

        return new ProcessStartInfo
        {
            FileName = "lp",
            Arguments = sb.ToString(),
        };
    }
#endif
}
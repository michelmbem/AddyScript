using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media;

namespace AddyScript.Gui.Configuration;

#region JSON Converters

public class FontFamilyConverter : JsonConverter<FontFamily>
{
    public override FontFamily Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        FontFamily.Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, FontFamily value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Color.Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

public class PageFormatConverter : JsonConverter<PageFormat>
{
    public override PageFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string name = reader.GetString();
        return PageFormat.Known.FirstOrDefault(pageFormat => pageFormat.Name == name);
    }

    public override void Write(Utf8JsonWriter writer, PageFormat value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Name);
}

public class ThicknessConverter : JsonConverter<Thickness>
{
    public override Thickness Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Thickness.Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, Thickness value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

public class CultureInfoConverter : JsonConverter<CultureInfo>
{
    public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new (reader.GetString());

    public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Name);
}

#endregion

#region Sections

public abstract class EditorOptionsBase
{
    [JsonConverter(typeof(FontFamilyConverter))]
    public FontFamily FontFamily { get; set; }

    public double FontSize { get; set; }

    public bool WordWrap { get; set; }
}

public class EditorOptions : EditorOptionsBase
{
    public bool ShowLineNumbers { get; set; }

    public bool ShowWhitespace { get; set; }

    public bool HighlightCurrentLine { get; set; }

    public EditorOptions Clone() => new()
    {
        FontFamily = FontFamily,
        FontSize = FontSize,
        WordWrap = WordWrap,
        ShowLineNumbers = ShowLineNumbers,
        ShowWhitespace = ShowWhitespace,
        HighlightCurrentLine = HighlightCurrentLine,
    };
}

public class TerminalOptions : EditorOptionsBase
{
    [JsonConverter(typeof(ColorConverter))]
    public Color Background { get; set; }

    [JsonConverter(typeof(ColorConverter))]
    public Color Foreground { get; set; }

    public TerminalOptions Clone() => new()
    {
        Background = Background,
        Foreground = Foreground,
        FontFamily = FontFamily,
        FontSize = FontSize,
        WordWrap = WordWrap,
    };
}

public record PageFormat(string Name, Size PageSize)
{
    public static readonly PageFormat[] Known = [
        new ("Letter", new Size(8.5, 11)),
        new ("A4", new Size(8.27, 11.69)),
        new ("B4", new Size(9.84, 13.90)),
        new ("A3", new Size(11.69, 16.54)),
        new ("B3", new Size(13.90, 19.69)),
    ];

    public static PageFormat Default => Known[0]; // US Letter by default

    public override string ToString() => $"{Name} ({PageSize.Width}\" x {PageSize.Height}\")";
}

public class PrintOptions
{
    public static readonly PrintOptions Default = new ()
    {
        PageFormat = PageFormat.Default,
        PageMargins = new Thickness(0.4), // About 1 cm on each side by default
        Landscape = false,
    };

    public string PrinterName { get; set; }

    [JsonConverter(typeof(PageFormatConverter))]
    public PageFormat PageFormat { get; set; }

    public bool Landscape { get; set; }

    [JsonConverter(typeof(ThicknessConverter))]
    public Thickness PageMargins { get; set; }

    public PrintOptions Clone() => new()
    {
        PrinterName = PrinterName,
        PageFormat = PageFormat,
        Landscape =  Landscape,
        PageMargins = PageMargins
    };
}

#endregion

public class Options
{
    public static readonly Options Default = new ()
    {
        UseEmulatedTerminal = true,
        SearchPaths = [
            Path.GetFullPath(@"../../../samples/library")
        ],
        References = [
            "Microsoft.Data.SqlClient"
        ],
    };

    [JsonConverter(typeof(CultureInfoConverter))]
    public CultureInfo Culture { get; set; }

    public EditorOptions Editor { get; set; }

    public bool UseEmulatedTerminal { get; set; }

    public TerminalOptions Terminal { get; set; }

    public PrintOptions PrintOptions { get; set; }

    public List<string> SearchPaths { get; set; }

    public List<string> References { get; set; }

    public Options Clone() => new()
    {
        Culture = Culture,
        Editor = Editor?.Clone(),
        UseEmulatedTerminal = UseEmulatedTerminal,
        Terminal = Terminal?.Clone(),
        PrintOptions = PrintOptions?.Clone(),
        SearchPaths = [.. SearchPaths],
        References = [.. References],
    };
}
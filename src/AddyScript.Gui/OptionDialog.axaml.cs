using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
#if WINDOWS
using System.Drawing.Printing;
#else
using System.Diagnostics;
#endif
using AddyScript.Gui.Configuration;
using AddyScript.Gui.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using AvaloniaEdit.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MBI = MsBox.Avalonia.Enums.Icon;
using SR = AddyScript.Gui.Properties.Resources;

namespace AddyScript.Gui;

#region Record types

public record Language(CultureInfo Culture, string Name)
{
    public Language(CultureInfo ci) :
        this(ci, $"{ci.NativeName.Capitalize()} ({ci.TwoLetterISOLanguageName})") {}
    
    public override string ToString() => Name;
}

public record Printer(string Name, string DisplayName)
{
    public Printer(string name) : this(name, name) {}
    
    public override string ToString() => DisplayName;
}

#endregion

public partial class OptionDialog : Window
{
    #region Fields
    
    private const string MARGIN_FORMAT = "0.#";
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;
    
    private readonly ObservableCollection<string> searchPaths = [];
    private readonly ObservableCollection<string> references = [];

    private Options options;
    private Border colorPreview;
    private TextBlock colorName;

    #endregion

    #region Constructor
    
    public OptionDialog()
    {
        InitializeComponent();

        LanguageComboBox.ItemsSource = GetLanguageList();
        LanguageComboBox.SelectedIndex = 0;
        PrinterNameComboBox.ItemsSource = GetPrinterList();
        PageFormatComboBox.ItemsSource = PageFormat.Known;
        SearchPathsListBox.ItemsSource = searchPaths;
        ReferencesListBox.ItemsSource = references;
    }
    
    #endregion

    #region Properties

    private CultureInfo SelectedCulture
    {
        get => (LanguageComboBox.SelectedItem as Language)?.Culture;
        set => LanguageComboBox.SelectedItem = LanguageComboBox
                                               .ItemsSource?
                                               .OfType<Language>()
                                               .FirstOrDefault(lang => Equals(lang.Culture, value));
    }

    private string SelectedPrinterName
    {
        get => (PrinterNameComboBox.SelectedItem as Printer)?.Name;
        set => PrinterNameComboBox.SelectedItem = PrinterNameComboBox
                                                  .ItemsSource?
                                                  .OfType<Printer>()
                                                  .FirstOrDefault(lang => Equals(lang.Name, value));
    }

    public Options Options
    {
        get => options;
        set
        {
            options = value;

            SelectedCulture = options.Culture;

            if (options.Editor != null)
            {
                SetEditorFontFace(options.Editor.FontFamily);
                SetEditorFontSize(options.Editor.FontSize);
                EditorWordWrapCheckBox.IsChecked = options.Editor.WordWrap;
                EditorShowLineNumbersCheckBox.IsChecked = options.Editor.ShowLineNumbers;
                EditorShowWhitespaceCheckBox.IsChecked = options.Editor.ShowWhitespace;
                EditorHighlightCurrentLineCheckBox.IsChecked = options.Editor.HighlightCurrentLine;
            }

            if (options.UseEmulatedTerminal)
                EmulatedTerminalRadioButton.IsChecked = true;
            else
                NativeTerminalRadioButton.IsChecked = true;

            if (options.Terminal != null)
            {
                SetTerminalFontFace(options.Terminal.FontFamily);
                SetTerminalFontSize(options.Terminal.FontSize);
                SetTerminalForegroundColor(options.Terminal.Foreground);
                SetTerminalBackgroundColor(options.Terminal.Background);
            }

            if (options.PrintOptions != null)
            {
                SelectedPrinterName = options.PrintOptions.PrinterName;
                PageFormatComboBox.SelectedItem = options.PrintOptions.PageFormat;
                SetPageMargins(options.PrintOptions.PageMargins);

                if (options.PrintOptions.Landscape)
                    LandscapeRadioButton.IsChecked = true;
                else
                    PortraitRadioButton.IsChecked = true;
            }

            searchPaths.Clear();
            searchPaths.AddRange(options.SearchPaths);

            references.Clear();
            references.AddRange(options.References);
        }
    }
    
    #endregion

    #region Utility methods

    private static IEnumerable<Language> GetLanguageList() => [
        new (null, SR.SystemDefault),
        ..CultureInfo.GetCultures(CultureTypes.NeutralCultures)
                     .Select(ci => new Language(ci))
                     .OrderBy(lang => lang.Name)
    ];

    private static IEnumerable<Printer> GetPrinterList()
    {
#if WINDOWS
        return PrinterSettings.InstalledPrinters.Select(name => new Printer(name));
#else
        var psi = new ProcessStartInfo
        {
            FileName = "lpstat",
            Arguments = "-e",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var lpstat = Process.Start(psi);
        if (lpstat == null) return [];

        lpstat.WaitForExit();

        return lpstat.StandardOutput
                     .ReadToEnd()
                     .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                     .Select(line => new Printer(line, line.Replace('_', ' ').Trim()));
#endif
    }

    private void SetEditorFontFace(FontFamily editorFontFamily)
    {
        EditorFontFaceText.Text = editorFontFamily.Name;
        EditorFontFaceText.FontFamily = editorFontFamily;
    }

    private void SetEditorFontSize(double editorFontSize)
    {
        EditorFontSizeSpinner.Content = EditorFontFaceText.FontSize = editorFontSize;
    }

    private void SetTerminalFontFace(FontFamily terminalFontFamily)
    {
        TerminalFontFaceText.Text = terminalFontFamily.Name;
        TerminalFontFaceText.FontFamily = terminalFontFamily;
    }

    private void SetTerminalFontSize(double terminalFontSize)
    {
        TerminalFontSizeSpinner.Content = TerminalFontFaceText.FontSize = terminalFontSize;
    }

    private void SetTerminalForegroundColor(Color color)
    {
        TerminalForegroundBorder.Background = new SolidColorBrush(color);
        TerminalForegroundText.Text = color.ToString();
        TerminalForegroundText.Foreground = color.IsDark() ? Brushes.White : Brushes.Black;
    }

    private void SetTerminalBackgroundColor(Color color)
    {
        TerminalBackgroundBorder.Background = new SolidColorBrush(color);
        TerminalBackgroundText.Text = color.ToString();
        TerminalBackgroundText.Foreground = color.IsDark() ? Brushes.White : Brushes.Black;
    }

    private void SetPageMargins(Thickness pageMargins)
    {
        TopMarginSpinner.Content = pageMargins.Top.ToString(MARGIN_FORMAT, CI);
        LeftMarginSpinner.Content = pageMargins.Left.ToString(MARGIN_FORMAT, CI);
        RightMarginSpinner.Content = pageMargins.Right.ToString(MARGIN_FORMAT, CI);
        BottomMarginSpinner.Content = pageMargins.Bottom.ToString(MARGIN_FORMAT, CI);
    }
    
    #endregion

    #region Event handlers

    private void TerminalColorViewColorChanged(object sender, ColorChangedEventArgs e)
    {
        colorPreview.Background = new SolidColorBrush(e.NewColor);
        colorName.Text = e.NewColor.ToString();
        colorName.Foreground = e.NewColor.IsDark() ? Brushes.White : Brushes.Black;
    }

    private async void EditorFontFaceButtonClick(object sender, RoutedEventArgs e)
    {
        var fontDialog = new FontDialog { SelectedFontFamily = EditorFontFaceText.FontFamily };
        if (!await fontDialog.ShowDialog<bool>(this)) return;

        EditorFontFaceText.FontFamily = fontDialog.SelectedFontFamily;
        EditorFontFaceText.Text = fontDialog.SelectedFontFamily?.Name;
    }

    private void EditorFontSizeSpinnerSpin(object sender, SpinEventArgs e)
    {
        var fontSize = Convert.ToInt32(EditorFontSizeSpinner.Content);

        switch (e.Direction)
        {
            case SpinDirection.Increase:
                ++fontSize;
                break;
            case SpinDirection.Decrease:
                if (fontSize <= 6) return;
                --fontSize;
                break;
        }

        EditorFontSizeSpinner.Content = fontSize;
        EditorFontFaceText.FontSize = fontSize;
    }

    private void TerminalForegroundButtonClick(object sender, RoutedEventArgs e)
    {
        colorPreview = TerminalForegroundBorder;
        colorName = TerminalForegroundText;
        TerminalColorView.Color = ((dynamic)colorPreview.Background)?.Color;
        ColorPopup.PlacementTarget = TerminalForegroundButton;
        ColorPopup.IsOpen = true;
    }

    private void TerminalBackgroundButtonClick(object sender, RoutedEventArgs e)
    {
        colorPreview = TerminalBackgroundBorder;
        colorName = TerminalBackgroundText;
        TerminalColorView.Color = ((dynamic)colorPreview.Background)?.Color;
        ColorPopup.PlacementTarget = TerminalBackgroundButton;
        ColorPopup.IsOpen = true;
    }

    private async void TerminalFontFaceButtonClick(object sender, RoutedEventArgs e)
    {
        var fontDialog = new FontDialog { SelectedFontFamily = TerminalFontFaceText.FontFamily };
        if (!await fontDialog.ShowDialog<bool>(this)) return;

        TerminalFontFaceText.FontFamily = fontDialog.SelectedFontFamily;
        TerminalFontFaceText.Text = fontDialog.SelectedFontFamily?.Name;
    }

    private void TerminalFontSizeSpinnerSpin(object sender, SpinEventArgs e)
    {
        var fontSize = Convert.ToInt32(TerminalFontSizeSpinner.Content);

        switch (e.Direction)
        {
            case SpinDirection.Increase:
                ++fontSize;
                break;
            case SpinDirection.Decrease:
                if (fontSize <= 6) return;
                --fontSize;
                break;
        }

        TerminalFontSizeSpinner.Content = fontSize;
        TerminalFontFaceText.FontSize = fontSize;
    }

    private void MarginSpinnerSpin(object sender, SpinEventArgs e)
    {
        if (sender is not ButtonSpinner spinner) return;

        var margin = Convert.ToDouble(spinner.Content, CI);

        switch (e.Direction)
        {
            case SpinDirection.Increase:
                margin += 0.1;
                break;
            case SpinDirection.Decrease:
                if (margin <= 0) return;
                margin -= 0.1;
                break;
        }

        spinner.Content = margin.ToString(MARGIN_FORMAT, CI);
    }

    private async void BrowseDirectoryButtonClick(object sender, RoutedEventArgs e)
    {
        var directories = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = SR.DirectoryChooserTitle,
                AllowMultiple = false,
            });

        if (directories.Count > 0)
            NewDirectoryTextBox.Text = directories[0].Path.LocalPath;
    }

    private void AddDirectoryButtonClick(object sender, RoutedEventArgs e)
    {
        var newDirectory = NewDirectoryTextBox.Text;

        if (string.IsNullOrWhiteSpace(newDirectory) || searchPaths.Contains(newDirectory))
            return;

        if (Directory.Exists(newDirectory))
        {
            searchPaths.Add(newDirectory);
            NewDirectoryTextBox.Clear();
        }
        else
        {
            MessageBoxManager.GetMessageBoxStandard(
                    SR.ErrorMessageTitle,
                    string.Format(SR.DirectoryNotFound, newDirectory),
                    ButtonEnum.Ok,
                    MBI.Error)
                .ShowAsync();
        }
    }

    private async void RemoveDirectoryButtonClick(object sender, RoutedEventArgs e)
    {
        var item = (sender as Visual)?.FindAncestorOfType<ListBoxItem>();
        if (item == null) return;

        var ans = await MessageBoxManager.GetMessageBoxStandard(
                SR.ConfirmationBoxTitle,
                SR.ConfirmDelete,
                ButtonEnum.YesNo,
                MBI.Error)
            .ShowAsync();

        if (ans == ButtonResult.Yes)
            searchPaths.Remove(item.DataContext?.ToString());
    }

    private async void BrowseAssemblyButtonClick(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = SR.AssemblyChooserTitle,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(SR.AssemblyChooserFilter) { Patterns = ["*.dll"] }
                ]
            });

        if (files.Count > 0)
            NewReferenceTextBox.Text = files[0].Path.LocalPath;
    }

    private void AddReferenceButtonClick(object sender, RoutedEventArgs e)
    {
        var newReference = NewReferenceTextBox.Text;

        if (string.IsNullOrWhiteSpace(newReference) || references.Contains(newReference))
            return;

        if (ScriptContext.LoadAssembly(newReference) != null)
        {
            references.Add(newReference);
            NewReferenceTextBox.Clear();
        }
        else
        {
            MessageBoxManager.GetMessageBoxStandard(
                    SR.AssemblyLoadFailureTitle,
                    string.Format(SR.AssemblyLoadFailureMessage, newReference),
                    ButtonEnum.Ok,
                    MBI.Error)
                .ShowAsync();
        }
    }

    private async void RemoveReferenceButtonClick(object sender, RoutedEventArgs e)
    {
        var item = (sender as Visual)?.FindAncestorOfType<ListBoxItem>();
        if (item == null) return;

        var ans = await MessageBoxManager.GetMessageBoxStandard(
                SR.ConfirmationBoxTitle,
                SR.ConfirmDelete,
                ButtonEnum.YesNo,
                MBI.Error)
            .ShowAsync();

        if (ans == ButtonResult.Yes)
            references.Remove(item.DataContext?.ToString());
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        options = new Options
        {
            Culture = SelectedCulture,
            Editor = new EditorOptions
            {
                FontFamily = EditorFontFaceText.FontFamily,
                FontSize = EditorFontFaceText.FontSize,
                WordWrap = EditorWordWrapCheckBox.IsChecked ?? false,
                ShowLineNumbers = EditorShowLineNumbersCheckBox.IsChecked ?? false,
                ShowWhitespace = EditorShowWhitespaceCheckBox.IsChecked ?? false,
                HighlightCurrentLine = EditorHighlightCurrentLineCheckBox.IsChecked ?? false,
            },
            UseEmulatedTerminal = EmulatedTerminalRadioButton.IsChecked ?? false,
            Terminal = new TerminalOptions
            {
                Foreground = ((dynamic)TerminalForegroundBorder.Background)?.Color,
                Background = ((dynamic)TerminalBackgroundBorder.Background)?.Color,
                FontFamily = TerminalFontFaceText.FontFamily,
                FontSize = TerminalFontFaceText.FontSize,
                WordWrap = TerminalWordWrapCheckBox.IsChecked ?? false,
            },
            PrintOptions = new PrintOptions
            {
                PrinterName = SelectedPrinterName,
                PageFormat = (PageFormat)PageFormatComboBox.SelectionBoxItem,
                Landscape = LandscapeRadioButton.IsChecked ?? false,
                PageMargins = new Thickness(Convert.ToDouble(LeftMarginSpinner.Content, CI),
                                            Convert.ToDouble(TopMarginSpinner.Content, CI),
                                            Convert.ToDouble(RightMarginSpinner.Content, CI),
                                            Convert.ToDouble(BottomMarginSpinner.Content, CI))
            },
            SearchPaths = [.. searchPaths],
            References = [.. references],
        };

        Close(true);
    }

    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
        if (ColorPopup.IsOpen)
            ColorPopup.IsOpen = false;
        else
            Close(false);
    }
    
    #endregion
}
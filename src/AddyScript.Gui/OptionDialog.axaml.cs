using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SR = AddyScript.Gui.Properties.Resources;
using MBI = MsBox.Avalonia.Enums.Icon;

namespace AddyScript.Gui;

public partial class OptionDialog : Window
{
    private readonly ObservableCollection<string> searchPaths = [];
    private readonly ObservableCollection<string> references = [];
    
    private Options options;
    private Border colorPreview;
    private TextBlock colorName;
    
    public OptionDialog()
    {
        InitializeComponent();

        Title = SR.OptionDialogTitle;
        
        SearchPathsListBox.ItemsSource = searchPaths;
        ReferencesListBox.ItemsSource = references;
    }

    public Options Options
    {
        get => options;
        set
        {
            options = value;
            
            searchPaths.Clear();
            searchPaths.AddRange(options.SearchPaths);
            
            references.Clear();
            references.AddRange(options.References);

            if (options.Editor != null)
            {
                SetEditorFontFace(options.Editor.FontFamily);
                SetEditorFontSize(options.Editor.FontSize);
            }

            if (options.Terminal != null)
            {
                SetTerminalFontFace(options.Terminal.FontFamily);
                SetTerminalFontSize(options.Terminal.FontSize);
                SetTerminalForegroundColor(options.Terminal.Foreground);
                SetTerminalBackgroundColor(options.Terminal.Background);
            }

            if (options.UseEmulatedTerminal)
                EmulatedTerminalRadioButton.IsChecked = true;
            else
                NativeTerminalRadioButton.IsChecked = true;
        }
    }

    private static double Luminance(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;

    private void SetEditorFontFace(FontFamily editorFontFamily)
    {
        EditorFontFaceText.Text = editorFontFamily.Name;
        EditorFontFaceText.FontFamily = editorFontFamily;
    }

    private void SetEditorFontSize(double editorFontSize)
    {
        EditorFontSizeSpinner.Content =  EditorFontFaceText.FontSize = editorFontSize;
    }

    private void SetTerminalFontFace(FontFamily terminalFontFamily)
    {
        TerminalFontFaceText.Text = terminalFontFamily.Name;
        TerminalFontFaceText.FontFamily = terminalFontFamily;
    }

    private void SetTerminalFontSize(double terminalFontSize)
    {
        TerminalFontSizeSpinner.Content =  TerminalFontFaceText.FontSize = terminalFontSize;
    }

    private void SetTerminalForegroundColor(Color color)
    {
        TerminalForegroundBorder.Background = new SolidColorBrush(color);
        TerminalForegroundText.Text = color.ToString();
        TerminalForegroundText.Foreground = Luminance(color) < 0.5 ? Brushes.White : Brushes.Black;
    }

    private void SetTerminalBackgroundColor(Color color)
    {
        TerminalBackgroundBorder.Background = new SolidColorBrush(color);
        TerminalBackgroundText.Text = color.ToString();
        TerminalBackgroundText.Foreground = Luminance(color) < 0.5 ? Brushes.White : Brushes.Black;
    }

    private void TerminalColorViewColorChanged(object sender, ColorChangedEventArgs e)
    {
        colorPreview.Background = new SolidColorBrush(e.NewColor);
        colorName.Text = e.NewColor.ToString();
        colorName.Foreground = Luminance(e.NewColor) < 0.5 ? Brushes.White : Brushes.Black;
    }

    private async void EditorFontFaceButtonClick(object sender, RoutedEventArgs e)
    {
        var fontDialog = new FontDialog();
        if (!await fontDialog.ShowDialog<bool>(this)) return;
        
        EditorFontFaceText.FontFamily = fontDialog.SelectedFontFamily;
        EditorFontFaceText.Text = fontDialog.SelectedFontFamily.Name;
    }

    private void EditorFontSizeSpinnerSpin(object sender, SpinEventArgs e)
    {
        var fontSize = int.Parse("" + EditorFontSizeSpinner.Content);
        
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
        var fontDialog = new FontDialog();
        if (!await fontDialog.ShowDialog<bool>(this)) return;
        
        TerminalFontFaceText.FontFamily = fontDialog.SelectedFontFamily;
        TerminalFontFaceText.Text = fontDialog.SelectedFontFamily.Name;
    }

    private void TerminalFontSizeSpinnerSpin(object sender, SpinEventArgs e)
    {
        var fontSize = int.Parse("" + TerminalFontSizeSpinner.Content);
        
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
        if (SearchPathsListBox.SelectedIndex < 0) return;
        
        var ans = await MessageBoxManager.GetMessageBoxStandard(
                SR.ConfirmationBoxTitle,
                SR.ConfirmationBoxMessage,
                ButtonEnum.YesNo,
                MBI.Error)
            .ShowAsync();
        
        if (ans == ButtonResult.Yes)
            searchPaths.RemoveAt(SearchPathsListBox.SelectedIndex);
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
        if (ReferencesListBox.SelectedIndex < 0) return;
        
        var ans = await MessageBoxManager.GetMessageBoxStandard(
                SR.ConfirmationBoxTitle,
                SR.ConfirmationBoxMessage,
                ButtonEnum.YesNo,
                MBI.Error)
            .ShowAsync();
        
        if (ans == ButtonResult.Yes)
            references.RemoveAt(ReferencesListBox.SelectedIndex);
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        options = new Options
        {
            SearchPaths = [..searchPaths],
            References = [..references],
            Editor = new EditorOptions
            {
                FontFamily = EditorFontFaceText.FontFamily,
                FontSize = EditorFontFaceText.FontSize,
                WordWrap = EditorWordWrapCheckBox.IsChecked ?? false,
            },
            Terminal = new TerminalOptions
            {
                Foreground = ((dynamic)TerminalForegroundBorder.Background)?.Color,
                Background = ((dynamic)TerminalBackgroundBorder.Background)?.Color,
                FontFamily = TerminalFontFaceText.FontFamily,
                FontSize = TerminalFontFaceText.FontSize,
                WordWrap = TerminalWordWrapCheckBox.IsChecked ?? false,   
            },
            UseEmulatedTerminal = EmulatedTerminalRadioButton.IsChecked ?? false,
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
}
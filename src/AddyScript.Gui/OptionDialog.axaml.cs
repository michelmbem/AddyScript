using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using StringRes = AddyScript.Gui.Properties.Resources;
using MBIcon = MsBox.Avalonia.Enums.Icon;

namespace AddyScript.Gui;

public partial class OptionDialog : Window
{
    public ObservableCollection<string> SearchPaths { get; } = [..App.SearchPaths];
    public ObservableCollection<string> References { get; } = [.. App.References];

    public OptionDialog()
    {
        InitializeComponent();

        SearchPathsListBox.ItemsSource = SearchPaths;
        ReferencesListBox.ItemsSource = References;
    }

    private async void BrowseDirectoryButtonClick(object sender, RoutedEventArgs e)
    {
        var directories = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select a folder",
            AllowMultiple = false,
        });

        if (directories.Count > 0)
            NewDirectoryTextBox.Text = directories[0].Path.LocalPath;
    }

    private void AddDirectoryButtonClick(object sender, RoutedEventArgs e)
    {
        var newDirectory = NewDirectoryTextBox.Text;
        
        if (string.IsNullOrWhiteSpace(newDirectory) ||
            SearchPaths.Contains(newDirectory)) return;
        
        if (Directory.Exists(newDirectory))
            SearchPaths.Add(newDirectory);
        else
            MessageBoxManager
                .GetMessageBoxStandard(StringRes.ErrorMessageTitle,$"Directory '{newDirectory}' does not exist", ButtonEnum.Ok, MBIcon.Error)
                .ShowAsync();
    }

    private void RemoveDirectoryButtonClick(object sender, RoutedEventArgs e)
    {
        if (SearchPathsListBox.SelectedIndex >= 0)
            SearchPaths.RemoveAt(SearchPathsListBox.SelectedIndex);
    }

    private async void BrowseAssemblyButtonClick(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a .Net assembly",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType(".Net assembly (*.dll)") { Patterns = ["*.dll"] }
            ]
        });

        if (files.Count > 0)
            NewReferenceTextBox.Text = files[0].Path.LocalPath;
    }

    private void AddReferenceButtonClick(object sender, RoutedEventArgs e)
    {
        var newReference = NewReferenceTextBox.Text;
        
        if (string.IsNullOrWhiteSpace(newReference) ||
            References.Contains(newReference)) return;
        
        if (ScriptContext.LoadAssembly(newReference) != null)
            References.Add(newReference);
        else
            MessageBoxManager
                .GetMessageBoxStandard(StringRes.ErrorMessageTitle,$"Could not load assembly '{newReference}'", ButtonEnum.Ok, MBIcon.Error)
                .ShowAsync();
    }

    private void RemoveReferenceButtonClick(object sender, RoutedEventArgs e)
    {
        if (ReferencesListBox.SelectedIndex >= 0)
            References.RemoveAt(ReferencesListBox.SelectedIndex);
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
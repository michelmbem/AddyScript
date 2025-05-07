using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AddyScript.Gui;

public partial class OptionDialog : Window
{
    private readonly ObservableCollection<string> searchPaths = [..App.Directories];
    private readonly ObservableCollection<string> references = [.. App.Assemblies];

    public OptionDialog()
    {
        InitializeComponent();

        SearchPathsListBox.ItemsSource = searchPaths;
        ReferencesListBox.ItemsSource = references;
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
        searchPaths.Add(NewDirectoryTextBox.Text);
    }

    private void AddReferenceButtonClick(object sender, RoutedEventArgs e)
    {
        references.Add(NewReferenceTextBox.Text);
    }

    private void RemoveDirectoryButtonClick(object sender, RoutedEventArgs e)
    {
        if (SearchPathsListBox.SelectedIndex >= 0)
            searchPaths.RemoveAt(SearchPathsListBox.SelectedIndex);
    }

    private void RemoveReferenceButtonClick(object sender, RoutedEventArgs e)
    {
        if (ReferencesListBox.SelectedIndex >= 0)
            references.RemoveAt(ReferencesListBox.SelectedIndex);
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
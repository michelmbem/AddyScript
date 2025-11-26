using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SR = AddyScript.Gui.Properties.Resources;
using MBI = MsBox.Avalonia.Enums.Icon;

namespace AddyScript.Gui;

public partial class OptionDialog : Window
{
    public ObservableCollection<string> SearchPaths { get; } = [..App.SearchPaths];
    public ObservableCollection<string> References { get; } = [.. App.References];

    public OptionDialog()
    {
        InitializeComponent();

        Title = SR.OptionDialogTitle;

        SearchPathsListBox.ItemsSource = SearchPaths;
        ReferencesListBox.ItemsSource = References;
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

        if (string.IsNullOrWhiteSpace(newDirectory) || SearchPaths.Contains(newDirectory))
            return;

        if (Directory.Exists(newDirectory))
        {
            SearchPaths.Add(newDirectory);
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
            SearchPaths.RemoveAt(SearchPathsListBox.SelectedIndex);
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

        if (string.IsNullOrWhiteSpace(newReference) || References.Contains(newReference))
            return;

        if (ScriptContext.LoadAssembly(newReference) != null)
        {
            References.Add(newReference);
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
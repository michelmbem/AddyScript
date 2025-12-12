using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AddyScript.Gui;

public partial class FontDialog : Window
{
    private readonly ObservableCollection<FontFamily> fontFamilies = [];
    private ObservableCollection<FontFamily> filteredFontFamilies;

    public FontDialog()
    {
        InitializeComponent();
        
        foreach (var font in FontManager.Current.SystemFonts.OrderBy(f => f.Name))
        {
            fontFamilies.Add(font);
        }

        FilterTextBoxTextChanged(null, null);
    }

    public FontFamily SelectedFontFamily
    {
        get => (FontFamily)FontListBox.SelectedItem;
        set => FontListBox.SelectedItem = value;
    }

    private void FilterTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        var filterText = FilterTextBox.Text ?? string.Empty;
        filteredFontFamilies = string.IsNullOrWhiteSpace(filterText)
            ? [..fontFamilies]
            : new ObservableCollection<FontFamily>(fontFamilies.Where(f =>
                f.Name.Contains(filterText, StringComparison.CurrentCultureIgnoreCase)));
        
        FontListBox.ItemsSource = filteredFontFamilies;
        FontListBox.SelectedItem = filteredFontFamilies.FirstOrDefault();
        OkButton.IsEnabled = filteredFontFamilies.Count > 0;
    }

    private void ClearFilterButtonClick(object sender, RoutedEventArgs e)
    {
        FilterTextBox.Text = string.Empty;
    }

    private void FontListItemDoubleTapped(object sender, TappedEventArgs e)
    {
        Close(true);
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
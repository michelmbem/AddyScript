using System;
using System.Collections.Generic;
using System.Linq;
using AddyScript.Gui.Extensions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AddyScript.Gui;

public partial class FontDialog : Window
{
    private const int PAGE_SIZE = 7;
    private readonly List<FontFamily> fontFamilies;
    private List<FontFamily> filteredFontFamilies;

    public FontDialog()
    {
        InitializeComponent();
        
        fontFamilies = [.. FontManager.Current.SystemFonts.OrderBy(f => f.Name)];
        
        FilterFontList(null, false);
    }

    public FontFamily SelectedFontFamily
    {
        get => (FontFamily)FontListBox.SelectedItem;
        set
        {
            FontListBox.SelectedItem = value;
            FontListBox.ScrollIntoView(value);
        }
    }

    private void FilterFontList(string filterText, bool monospacedOnly)
    {
        var matchingFontFamilies = string.IsNullOrWhiteSpace(filterText)
            ? fontFamilies
            : fontFamilies.Where(f => f.Name.Contains(filterText, StringComparison.CurrentCultureIgnoreCase));

        if (monospacedOnly)
            matchingFontFamilies = matchingFontFamilies.Where(f => f.IsMonospaced());

        filteredFontFamilies = [.. matchingFontFamilies];
        FontListBox.ItemsSource = filteredFontFamilies;
        FontListBox.SelectedItem = filteredFontFamilies.FirstOrDefault();
        OkButton.IsEnabled = filteredFontFamilies.Count > 0;
    }

    private void WindowKeyUp(object sender, KeyEventArgs e)
    {
        int selectedIndex = FontListBox.SelectedIndex;

        switch (e.Key)
        {
            case Key.Home:
                selectedIndex = 0;
                break;
            case Key.End:
                selectedIndex = filteredFontFamilies.Count - 1;
                break;
            case Key.Up:
                if (selectedIndex > 0)
                    --selectedIndex;
                break;
            case Key.Down:
                if (selectedIndex < filteredFontFamilies.Count - 1)
                    ++selectedIndex;
                break;
            case Key.PageUp:
                if (selectedIndex > PAGE_SIZE)
                    selectedIndex -= PAGE_SIZE;
                break;
            case Key.PageDown:
                if (selectedIndex + PAGE_SIZE < filteredFontFamilies.Count - 1)
                    selectedIndex += PAGE_SIZE;
                break;
            default:
                return;
        }

        FontListBox.SelectedIndex = selectedIndex;
        FontListBox.ScrollIntoView(selectedIndex);
    }

    private void FilterTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterFontList(FilterTextBox.Text, MonospaceFontCheckBox.IsChecked ?? false);
    }

    private void MonospaceFontCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    {
        FilterFontList(FilterTextBox.Text, MonospaceFontCheckBox.IsChecked ?? false);
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
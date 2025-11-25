using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AddyScript.Gui;

public partial class AboutBox : Window
{
    public AboutBox()
    {
        InitializeComponent();
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
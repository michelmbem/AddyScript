using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AddyScript.Gui;

public partial class AboutBox : Window
{
    public AboutBox()
    {
        InitializeComponent();
        
        TitleTextBlock.Text = AssemblyInfo.Title;
        VersionTextBlock.Text = $"Version {AssemblyInfo.Version}";
        DescriptionTextBlock.Text = AssemblyInfo.Description;
        CopyrightTextBlock.Text = AssemblyInfo.Copyright;
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
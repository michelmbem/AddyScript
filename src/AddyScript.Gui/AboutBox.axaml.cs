using Avalonia.Controls;
using Avalonia.Interactivity;

using AI = AddyScript.Gui.AssemblyInfo;


namespace AddyScript.Gui;


public partial class AboutBox : Window
{
    public AboutBox()
    {
        InitializeComponent();
        
        Title = $"About {AI.Title}";

        VersionTextBlock.Text = $"Version {AI.Version}";
        DescriptionTextBlock.Text = AI.Description;
        CopyrightTextBlock.Text = AI.Copyright;
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
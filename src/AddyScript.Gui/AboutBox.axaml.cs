using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AI = AddyScript.Gui.AssemblyInfo;


namespace AddyScript.Gui;


public partial class AboutBox : Window
{
    private const string GITHUB_LINK = "https://github.com/michelmbem/AddyScript/tree/universal";
    
    public AboutBox()
    {
        InitializeComponent();
        
        Title = $"About {AI.Title}";

        VersionTextBlock.Text = $"Version {AI.Version}";
        DescriptionTextBlock.Text = AI.Description;
        CopyrightTextBlock.Text = AI.Copyright;
    }

    private void HyperlinkButtonClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(GITHUB_LINK) { UseShellExecute = true });
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
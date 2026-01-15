using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AI = AddyScript.Gui.AssemblyInfo;
using SR = AddyScript.Gui.Properties.Resources;

namespace AddyScript.Gui;

public partial class AboutBox : Window
{
    private const string REPO_URL = "https://github.com/michelmbem/AddyScript/tree/universal";
    
    public AboutBox()
    {
        InitializeComponent();
        
        Title = string.Format(SR.AboutBoxTitle, AI.Title);

        VersionTextBlock.Text = string.Format(SR.VersionLabel, AI.Version);
        DescriptionTextBlock.Text = AI.Description;
        CopyrightTextBlock.Text = AI.Copyright;
    }

    private void HyperlinkButtonClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(REPO_URL) { UseShellExecute = true });
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
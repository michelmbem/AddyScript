using AddyScript.Gui.CallTips;
using AvaloniaEdit.CodeCompletion;
using System.ComponentModel;

internal class SimpleOverloadProvider(params CallTipInfo[] callTips) : IOverloadProvider
{
    public int SelectedIndex { get; set; }

    public int Count => callTips.Length;

    public string CurrentIndexText => callTips[SelectedIndex].ToString();

    public object CurrentHeader => $"Overload {SelectedIndex + 1} of {Count}";

    public object CurrentContent => callTips[SelectedIndex].ToControl();

    public event PropertyChangedEventHandler PropertyChanged;
}

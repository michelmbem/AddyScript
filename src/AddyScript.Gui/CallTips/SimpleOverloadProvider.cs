using AvaloniaEdit.CodeCompletion;
using System.ComponentModel;

namespace AddyScript.Gui.CallTips;

internal class SimpleOverloadProvider(params CallTipInfo[] callTips) : IOverloadProvider
{
    public int SelectedIndex { get; set; }

    public int Count => callTips.Length;

    public string CurrentIndexText => callTips[SelectedIndex].ToString();

    public object CurrentHeader => null;

    public object CurrentContent => callTips[SelectedIndex].ToControl();

    public event PropertyChangedEventHandler PropertyChanged;
}

using System.Collections.Generic;
using System.Linq;
using System.Text;
using AddyScript.Runtime;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using Projektanker.Icons.Avalonia;

namespace AddyScript.Gui.CallTips;

internal class CallTipInfo(string functionName, List<ParameterInfo> parameters)
{
    private int activeParameterIndex;

    public CallTipInfo(InnerFunction innerFunction) :
        this(innerFunction.Name, [.. innerFunction.Parameters.Select(p => new ParameterInfo(p))])
    {
    }

    public void Reset() => activeParameterIndex = 0;

    public bool NextParameter()
    {
        var lastParameterIndex = parameters.Count - 1;

        if (activeParameterIndex >= lastParameterIndex)
            return activeParameterIndex == lastParameterIndex && parameters[activeParameterIndex].Infinite;

        ++activeParameterIndex;
        return true;

    }

    public Visual ToVisual()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
        };

        panel.Children.Add(new Icon
        {
            Value = "mdi-function-variant",
            Width = 16,
            Height = 16,
            Margin = new Thickness(2, 0),
        });

        panel.Children.Add(new TextBlock
        {
            Text = functionName,
            Foreground = Brushes.DarkOrange,
            FontWeight = FontWeight.Bold,
        });

        panel.Children.Add(new TextBlock { Text = "(" });

        for (var i = 0; i < parameters.Count; ++i)
        {
            if (i > 0) panel.Children.Add(new TextBlock { Text = ", " });

            panel.Children.Add(new TextBlock
            {
                Text = parameters[i].Text,
                FontWeight = i == activeParameterIndex ? FontWeight.Bold : FontWeight.Regular,
            });
        }

        panel.Children.Add(new TextBlock { Text = ")" });
        return panel;
    }

    public override string ToString()
    {
        var textBuilder = new StringBuilder(functionName).Append('(');

        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0) textBuilder.Append(", ");
            textBuilder.Append(parameters[i].Text);
        }

        return textBuilder.Append(')').ToString();
    }
}
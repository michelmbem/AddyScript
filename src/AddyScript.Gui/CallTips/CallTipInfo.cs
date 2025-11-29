using AddyScript.Runtime;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AddyScript.Gui.CallTips;

public class CallTipInfo(string functionName, List<ParameterInfo> parameters)
{
    public CallTipInfo(InnerFunction innerFunction) :
        this(innerFunction.Name, innerFunction.Parameters
            .Select(p => new ParameterInfo(p.Name, p.VaList))
            .ToList())
    {
    }

    public string FunctionName { get; private init; } = functionName;

    public List<ParameterInfo> Parameters { get; private init; } = parameters;
    
    public int ActiveParameterIndex { get; private set; } = 0;

    public void Reset() => ActiveParameterIndex = 0;

    public bool NextParameter()
    {
        int activeIndex = ActiveParameterIndex;
        
        if (activeIndex < Parameters.Count - 1)
        {
            ActiveParameterIndex = activeIndex + 1;
            return true;
        }

        return false;
    }

    public Control ToControl()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock { Text = $"{FunctionName}(" });

        for (int i = 0; i < Parameters.Count; i++)
        {
            if (i > 0)
                panel.Children.Add(new TextBlock { Text = ", " });

            panel.Children.Add(
                new TextBlock
                {
                    Text = Parameters[i].Text,
                    FontWeight = i == ActiveParameterIndex ? FontWeight.Bold : FontWeight.Regular
                });
        }

        panel.Children.Add(new TextBlock { Text = ")" });
        return panel;
    }

    public override string ToString()
    {
        var textBuilder = new StringBuilder(FunctionName).Append('(');
        bool firstParam = true;

        foreach (ParameterInfo parameter in Parameters)
        {
            if (firstParam)
                firstParam = false;
            else
                textBuilder.Append(", ");

            textBuilder.Append(parameter.Text);
        }

        return textBuilder.Append(')').ToString();
    }
}
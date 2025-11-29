using AddyScript.Runtime;
using AddyScript.Runtime.OOP;
using System.Text;

namespace AddyScript.Gui.CallTips;

internal class ParameterInfo(string text, bool infinite)
{
    public ParameterInfo(Parameter parameter) :
        this(ParameterText(parameter), parameter.VaList)
    {
    }

    public string Text { get; private init; } = text;

    public bool Infinite { get; private init; } = infinite;

    private static string ParameterText(Parameter parameter)
    {
        var textBuilder = new StringBuilder();
        
        if (parameter.ByRef)
            textBuilder.Append('&');
        else if (parameter.VaList)
            textBuilder.Append("..");
        
        textBuilder.Append(parameter.Name);
        
        if (!parameter.CanBeEmpty) textBuilder.Append('!');
        
        if (parameter.DefaultValue != null)
            switch (parameter.DefaultValue.Class.ClassID)
            {
                case ClassID.Date:
                    textBuilder.Append($" = `{parameter.DefaultValue}`");
                    break;
                case ClassID.String:
                    textBuilder.Append($" = \"{parameter.DefaultValue}\"");
                    break;
                default:
                    textBuilder.Append($" = {parameter.DefaultValue}");
                    break;
            }

        return textBuilder.ToString();
    }
}
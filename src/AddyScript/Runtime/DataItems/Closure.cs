using System;
using System.Text;

using AddyScript.Runtime.OOP;
using AddyScript.Translators;


namespace AddyScript.Runtime.DataItems;


public sealed class Closure(Function function) : DataItem
{
    public override Class Class => Class.Closure;

    public override object AsNativeObject => function;

    public override Function AsFunction => function;

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var sb = new StringBuilder();
        sb.Append($"<{Class.Name} (");

        bool stripEnd = false;

        foreach (var param in function.Parameters)
        {

            if (param.ByRef)
                sb.Append('&');
            else if (param.VaList)
                sb.Append("..");

            sb.Append(param.Name);

            if (!param.CanBeEmpty) sb.Append('!');

            if (param.DefaultValue != null)
                switch (param.DefaultValue.Class.ClassID)
                {
                    case ClassID.Date:
                        sb.Append($" = `{param.DefaultValue}`");
                        break;
                    case ClassID.String:
                        sb.Append($" = \"{CodeGenerator.EscapedString(param.DefaultValue.ToString(), false)}\"");
                        break;
                    default:
                        sb.Append($" = {param.DefaultValue}");
                        break;
                }

            sb.Append(", ");
            stripEnd = true;
        }

        if (stripEnd) sb.Length -= 2;

        return sb.Append(")>").ToString();
    }

    protected override bool UnsafeEquals(DataItem other) => function.Equals(other.AsFunction);

    public override int GetHashCode() => function.GetHashCode();

    public override object ConvertTo(Type targetType) => targetType.IsSubclassOf(typeof(Delegate))
         ? function.ToDelegate(targetType)
         : base.ConvertTo(targetType);
}

using System.Collections.Generic;
using System.Text;

using AddyScript.Runtime;
using AddyScript.Runtime.OOP;


namespace AddyScript.Gui.CallTips
{
    public class CallTipInfo
    {
        private readonly List<ParameterInfo> parameters = [];
        private int parameterIndex;

        public CallTipInfo(string text, params ParameterInfo[] parameters)
        {
            Text = text;
            this.parameters.AddRange(parameters);
        }

        public CallTipInfo(InnerFunction innerFunction)
        {
            var textBuilder = new StringBuilder(innerFunction.Name).Append("(");
            bool firstParam = true;

            foreach (Parameter parameter in innerFunction.Parameters)
            {
                if (firstParam)
                    firstParam = false;
                else
                    textBuilder.Append(", ");

                int paramStart = textBuilder.Length;

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
                            textBuilder.AppendFormat(" = `{0}`", parameter.DefaultValue);
                            break;
                        case ClassID.String:
                            textBuilder.AppendFormat(" = '{0}'", parameter.DefaultValue);
                            break;
                        default:
                            textBuilder.AppendFormat(" = {0}", parameter.DefaultValue);
                            break;
                    }

                parameters.Add(new ParameterInfo(paramStart, textBuilder.Length, parameter.VaList));
            }

            Text = textBuilder.Append(')').ToString();
        }

        public string Text { get; private set; }

        public CallTipInfo Parent { get; set; }

        public ParameterInfo ActiveParameter
        {
            get => 0 <= parameterIndex && parameterIndex < parameters.Count
                ? parameters[parameterIndex]
                : null;
        }

        public void Reset()
        {
            parameterIndex = 0;
        }

        public bool NextParameter()
        {
            if (parameterIndex < 0 || parameterIndex >= parameters.Count) return false;
            if (!parameters[parameterIndex].Infinite) ++parameterIndex;
            return parameterIndex < parameters.Count;
        }
    }
}
using System.Collections.Generic;
using System.Text;

using AddyScript.Runtime;


namespace AddyScript.Gui.CallTips
{
    public class CallTipInfo
    {
        private readonly List<ParameterInfo> parameters = new List<ParameterInfo>();
        private int parameterIndex;

        public CallTipInfo(string text, params ParameterInfo[] parameters)
        {
            Text = text;
            this.parameters.AddRange(parameters);
        }

        public CallTipInfo(InnerFunction innerFunction)
        {
            StringBuilder sb = new StringBuilder(innerFunction.Name).Append("(");
            bool trimEnd = false;

            foreach (Parameter parameter in innerFunction.Parameters)
            {
                int start = sb.Length;

                if (parameter.ByRef) sb.Append("ref ");
                if (parameter.VaArgs) sb.Append("params ");
                sb.Append(parameter.Name);

                if (parameter.DefaultValue != null)
                    switch (parameter.DefaultValue.Class.ClassID)
                    {
                        case ClassID.Date:
                            sb.AppendFormat(" = `{0}`", parameter.DefaultValue);
                            break;
                        case ClassID.String:
                            sb.AppendFormat(" = '{0}'", parameter.DefaultValue);
                            break;
                        default:
                            sb.AppendFormat(" = {0}", parameter.DefaultValue);
                            break;
                    }

                parameters.Add(new ParameterInfo(start, sb.Length, parameter.VaArgs));
                sb.Append(", ");
                trimEnd = true;
            }

            if (trimEnd) sb.Remove(sb.Length - 2, 2);
            Text = sb.Append(")").ToString();
        }

        public string Text { get; private set; }

        public CallTipInfo Parent { get; set; }

        public ParameterInfo ActiveParameter
        {
            get
            {
                return (0 <= parameterIndex && parameterIndex < parameters.Count)
                           ? parameters[parameterIndex]
                           : null;
            }
        }

        public void Reset()
        {
            parameterIndex = 0;
        }

        public bool NextParameter()
        {
            if (0 > parameterIndex || parameterIndex >= parameters.Count) return false;
            if (!parameters[parameterIndex].Infinite) ++parameterIndex;
            return parameterIndex < parameters.Count;
        }
    }
}
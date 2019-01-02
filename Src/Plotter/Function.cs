using System.IO;

using AddyScript;
using AddyScript.Ast.Expressions;
using AddyScript.Parsers;


namespace Plotter
{
    public class Function
    {
        private readonly ScriptContext context = new ScriptContext();
        private readonly Expression expression;
        private readonly string parameter;
        
        public Function(string script, string parameter)
        {
            var lexer = new Lexer(new StringReader(script));
            var parser = new ExpressionParser(lexer);
            expression = parser.Expression();

            this.parameter = parameter;
        }

        public double this[double argument]
        {
            get
            {
                context.Variables[parameter] = argument;
                return ScriptEngine.Evaluate(expression, context).AsDouble;
            }
        }
    }
}

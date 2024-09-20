using System.IO;

using AddyScript.Ast.Expressions;
using AddyScript.Parsers;


namespace AddyScript.Plotter
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
                context.Bindings[parameter] = argument;
                return ScriptEngine.Evaluate(expression, context).AsDouble;
            }
        }
    }
}

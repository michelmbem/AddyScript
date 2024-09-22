using System;
using System.IO;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Translators;
using AddyScript.Parsers;
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;
using System.Linq;
using AddyScript.Ast.Statements;


namespace AddyScript
{
    /// <summary>
    /// Simplifies interaction with the whole scripting engine.
    /// </summary>
    public class ScriptEngine
    {
        private readonly Interpreter interpreter; // The interpreter used by this instance
        private string commandPrefix = string.Empty; // Keeps the beginning of uncomplete commands
        private bool satisfied = true; // Indicates that the engine is expecting the continuation of some command or not
        
        /// <summary>
        /// Initializes an instance of ScriptEngine.
        /// </summary>
        /// <param name="context">The initial context of the internal interpreter</param>
        public ScriptEngine(ScriptContext context)
        {
            interpreter = new Interpreter(context);
        }

        /// <summary>
        /// Initializes an instance of ScriptEngine.
        /// </summary>
        public ScriptEngine()
        {
            interpreter = new Interpreter();
        }

        /// <summary>
        /// Indicates that the engine is expecting the continuation of some command or not.
        /// </summary>
        public bool Satisfied => satisfied;

        /// <summary>
        /// Executes a command and returns the resulting value if the command is an expression.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The value produced by the command if any</returns>
        /// <exception cref="ParseException">Any syntax error encountered</exception>
        /// <exception cref="RuntimeException">The command may be erroneous</exception>
        /// <exception cref="ScriptException">There is something wrong either in the syntax or in the logic of the command</exception>
        public DataItem Execute(string command)
        {
            DataItem result = null;
            string fullCommand = (commandPrefix + Environment.NewLine + command).Trim();

            /**
             * Notes: never forget to assign a value to RuntimeServices.Interpreter before running a script!
             * Some features depend on the fact of globally having access to the currently running interpreter.
             */
            RuntimeServices.Interpreter = interpreter;
            
            try
            {
                while (fullCommand.Length > 0)
                {
                    Statement statement = new Parser(new Lexer(new StringReader(fullCommand))).RequiredStatement();
                    statement.AcceptTranslator(interpreter);
                    if (statement is Expression) result = interpreter.ReturnedValue;
                    fullCommand = fullCommand[statement.End.Offset..];
                }

                interpreter.UpdateInitialContextBindings();
                commandPrefix = string.Empty;
                satisfied = true;
            }
            catch (ParseException px)
            {
                if (px.Token.TokenID == TokenID.EndOfFile)
                {
                    commandPrefix = fullCommand;
                    satisfied = false;
                }
                else
                {
                    commandPrefix = string.Empty;
                    throw;
                }
            }
            catch
            {
                commandPrefix = string.Empty;
                throw;
            }

            return result;
        }

        /// <summary>
        /// Invokes a scripted function from user code.
        /// </summary>
        /// <param name="functionName">The name of the function to be invoked</param>
        /// <param name="args">Arguments that will be passed to the function</param>
        /// <returns>The value returned by the function itself</returns>
        public DataItem Invoke(string functionName, params object[] args)
        {
            // Important: define the currently running interpreter!
            RuntimeServices.Interpreter = interpreter;

            var literals = args.Select(arg => new Literal(DataItemFactory.CreateDataItem(arg))).ToArray();
            var call = new FunctionCall(functionName, literals);
            call.AcceptTranslator(interpreter);

            return interpreter.ReturnedValue;
        }

        /// <summary>
        /// Generates a delegate that wraps a scripted function and that could be invoked from the user code.
        /// </summary>
        /// <typeparam name="T">The type of delegate to create</typeparam>
        /// <param name="functionName">The name of the function to be wrapped</param>
        /// <returns>A <see cref="Delegate"/> of the <typeparamref name="T"/> type</returns>
        /// <remarks>
        /// For the returned delegate to run properly, the ScriptEngine from which it was created must be kept alive.
        /// </remarks>
        public T GetDelegate<T>(string functionName) where T : Delegate
        {
            // Important: define the currently running interpreter!
            RuntimeServices.Interpreter = interpreter;
            
            var varRef = new VariableRef(functionName);
            varRef.AcceptTranslator(interpreter);

            return (T)interpreter.ReturnedValue.AsFunction.ToDelegate(typeof(T));
        }

        /// <summary>
        /// Parses a string and gets an AST from it.
        /// </summary>
        /// <param name="script">The string to be parsed</param>
        /// <returns>The AST of the script as an instance of the <see cref="Program"/> class</returns>
        public static Program ParseString(string script)
        {
            return new Parser(new Lexer(new StringReader(script))).Program();
        }

        /// <summary>
        /// Parses a file and gets an AST from it.
        /// </summary>
        /// <param name="path">The path to the source file to be parsed</param>
        /// <returns>The AST of the script as an instance of the <see cref="Program"/> class</returns>
        public static Program ParseFile(string path)
        {
            using StreamReader reader = File.OpenText(path);
            return new Parser(new Lexer(reader)).Program();
        }

        /// <summary>
        /// Parses a string and gets an AST from it representing a single expression.
        /// </summary>
        /// <param name="exprStr">The string to be parsed</param>
        /// <returns>The AST of an expression as an instance of the <see cref="Expression"/> class</returns>
        public static Expression ParseExpression(string exprStr)
        {
            return new ExpressionParser(new Lexer(new StringReader(exprStr))).Expression();
        }

        /// <summary>
        /// Interprets an entire script.
        /// </summary>
        /// <param name="program">The script to interpret</param>
        /// <param name="context">
        /// The context in which the script is interpreted.<br/>
        /// Typically, this will hold the names and values of
        /// the parameters that you want to pass to the script
        /// and may want to retrieve upon completion.
        /// </param>
        public static void Execute(Program program, ScriptContext context)
        {
            var interpreter = new Interpreter(context);
            RuntimeServices.Interpreter = interpreter;
            program.AcceptTranslator(interpreter);
            interpreter.UpdateInitialContextBindings();
        }

        /// <summary>
        /// Interprets an entire script.
        /// </summary>
        /// <param name="script">A string containing the script to execute</param>
        /// <param name="context">
        /// The context in which the script is interpreted.<br/>
        /// Typically, this will hold the names and values of
        /// the parameters that you want to pass to the script
        /// and may want to retrieve upon completion.
        /// </param>
        public static void ExecuteString(string script, ScriptContext context)
        {
            Execute(ParseString(script), context);
        }

        /// <summary>
        /// Interprets an entire script.
        /// </summary>
        /// <param name="path">The path to the script's source file</param>
        /// <param name="context">
        /// The context in which the script is interpreted.<br/>
        /// Typically, this will hold the names and values of
        /// the parameters that you want to pass to the script
        /// and may want to retrieve upon completion.
        /// </param>
        public static void ExecuteFile(string path, ScriptContext context)
        {
            Execute(ParseFile(path), context);
        }

        /// <summary>
        /// Evaluates an eventually parameterized expression and returns the result.
        /// </summary>
        /// <param name="expression">The expression to evaluate</param>
        /// <param name="context">
        /// The context in which the expression is evaluated.
        /// Typically, this will hold the names and values of
        /// the parameters that we want to pass to the expression.
        /// </param>
        /// <returns>A <see cref="DataItem"/></returns>
        public static DataItem Evaluate(Expression expression, ScriptContext context)
        {
            var interpreter = new Interpreter(context);
            RuntimeServices.Interpreter = interpreter;
            expression.AcceptTranslator(interpreter);
            interpreter.UpdateInitialContextBindings();

            return interpreter.ReturnedValue;
        }

        /// <summary>
        /// Evaluates an eventually parameterized expression and returns the result.
        /// </summary>
        /// <param name="exprStr">The string containing the expresion to be parsed</param>
        /// <param name="context">
        /// The context in which the expression is evaluated.
        /// Typically, this will hold the names and values of
        /// the parameters that we want to pass to the expression.
        /// </param>
        /// <returns>A <see cref="DataItem"/></returns>
        public static DataItem EvaluateString(string exprStr, ScriptContext context)
        {
            return Evaluate(ParseExpression(exprStr), context);
        }

        /// <summary>
        /// Exports the AST to Xml format.
        /// </summary>
        /// <param name="program">The AST root node</param>
        /// <param name="output">The destination output stream</param>
        public static void ExportXml(Program program, Stream output)
        {
            var exporter = new XmlGenerator();
            program.AcceptTranslator(exporter);
            exporter.Document.Save(output);
        }

        /// <summary>
        /// Exports the AST to Xml format.
        /// </summary>
        /// <param name="program">The AST root node</param>
        /// <param name="fileName">The path to the destination file</param>
        public static void ExportXml(Program program, string fileName)
        {
            var exporter = new XmlGenerator();
            program.AcceptTranslator(exporter);
            exporter.Document.Save(fileName);
        }

        /// <summary>
        /// Regenerates the source-code of a script from its AST.
        /// </summary>
        /// <param name="program">The AST root node</param>
        /// <returns>A string</returns>
        public static string GenerateCode(Program program)
        {
            using var writer = new StringWriter();
            var codegen = new CodeGenerator(writer);
            program.AcceptTranslator(codegen);
            writer.Flush();

            return writer.ToString();
        }
    }
}
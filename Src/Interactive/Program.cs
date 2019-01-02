using System;

using AddyScript.Runtime;
using AddyScript.Runtime.Dynamics;


namespace AddyScript.Interactive
{
    static class Program
    {
        const string WELCOME_MESSAGE_FORMAT = "{0} Interactive Shell, version {1} by {2}.\r\nWebsite: addyscript.codeplex.com.";
        const string SUCCESS_MESSAGE_FORMAT = "res: {0}";
        const string ERROR_MESSAGE_FORMAT = "{0}: {1} @{2}: {3}";
        const string MAIN_PROMPT = ">>> ";
        const string CONTINUATION_PROMPT = "... ";
        const string WAIT_KEY_PROMPT = "\r\nPress any key before exit";

        static string promt = MAIN_PROMPT;
        
        static void Main(string[] args)
        {
            OptionSet options = null;
            ScriptContext context = null;

            try
            {
                options = new OptionSet(args);
                context = options.GetScriptContext();
            }
            catch (InvalidOptionException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(-1);
            }

            switch (options.ExecutionMode)
            {
                case ExecutionMode.Default:
                case ExecutionMode.Interactive:
                    RunInteractive(context);
                    break;
                case ExecutionMode.Evaluate:
                    Evaluate(options.Input, context);
                    break;
                case ExecutionMode.RunFile:
                    RunFile(options.Input, context);
                    break;
            }
        }

        private static void RunInteractive(ScriptContext context)
        {
            var engine = new ScriptEngine(context);

            ShowWelcome();

            while (true)
            {
                string command = ReadCommand();
                if (command == null || command.Trim().Length == 0) continue;
                if (engine.Satisfied && command == "exit") break;

                try
                {
                    var result = engine.Execute(command);
                    if (result != null) ShowResult(result);

                    promt = engine.Satisfied ? MAIN_PROMPT : CONTINUATION_PROMPT;
                }
                catch (ScriptException sx)
                {
                    ShowError(sx);
                    promt = MAIN_PROMPT;
                }
            }
        }

        private static void Evaluate(string command, ScriptContext context)
        {
            try
            {
                ScriptEngine.ExecuteString(command + ";", context);

                var result = RuntimeServices.Interpreter.ReturnedValue;
                Console.WriteLine(RuntimeServices.ToString(result));
            }
            catch (ScriptException sx)
            {
                ShowError(sx);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void RunFile(string path, ScriptContext context)
        {
            try
            {
                ScriptEngine.ExecuteFile(path, context);
            }
            catch (ScriptException sx)
            {
                ShowError(sx);
                WaitKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                WaitKey();
            }
        }

        private static string ReadCommand()
        {
            Console.Write(promt);
            return Console.ReadLine();
        }

        private static void ShowWelcome()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(WELCOME_MESSAGE_FORMAT,
                              AssemblyInfo.Title,
                              AssemblyInfo.Version,
                              AssemblyInfo.Company);
            Console.ResetColor();
        }

        private static void ShowResult(Dynamic result)
        {
            string resultStr = RuntimeServices.ToString(result);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(SUCCESS_MESSAGE_FORMAT, resultStr);
            Console.ResetColor();
        }

        private static void ShowError(ScriptException sx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ERROR_MESSAGE_FORMAT,
                              sx.GetType().FullName,
                              sx.Message,
                              string.IsNullOrEmpty(sx.FileName) ? "(asis)" : sx.FileName,
                              sx.ScriptElement.Start.LineNumber + 1);
            Console.ResetColor();
        }

        private static void WaitKey()
        {
            Console.WriteLine(WAIT_KEY_PROMPT);
            Console.ReadKey(true);
        }
    }
}

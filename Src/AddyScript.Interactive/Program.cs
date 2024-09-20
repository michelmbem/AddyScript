﻿using System;
using System.IO;

using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Interactive
{
    static class Program
    {
        const string WELCOME_MESSAGE_FORMAT = "{0} Interactive Shell, version {1} by {2}.\r\nGitHub page: https://github.com/michelmbem/AddyScript.\r\nUse the exit() function to quit.";
        const string SUCCESS_MESSAGE_FORMAT = "res: {0}";
        const string ERROR_MESSAGE_FORMAT = "{0}: \"{1}\" in {2} at line {3}, column {4}";
        const string MAIN_PROMPT = ">>> ";
        const string CONTINUATION_PROMPT = "... ";
        const string WAIT_KEY_PROMPT = "\r\nPress any key before exit";

        static string prompt = MAIN_PROMPT;

        [STAThread]
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
                ShowError(ex);
                Environment.Exit(1);
            }

            InnerFunction.Globals.Add(SourceFunction(context));
            InnerFunction.Globals.Add(ExitFunction());

            switch (options.ExecutionMode)
            {
                case ExecutionMode.Evaluate:
                    Evaluate(options.Input, options.Log, context);
                    break;
                case ExecutionMode.RunFile:
                    RunFile(options.Input, options.Log, context);
                    break;
                default:
                    RunInteractive(context);
                    break;
            }
        }

        private static void Evaluate(string command, string log, ScriptContext context)
        {
            try
            {
                ScriptEngine.ExecuteString(command + ";", context);

                var result = RuntimeServices.Interpreter.ReturnedValue;
                Console.WriteLine(RuntimeServices.ToString(result));
            }
            catch (ScriptException sx)
            {
                ShowError(sx, log);
                Environment.Exit(2);
            }
            catch (Exception ex)
            {
                ShowError(ex);
                Environment.Exit(3);
            }
        }

        private static void RunFile(string path, string log, ScriptContext context)
        {
            try
            {
                ScriptEngine.ExecuteFile(path, context);
            }
            catch (ScriptException sx)
            {
                ShowError(sx, log);
                WaitKey();
                Environment.Exit(2);
            }
            catch (Exception ex)
            {
                ShowError(ex);
                WaitKey();
                Environment.Exit(3);
            }
        }

        private static void RunInteractive(ScriptContext context)
        {
            var engine = new ScriptEngine(context);

            ShowWelcome();

            while (true)
            {
                string command = ReadCommand();
                if (string.IsNullOrWhiteSpace(command)) continue;

                try
                {
                    var result = engine.Execute(command);
                    if (result != null) ShowResult(result);

                    prompt = engine.Satisfied ? MAIN_PROMPT : CONTINUATION_PROMPT;
                }
                catch (ScriptException sx)
                {
                    ShowError(sx, null);
                    prompt = MAIN_PROMPT;
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                    prompt = MAIN_PROMPT;
                }
            }
        }

        private static void ShowWelcome()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(WELCOME_MESSAGE_FORMAT, AssemblyInfo.Title, AssemblyInfo.Version, AssemblyInfo.Company);
            Console.ResetColor();
        }

        private static string ReadCommand()
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }

        private static void ShowResult(DataItem result)
        {
            string resultStr = RuntimeServices.ToString(result);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(SUCCESS_MESSAGE_FORMAT, resultStr);
            Console.ResetColor();
        }

        private static void ShowError(ScriptException sx, string log)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ERROR_MESSAGE_FORMAT,
                              sx.GetType().FullName,
                              sx.Message,
                              string.IsNullOrEmpty(sx.FileName) ? "(asis)" : sx.FileName,
                              sx.Element.Start.LineNumber + 1,
                              sx.Element.Start.Offset - sx.Element.Start.LineOffset + 1);
            Console.ResetColor();

            if (string.IsNullOrWhiteSpace(log)) return;

            using var logWriter = File.CreateText(log);
            logWriter.WriteLine(sx.FileName);
            logWriter.WriteLine($"{sx.Element.Start.Offset},{sx.Element.Start.LineOffset},{sx.Element.Start.LineNumber}");
            logWriter.WriteLine($"{sx.Element.End.Offset},{sx.Element.End.LineOffset},{sx.Element.End.LineNumber}");
            logWriter.WriteLine(sx.Message);
        }

        private static void ShowError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"{ex.GetType().FullName} : {ex.Message}");
            Console.ResetColor();
        }

        private static void WaitKey()
        {
            Console.WriteLine(WAIT_KEY_PROMPT);
            Console.ReadKey(true);
        }

        private static InnerFunction SourceFunction(ScriptContext context)
        {
            var functionLogic = new InnerFunctionLogic(arguments =>
            {
                RunFile(arguments[0].ToString(), null, context);
                return Runtime.DataItems.Void.Value;
            });

            return new InnerFunction("source", [new Parameter("path")], functionLogic);
        }

        private static InnerFunction ExitFunction()
        {
            var functionLogic = new InnerFunctionLogic(arguments =>
            {
                Environment.Exit(0);
                return null; //will never be run
            });

            return new InnerFunction("exit", [], functionLogic);
        }
    }
}

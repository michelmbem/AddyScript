using System;
using System.IO;
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;
using String = AddyScript.Runtime.DataItems.String;
using Void = AddyScript.Runtime.DataItems.Void;

namespace AddyScript.Interactive;

internal static class Program
{
    private const string WELCOME_MESSAGE_FORMAT = "{0} Interactive Shell, version {1} by {2}.\r\n" +
                                                  "GitHub: https://github.com/michelmbem/AddyScript.\r\n" +
                                                  "Wiki: https://michelmbem.github.io/AddyScript.\r\n" +
                                                  "Use the exit() function to quit.";
    private const string SUCCESS_MESSAGE_FORMAT = "res: {0}";
    private const string ERROR_MESSAGE_FORMAT = "{0}: \"{1}\" in {2} at line {3}, column {4}";
    private const string MAIN_PROMPT = ">>> ";
    private const string CONTINUATION_PROMPT = "... ";
    private const string WAIT_KEY_PROMPT = "\r\nPress any key before exit";

    private static string prompt = MAIN_PROMPT;

    [STAThread]
    public static void Main(string[] args)
    {
        OptionSet options = null;

        try
        {
            options = new OptionSet(args);
        }
        catch (InvalidOptionException ex)
        {
            ShowError(ex);
            Environment.Exit(1);
        }

        switch (options.ExecutionMode)
        {
            case ExecutionMode.Evaluate:
                Evaluate(options.Input, options.Log, options.Context);
                break;
            case ExecutionMode.RunFile:
                RunFile(options.Input, options.Log, options.Context);
                break;
            default:
                RunInteractive(options.Context);
                break;
        }
    }

    private static void Evaluate(string command, string log, ScriptContext context)
    {
        try
        {
            ScriptEngine.ExecuteString($"{command};", context);

            var result = RuntimeServices.Interpreter.ReturnedValue;
            Console.WriteLine(RuntimeServices.ToString(result));
        }
        catch (ScriptError se)
        {
            ShowError(se, log);
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
        catch (ScriptError se)
        {
            ShowError(se, log);
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
        InnerFunction.Globals.Add(SourceFunction(context));

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
            catch (ScriptError se)
            {
                ShowError(se, null);
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
        var resultStr = result is String or Blob
            ? RuntimeServices.ToString(result, "x")
            : RuntimeServices.ToString(result);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(SUCCESS_MESSAGE_FORMAT, resultStr);
        Console.ResetColor();
    }

    private static void ShowError(ScriptError se, string log)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ERROR_MESSAGE_FORMAT,
                          se.GetType().FullName,
                          se.Message,
                          string.IsNullOrEmpty(se.FileName) ? "(stdin)" : se.FileName,
                          se.Element.Start.LineNumber + 1,
                          se.Element.Start.Offset - se.Element.Start.LineOffset + 1);
        Console.ResetColor();

        if (string.IsNullOrWhiteSpace(log)) return;

        using var logWriter = File.CreateText(log);
        logWriter.WriteLine(se.FileName);
        logWriter.WriteLine($"{se.Element.Start.Offset},{se.Element.Start.LineOffset},{se.Element.Start.LineNumber}");
        logWriter.WriteLine($"{se.Element.End.Offset},{se.Element.End.LineOffset},{se.Element.End.LineNumber}");
        logWriter.WriteLine(se.Message);
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
            return Void.Value;
        });

        return new InnerFunction("source", [new Parameter("path")], functionLogic);
    }
}
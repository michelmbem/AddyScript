using System.Globalization;
using System.Threading;

namespace AddyScript.Interactive;

public class OptionSet
{
    public OptionSet(string[] args)
    {
        var mode = ExecutionMode.Default;
        string option = null, input = null, log = null, culture = null;

        Context.AddReference(typeof(System.Diagnostics.Process).Assembly);
        Context.AddReference(typeof(System.Console).Assembly);

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-i":
                    CheckOption(option);
                    CheckMode(mode, arg);
                    option = arg;
                    mode = ExecutionMode.Interactive;
                    break;
                case "-e":
                    CheckOption(option);
                    CheckMode(mode, arg);
                    option = arg;
                    mode = ExecutionMode.Evaluate;
                    break;
                case "-f":
                    CheckOption(option);
                    CheckMode(mode, arg);
                    option = arg;
                    mode = ExecutionMode.RunFile;
                    break;
                case "-d" or "-r" or "-l" or "-c":
                    CheckOption(option);
                    option = arg;
                    break;
                default:
                    switch (option)
                    {
                        case "-e" or "-f":
                            input = arg;
                            break;
                        case "-d":
                            Context.AddImportPath(arg);
                            break;
                        case "-r":
                            Context.AddReference(arg);
                            break;
                        case "-l":
                            log = arg;
                            break;
                        case "-c":
                            culture = arg;
                            break;
                        case null:
                            throw new InvalidOptionException(arg);
                    }

                    option = null;
                    break;
            }
        }

        ExecutionMode = mode;
        Input = input;
        Log = log;

        if (culture == null) return;
        var currentThread = Thread.CurrentThread;
        currentThread.CurrentUICulture = currentThread.CurrentCulture = new CultureInfo(culture);
    }

    public ExecutionMode ExecutionMode { get; }

    public string Input { get; }

    public string Log { get; }

    public ScriptContext Context { get; } = new ();

    private static void CheckOption(string option)
    {
        if (option == null) return;

        var message = option switch
        {
            "-e" => "An expression is expected after -e",
            "-f" => "A file name is expected after -f",
            "-d" => "A directory name is expected after -d",
            "-r" => "An assembly name is expected after -r",
            "-l" => "A file name is expected after -l",
            "-c" => "A culture name is expected after -c",
            _ => null
        };

        if (message == null) return;
        throw new InvalidOptionException(option, message);
    }

    private static void CheckMode(ExecutionMode mode, string option)
    {
        if (mode == ExecutionMode.Default) return;
        throw new InvalidOptionException(option, "Cannot specify execution mode twice");
    }
}

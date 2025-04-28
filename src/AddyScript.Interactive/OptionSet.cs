namespace AddyScript.Interactive
{
    public class OptionSet
    {
        public OptionSet(string[] args)
        {
            var mode = ExecutionMode.Default;
            string option = null, input = null, log = null;

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
                    case "-d":
                    case "-r":
                    case "-l":
                        CheckOption(option);
                        option = arg;
                        break;
                    default:
                        switch (option)
                        {
                            case "-e":
                            case "-f":
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
                            default:
                                if (option == null)
                                    throw new InvalidOptionException(arg);
                                break;
                        }

                        option = null;
                        break;
                }
            }

            ExecutionMode = mode;
            Input = input;
            Log = log;
        }

        public ExecutionMode ExecutionMode { get; private set; }

        public string Input { get; private set; }

        public string Log { get; private set; }

        public ScriptContext Context { get; } = new();

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
                _ => null
            };

            if (message != null)
                throw new InvalidOptionException(option, message);
        }

        private static void CheckMode(ExecutionMode mode, string option)
        {
            if (mode != ExecutionMode.Default)
                throw new InvalidOptionException(option, "Cannot specify execution mode twice");
        }
    }
}

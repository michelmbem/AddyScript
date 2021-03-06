﻿using System.Collections.Generic;
using System.Reflection;


namespace AddyScript.Interactive
{
    public class OptionSet
    {
        public OptionSet(string[] args)
        {
            string option = null, input = null;
            var directories = new List<string>();
            var assemblies = new List<Assembly> { ScriptContext.Mscorlib };
            ExecutionMode mode = ExecutionMode.Default;
            
            foreach (string arg in args)
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
                                directories.Add(arg);
                                break;
                            case "-r":
                                assemblies.Add(ScriptContext.LoadAssembly(arg));
                                break;
                            default:
                                if (option == null) throw new InvalidOptionException(arg);
                                break;
                        }

                        option = null;
                        break;
                }
            }

            ExecutionMode = mode;
            Input = input;
            Directories = directories.ToArray();
            Assemblies = assemblies.ToArray();
        }

        public ExecutionMode ExecutionMode { get; private set; }

        public string[] Directories { get; private set; }

        public Assembly[] Assemblies { get; private set; }

        public string Input { get; private set; }

        public ScriptContext GetScriptContext()
        {
            return new ScriptContext
                       {
                           SearchPath = Directories,
                           References = Assemblies
                       };
        }

        private static void CheckOption(string option)
        {
            if (option == null) return;

            string message = null;

            switch (option)
            {
                case "-e":
                    message = "An expression is expected after -e";
                    break;
                case "-f":
                    message = "A file name is expected after -f";
                    break;
                case "-d":
                    message = "A directory name is expected after -d";
                    break;
                case "-r":
                    message = "An assembly name is expected after -r";
                    break;
            }

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

using System;


namespace AddyScript.Interactive
{
    public class InvalidOptionException : ApplicationException
    {
        public InvalidOptionException(string option)
            : base("Invalid option: " + option)
        {
            Option = option;
        }

        public InvalidOptionException(string option, string message)
            : base(message)
        {
            Option = option;
        }

        public string Option { get; private set; }
    }
}

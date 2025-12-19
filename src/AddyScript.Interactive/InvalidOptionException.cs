using System;


namespace AddyScript.Interactive
{
    public class InvalidOptionException(string option, string message) : ApplicationException(message)
    {
        public InvalidOptionException(string option) : this(option, "Invalid option: " + option)
        {
        }

        public string Option => option;
    }
}

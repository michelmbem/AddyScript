using System;

using AddyScript.Properties;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Undefined : DataItem
    {
        public static readonly Undefined Value = new ();

        private Undefined()
        {
        }

        public override Class Class
        {
            get { throw new InvalidOperationException(Resources.ObjectInInvalidState); }
        }
    }
}

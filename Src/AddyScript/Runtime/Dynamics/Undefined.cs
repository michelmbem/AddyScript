using System;

using AddyScript.Properties;


namespace AddyScript.Runtime.Dynamics
{
    public class Undefined : Dynamic
    {
        public static readonly Undefined Value = new Undefined();

        private Undefined()
        {
        }

        public override Class Class
        {
            get { throw new InvalidOperationException(Resources.ObjectInInvalidState); }
        }
    }
}

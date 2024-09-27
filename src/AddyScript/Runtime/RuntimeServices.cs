using System;
using System.IO;
using System.Linq;

using AddyScript.Ast.Expressions;
using AddyScript.Translators;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime
{
    /// <summary>
    /// Provides a set of services that may only be available at runtime.
    /// </summary>
    public static class RuntimeServices
    {
        /// <summary>
        /// Class initializer.
        /// </summary>
        static RuntimeServices()
        {
            In = Console.In;
            Out = Console.Out;
        }

        /// <summary>
        /// Gets/Sets the standard input for the currently running script.
        /// </summary>
        public static TextReader In { get; set; }

        /// <summary>
        /// Gets/Sets the standard output for the currently running script.
        /// </summary>
        public static TextWriter Out { get; set; }

        /// <summary>
        /// Gets/Sets the currently running interpreter.
        /// </summary>
        public static Interpreter Interpreter { get; set; }

        /// <summary>
        /// Invokes a static method with the given arguments.
        /// </summary>
        /// <param name="methodName">The name of the method that should be invoked</param>
        /// <param name="methodHolder">The class that holds the method</param>
        /// <param name="args">The arguments to pass to the method</param>
        /// <returns>The value returned by the method</returns>
        public static DataItem Invoke(string methodName, Class methodHolder, params object[] args)
        {
            var name = new QualifiedName(methodHolder.Name, methodName);
            var literals = args.Select(arg => new Literal(DataItemFactory.CreateDataItem(arg))).ToArray();
            new StaticMethodCall(name, literals).AcceptTranslator(Interpreter);

            return Interpreter.ReturnedValue; ;
        }

        /// <summary>
        /// Invokes a method from the given object with the given arguments.
        /// </summary>
        /// <param name="methodName">The name of the method that should be invoked</param>
        /// <param name="methodTarget">The object from which to call a method</param>
        /// <param name="args">The arguments to pass to the method</param>
        /// <returns>The value returned by the method</returns>
        public static DataItem Invoke(string methodName, DataItem methodTarget, params object[] args)
        {
            Expression targetExpr = new Literal(methodTarget);
            var literals = args.Select(arg => new Literal(DataItemFactory.CreateDataItem(arg))).ToArray();
            new MethodCall(targetExpr, methodName, literals).AcceptTranslator(Interpreter);

            return Interpreter.ReturnedValue;
        }

        /// <summary>
        /// Invokes <i>equals</i> on the given object with the given argument.
        /// </summary>
        /// <param name="a">The object on which to invoke <i>equals</i></param>
        /// <param name="b">The given argument</param>
        /// <returns>a <see cref="bool"/></returns>
        public static bool Equals(DataItem a, DataItem b)
        {
            return Invoke("equals", a, b).AsBoolean;
        }

        /// <summary>
        /// Invokes <i>hashCode</i> on the given object.
        /// </summary>
        /// <param name="value">The object on which to invoke <i>hashCode</i></param>
        /// <returns>An <see cref="int"/></returns>
        public static int HashCode(DataItem value)
        {
            return Invoke("hashCode", value).AsInt32;
        }

        /// <summary>
        /// Invokes <i>compareTo</i> on the given object with the given argument.
        /// </summary>
        /// <param name="a">The object on which to invoke <i>compareTo</i></param>
        /// <param name="b">The given argument</param>
        /// <returns>An <see cref="int"/></returns>
        public static int CompareTo(DataItem a, DataItem b)
        {
            return Invoke("compareTo", a, b).AsInt32;
        }

        /// <summary>
        /// Invokes <i>toString</i> on the given object with the given format.
        /// </summary>
        /// <param name="value">The object on which to invoke <i>toString</i></param>
        /// <param name="format">The given format</param>
        /// <returns>A <see cref="string"/></returns>
        public static string ToString(DataItem value, string format)
        {
            return Invoke("toString", value, format).ToString();
        }

        /// <summary>
        /// Invokes <i>toString</i> on the given object.
        /// </summary>
        /// <param name="value">The object on which to invoke <i>toString</i></param>
        /// <returns>A <see cref="string"/></returns>
        public static string ToString(DataItem value)
        {
            return Invoke("toString", value).ToString();
        }

        /// <summary>
        /// Invokes <i>clone</i> on the given object.
        /// </summary>
        /// <param name="value">The object on which to invoke <i>clone</i></param>
        /// <returns>A <see cref="DataItem"/></returns>
        public static DataItem Clone(DataItem value)
        {
            return Invoke("clone", value);
        }

        /// <summary>
        /// Invokes <i>dispose</i> on the given object.
        /// </summary>
        /// <param name="value">The object on which to invoke <i>dispose</i></param>
        public static void Dispose(DataItem value)
        {
            Invoke("dispose", value);
        }
    }
}

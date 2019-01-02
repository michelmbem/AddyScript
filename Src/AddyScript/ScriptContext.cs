using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


namespace AddyScript
{
    /// <summary>
    /// A way to interact with the scripting engine.
    /// </summary>
    public class ScriptContext
    {
        /// <summary>
        /// Keeps a global reference to the mscorlib assembly
        /// </summary>
        public static Assembly Mscorlib = typeof(object).Assembly;

        /// <summary>
        /// Initializes a new instance of <see cref="ScriptContext"/>.
        /// </summary>
        public ScriptContext()
        {
            Variables = new Dictionary<string, object>();
            SearchPath = new string[] {};
            References = new[] { Mscorlib };
        }

        /// <summary>
        /// A set of variables that will automatically be declared in the script on startup.
        /// </summary>
        public Dictionary<string, object> Variables { get; private set; }

        /// <summary>
        /// The set of directories where imported scripts are searched for.
        /// </summary>
        public string[] SearchPath { get; set; }

        /// <summary>
        /// The set of assemblies referenced by the script.
        /// </summary>
        public Assembly[] References { get; set; }

        /// <summary>
        /// Loads an assembly given its name.
        /// </summary>
        /// <param name="name">The name of the assembly to be loaded</param>
        /// <returns>An <see cref="Assembly"/></returns>
        public static Assembly LoadAssembly(string name)
        {
            Assembly asm;

            try
            {
                asm = Assembly.LoadFrom(name);
            }
            catch (FileNotFoundException)
            {
                try
                {
                    asm = Assembly.Load(name);
                }
                catch (FileNotFoundException)
                {
#pragma warning disable 618,612
                    asm = Assembly.LoadWithPartialName(name);
#pragma warning restore 618,612
                }
            }

            return asm;
        }

        /// <summary>
        /// Adds a directory to the search path.
        /// </summary>
        /// <param name="dirName">The path to be added</param>
        public void AddToSearchPath(string dirName)
        {
            if (Array.IndexOf(SearchPath, dirName) >= 0) return;

            var tmpList = new List<string>(SearchPath) { dirName };
            SearchPath = tmpList.ToArray();
        }

        /// <summary>
        /// Removes a path from the set of directories where imported scripts are searched for.
        /// </summary>
        /// <param name="dirName">The path to be removed</param>
        public void RemoveFromSearchPath(string dirName)
        {
            var tmpList = new List<string>(SearchPath);
            tmpList.Remove(dirName);
            SearchPath = tmpList.ToArray();
        }

        /// <summary>
        /// Adds an assembly to the set of assemblies referenced by the script.
        /// </summary>
        /// <param name="reference">The assembly to be added</param>
        public void AddReference(Assembly reference)
        {
            if (Array.IndexOf(References, reference) >= 0) return;

            var tmpList = new List<Assembly>(References) { reference };
            References = tmpList.ToArray();
        }

        /// <summary>
        /// Adds an assembly to the set of assemblies referenced by the script.
        /// </summary>
        /// <param name="reference">The name of the assembly to be added</param>
        public void AddReference(string reference)
        {
            AddReference(LoadAssembly(reference));
        }

        /// <summary>
        /// Removes an assembly from the set of assemblies referenced by the script.
        /// </summary>
        /// <param name="reference">The assembly to be removed</param>
        public void RemoveReference(Assembly reference)
        {
            var tmpList = new List<Assembly>(References);
            tmpList.Remove(reference);
            References = tmpList.ToArray();
        }

        /// <summary>
        /// Removes an assembly from the set of assemblies referenced by the script.
        /// </summary>
        /// <param name="reference">The name of the assembly to be removed</param>
        public void RemoveReference(string reference)
        {
            RemoveReference(Array.Find(References, a => a.FullName == reference));
        }
    }
}
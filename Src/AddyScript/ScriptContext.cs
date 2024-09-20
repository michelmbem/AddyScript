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
        /// Keeps a global reference to the <i>System.Private.CoreLib</i> assembly
        /// </summary>
        public static readonly Assembly CoreLib = typeof(object).Assembly;

        /// <summary>
        /// Initializes a new instance of <see cref="ScriptContext"/>.
        /// </summary>
        public ScriptContext()
        {
            Bindings = [];
            ImportPaths = [];
            References = [CoreLib];
        }

        /// <summary>
        /// A set of variable bindings that will automatically be declared in the script on startup.
        /// </summary>
        public Dictionary<string, object> Bindings { get; private set; }

        /// <summary>
        /// The set of directories where imported scripts are searched for.
        /// </summary>
        public string[] ImportPaths { get; set; }

        /// <summary>
        /// The set of .Net assemblies referenced by the script.
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
        /// Adds a directory to the <see cref="ImportPaths"/> property.
        /// </summary>
        /// <param name="importPath">The path to be added</param>
        public void AddImportPath(string importPath)
        {
            if (Array.IndexOf(ImportPaths, importPath) >= 0) return;

            var tmpList = new List<string>(ImportPaths) { importPath };
            ImportPaths = [.. tmpList];
        }

        /// <summary>
        /// Removes a path from the set of directories where imported scripts are searched for.
        /// </summary>
        /// <param name="importPath">The path to be removed</param>
        public void RemoveImportPath(string importPath)
        {
            var tmpList = new List<string>(ImportPaths);
            tmpList.Remove(importPath);
            ImportPaths = [.. tmpList];
        }

        /// <summary>
        /// Adds an assembly to the set of assemblies referenced by the script.
        /// </summary>
        /// <param name="reference">The assembly to be added</param>
        public void AddReference(Assembly reference)
        {
            if (Array.IndexOf(References, reference) >= 0) return;

            var tmpList = new List<Assembly>(References) { reference };
            References = [.. tmpList];
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
            References = [.. tmpList];
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
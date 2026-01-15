using System.Collections.Generic;
using System.IO;
using System.Reflection;


namespace AddyScript;


/// <summary>
/// A way to interact with the scripting engine.
/// </summary>
public class ScriptContext
{
    /// <summary>
    /// Keeps a global reference to the <i>System.Private.CoreLib</i> assembly
    /// </summary>
    public static readonly Assembly CoreLib = typeof(object).Assembly;

    private readonly Dictionary<string, object> bindings = [];
    private readonly HashSet<string> importPaths = [];
    private readonly HashSet<Assembly> references = [CoreLib];

    /// <summary>
    /// A set of variable bindings that will automatically be declared in the script on startup.
    /// </summary>
    public Dictionary<string, object> Bindings => bindings;

    /// <summary>
    /// The set of directories where imported scripts are searched for.
    /// </summary>
    public IEnumerable<string> ImportPaths => importPaths;

    /// <summary>
    /// The set of .Net assemblies referenced by the script.
    /// </summary>
    public IEnumerable<Assembly> References => references;

    /// <summary>
    /// Loads an assembly given its name.
    /// </summary>
    /// <param name="name">The name of the assembly to be loaded</param>
    /// <returns>An <see cref="Assembly"/></returns>
    public static Assembly LoadAssembly(string name)
    {
        Assembly assembly;

        try
        {
            assembly = Assembly.LoadFrom(name);
        }
        catch (FileNotFoundException)
        {
            try
            {
                assembly = Assembly.Load(name);
            }
            catch (FileNotFoundException)
            {
#pragma warning disable 618,612
                assembly = Assembly.LoadWithPartialName(name);
#pragma warning restore 618,612
            }
        }

        return assembly;
    }

    /// <summary>
    /// Adds a directory to the <see cref="ImportPaths"/> property.
    /// </summary>
    /// <param name="importPath">The path to add</param>
    public void AddImportPath(string importPath) => importPaths.Add(importPath);

    /// <summary>
    /// Removes a path from the set of directories where imported scripts are searched for.
    /// </summary>
    /// <param name="importPath">The path to remove</param>
    public void RemoveImportPath(string importPath) => importPaths.Remove(importPath);

    /// <summary>
    /// Adds an assembly along with all of its direct and indirect dependencies to the set of assemblies referenced by the script.
    /// </summary>
    /// <param name="reference">The assembly to add</param>
    public void AddReference(Assembly reference) => RecursivelyAdd(reference);

    /// <summary>
    /// Adds an assembly along with all of its direct and indirect dependencies to the set of assemblies referenced by the script.
    /// </summary>
    /// <param name="reference">The name of the <see cref="Assembly"/> to add</param>
    public void AddReference(string reference) => AddReference(LoadAssembly(reference));

    /// <summary>
    /// Removes an assembly from the set of assemblies referenced by the script.
    /// </summary>
    /// <param name="reference">The assembly to remove</param>
    public void RemoveReference(Assembly reference) => references.Remove(reference);

    /// <summary>
    /// Removes an assembly from the set of assemblies referenced by the script.
    /// </summary>
    /// <param name="reference">The full name of the <see cref="Assembly"/> to remove</param>
    public void RemoveReference(string reference) =>
        references.RemoveWhere(a => a.FullName == reference);

    /// <summary>
    /// Recursively adds an <see cref="Assembly"/> and its dependencies to the <see cref="references"/> set.
    /// </summary>
    /// <param name="reference">The <see cref="Assembly"/> to add as a reference</param>
    private void RecursivelyAdd(Assembly reference)
    {
        if (references.Contains(reference)) return;

        references.Add(reference);

        foreach (AssemblyName dependency in reference.GetReferencedAssemblies())
            try
            {
                RecursivelyAdd(Assembly.Load(dependency));
            }
            catch
            {
                // Ignore loading failures
            }
    }
}
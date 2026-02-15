using System.IO;
using System.Linq;
using System.Reflection;

namespace AddyScript.Interactive;

internal static class AssemblyInfo
{
    public static string Title => GetAssemblyAttribute<AssemblyTitleAttribute>()?.Title ??
                                  Path.GetFileNameWithoutExtension(ExecutingAssembly.Location);

    public static string Description => GetAssemblyAttribute<AssemblyDescriptionAttribute>()?.Description;

    public static string Version => ExecutingAssembly.GetName().Version?.ToString();

    public static string Copyright => GetAssemblyAttribute<AssemblyCopyrightAttribute>()?.Copyright;

    public static string Company => GetAssemblyAttribute<AssemblyCompanyAttribute>()?.Company;

    private static Assembly ExecutingAssembly => Assembly.GetExecutingAssembly();

    private static T GetAssemblyAttribute<T>() => ExecutingAssembly.GetCustomAttributes(typeof(T), false)
                                                                   .Cast<T>()
                                                                   .FirstOrDefault();
}

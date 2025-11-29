using System.IO;
using System.Reflection;

namespace AddyScript.Gui;

internal static class AssemblyInfo
{
    private static Assembly ExecutingAssembly => Assembly.GetExecutingAssembly();

    public static string Version => ExecutingAssembly.GetName().Version!.ToString();

    private static T GetAssemblyAttribute<T>()
    {
        var attributes = ExecutingAssembly.GetCustomAttributes(typeof(T), false);
        if (attributes.Length <= 0) return default;
        return (T)attributes[0];
    }

    public static string Title
    {
        get
        {
            var titleAttribute = GetAssemblyAttribute<AssemblyTitleAttribute>();
            return titleAttribute != null
                ? titleAttribute.Title
                : Path.GetFileNameWithoutExtension(ExecutingAssembly.Location);
        }
    }

    public static string Description
    {
        get
        {
            var descriptionAttribute = GetAssemblyAttribute<AssemblyDescriptionAttribute>();
            return descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
        }
    }

    public static string Copyright
    {
        get
        {
            var copyrightAttribute = GetAssemblyAttribute<AssemblyCopyrightAttribute>();
            return copyrightAttribute != null ? copyrightAttribute.Copyright : string.Empty;
        }
    }

    public static string Company
    {
        get
        {
            var companyAttribute = GetAssemblyAttribute<AssemblyCompanyAttribute>();
            return companyAttribute != null ? companyAttribute.Company : string.Empty;
        }
    }
}
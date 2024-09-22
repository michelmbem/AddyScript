using System.IO;
using System.Reflection;


namespace AddyScript.Interactive
{
    internal static class AssemblyInfo
    {
        internal static Assembly ExecutingAssembly
        {
            get => Assembly.GetExecutingAssembly();
        }

        internal static string Version
        {
            get => ExecutingAssembly.GetName().Version.ToString();
        }

        internal static T GetAssemblyAttribute<T>()
        {
            object[] attributes = ExecutingAssembly.GetCustomAttributes(typeof(T), false);
            if (attributes.Length <= 0) return default;
            return (T)attributes[0];
        }

        internal static string Title
        {
            get
            {
                var titleAttribute = GetAssemblyAttribute<AssemblyTitleAttribute>();
                return titleAttribute != null
                     ? titleAttribute.Title
                     : Path.GetFileNameWithoutExtension(ExecutingAssembly.Location);
            }
        }

        internal static string Description
        {
            get
            {
                var descriptionAttribute = GetAssemblyAttribute<AssemblyDescriptionAttribute>();
                return descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
            }
        }

        internal static string Copyright
        {
            get
            {
                var copyrightAttribute = GetAssemblyAttribute<AssemblyCopyrightAttribute>();
                return copyrightAttribute != null ? copyrightAttribute.Copyright : string.Empty;
            }
        }

        internal static string Company
        {
            get
            {
                var companyAttribute = GetAssemblyAttribute<AssemblyCompanyAttribute>();
                return companyAttribute != null ? companyAttribute.Company : string.Empty;
            }
        }
    }
}

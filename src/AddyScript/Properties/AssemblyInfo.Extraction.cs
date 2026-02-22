using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace AddyScript.Properties;


public static class AssemblyInfo
{
    public static string Title => GetAssemblyAttribute<AssemblyTitleAttribute>()?.Title ??
                                  Path.GetFileNameWithoutExtension(ExecutingAssembly.Location);

    public static string Description => GetAssemblyAttribute<AssemblyDescriptionAttribute>()?.Description;

    public static string Version => ExecutingAssembly.GetName().Version?.ToString();

    public static string Copyright => GetAssemblyAttribute<AssemblyCopyrightAttribute>()?.Copyright;

    public static string Company => GetAssemblyAttribute<AssemblyCompanyAttribute>()?.Company;

    public static string GitHubRepoUrl => GetAssemblyMetadata();

    public static string WikiUrl => GetAssemblyMetadata();

    private static Assembly ExecutingAssembly => Assembly.GetExecutingAssembly();

    private static T GetAssemblyAttribute<T>() where T : Attribute => ExecutingAssembly.GetCustomAttribute<T>();

    private static IEnumerable<T> GetAssemblyAttributes<T>() where T : Attribute =>
        ExecutingAssembly.GetCustomAttributes<T>();

    private static string GetAssemblyMetadata([CallerMemberName] string key = null) =>
        GetAssemblyAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)
            ?.Value;
}
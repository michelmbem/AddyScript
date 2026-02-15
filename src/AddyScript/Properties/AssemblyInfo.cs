using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


[assembly: AssemblyTitle("AddyScript")]
[assembly: AssemblyDescription("A scripting engine for the .NET platform")]
[assembly: AssemblyCompany("Addy")]
[assembly: AssemblyCopyright("\u00A9 Addy 2009-2026")]
[assembly: AssemblyVersion("1.0.1")]
[assembly: AssemblyFileVersion("1.0.1.0")]
[assembly: AssemblyMetadata("GitHubRepoUrl", "https://github.com/michelmbem/AddyScript")]
[assembly: AssemblyMetadata("WikiUrl", "https://michelmbem.github.io/AddyScript")]


namespace AddyScript.Properties;


public static class AssemblyInfo
{
    public static string Title => GetAssemblyAttribute<AssemblyTitleAttribute>()?.Title ??
                                  Path.GetFileNameWithoutExtension(ExecutingAssembly.Location);

    public static string Description => GetAssemblyAttribute<AssemblyDescriptionAttribute>()?.Description;

    public static string Version => ExecutingAssembly.GetName().Version?.ToString();

    public static string Copyright => GetAssemblyAttribute<AssemblyCopyrightAttribute>()?.Copyright;

    public static string Company => GetAssemblyAttribute<AssemblyCompanyAttribute>()?.Company;

    public static string GitHubRepoUrl => GetAssemblyMetadata(nameof(GitHubRepoUrl));

    public static string WikiUrl => GetAssemblyMetadata(nameof(WikiUrl));

    private static Assembly ExecutingAssembly => Assembly.GetExecutingAssembly();

    private static T GetAssemblyAttribute<T>() where T : Attribute => ExecutingAssembly.GetCustomAttribute<T>();

    private static IEnumerable<T> GetAssemblyAttributes<T>() where T : Attribute => ExecutingAssembly.GetCustomAttributes<T>();

    private static string GetAssemblyMetadata(string key) => GetAssemblyAttributes<AssemblyMetadataAttribute>()
                                                             .FirstOrDefault(a => a.Key == key)
                                                             ?.Value;
}
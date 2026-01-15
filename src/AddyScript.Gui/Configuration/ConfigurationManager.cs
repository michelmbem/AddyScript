using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;

namespace AddyScript.Gui.Configuration;

public record WindowSettings(WindowState State, int X, int Y, double Width, double Height, int CaretOffset);

public record ApplicationSettings(Options Options, Dictionary<string, WindowSettings> Windows);

public static class ConfigurationManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AssemblyInfo.Title,
        "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Options LoadOptions()
    {
        ApplicationSettings appSettings = LoadAllSettings();
        return appSettings.Options ?? Options.Default;
    }

    public static void SaveOptions(Options options) =>
        SaveAllSettings(LoadAllSettings() with { Options = options });

    public static WindowSettings LoadWindowSettings(string path)
    {
        ApplicationSettings appSettings = LoadAllSettings();
        appSettings.Windows.TryGetValue(path, out var window);
        return window;
    }

    public static void SaveWindowSettings(string path, WindowSettings windowSettings)
    {
        var appSettings = LoadAllSettings();
        appSettings.Windows[path] = windowSettings;
        SaveAllSettings(appSettings);
    }

    private static ApplicationSettings LoadAllSettings()
    {
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<ApplicationSettings>(json, JsonOptions);
        }

        return new ApplicationSettings(null, []);
    }

    private static void SaveAllSettings(ApplicationSettings appSettings)
    {
        string json = JsonSerializer.Serialize(appSettings, JsonOptions);
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        File.WriteAllText(ConfigPath, json);
    }
}

using Avalonia.Input;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides extension methods for working with event argument types.
/// </summary>
internal static class EventArgsExtensions
{
    /// <summary>
    /// Checks whether the given <see cref="KeyEventArgs"/> matches the expected hotkey configuration.
    /// </summary>
    /// <param name="e">The <see cref="KeyEventArgs"/> to check</param>
    /// <param name="key">The expected <see cref="Key"/> member</param>
    /// <param name="modifiers">Tells whether one or any of the Control/Alt/System/Shift keys should be pressed or not</param>
    /// <returns><b>true</b> is <paramref name="e"/> matches the configuration. <b>false</b> otherwise</returns>
    public static bool IsHotKey(this KeyEventArgs e, Key key, KeyModifiers modifiers = KeyModifiers.None) =>
        e.Key == key && (e.KeyModifiers & modifiers) == modifiers;
}
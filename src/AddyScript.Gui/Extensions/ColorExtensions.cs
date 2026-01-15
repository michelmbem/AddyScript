using Avalonia.Media;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="Color"/> type.
/// </summary>
internal static class ColorExtensions
{
    private const double DARKNESS_TRESHOLD = 0.5;

    /// <summary>
    /// Computes the luminance (i.e. brightness) of a color.
    /// </summary>
    /// <param name="color">The color for which to get luminance</param>
    /// <returns>A floatting-point value in the range [0-1], 0 for black and 1 for white</returns>
    public static double Luminance(this Color color) =>
        (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;

    /// <summary>
    /// Checks whether the given color can be cosidered as bright or not.
    /// </summary>
    /// <param name="color">The color for which to check the brightness</param>
    /// <returns>
    /// <b>true</b> if the luminance of <paramref name="color"/> is greater than or equal to <see cref="DARKNESS_TRESHOLD"/>,
    /// <b>false</b> otherwise
    /// </returns>
    public static bool IsLight(this Color color) => color.Luminance() >= DARKNESS_TRESHOLD;

    /// <summary>
    /// Checks whether the given color can be cosidered as dark or not.
    /// </summary>
    /// <param name="color">The color for which to check the darkness</param>
    /// <returns>
    /// <b>true</b> if the luminance of <paramref name="color"/> is lower than <see cref="DARKNESS_TRESHOLD"/>,
    /// <b>false</b> otherwise
    /// </returns>
    public static bool IsDark(this Color color) => color.Luminance() < DARKNESS_TRESHOLD;

    /// <summary>
    /// Mixes two colors and creates another one by blending their components.
    /// </summary>
    /// <param name="color">The first color to mix</param>
    /// <param name="other">The second color to mix</param>
    /// <param name="level">Indicates the weight of each color in the mixing</param>
    /// <returns>A <see cref="Color"/></returns>
    public static Color Mix(this Color color, Color other, double level = 0.5)
    {
        switch (level)
        {
            case >= 1:
                return color;
            case <= 0:
                return other;
            default:
            {
                var complement = 1 - level;
                var A = (byte)(complement * color.A + level * other.A);
                var R = (byte)(complement * color.R + level * other.R);
                var G = (byte)(complement * color.G + level * other.G);
                var B = (byte)(complement * color.B + level * other.B);
                return Color.FromArgb(A, R, G, B);
            }
        }
    }

    /// <summary>
    /// Creates a brigther version of a color.
    /// </summary>
    /// <param name="color">The color for which to get a brighter version</param>
    /// <param name="level">The level of brigthness</param>
    /// <returns>A mix of <paramref name="color"/> and <see cref="Colors.White"/> at the given level</returns>
    public static Color Lighter(this Color color, double level = 0.5) =>
        color.Mix(Colors.White, level);

    /// <summary>
    /// Creates a darker version of a color.
    /// </summary>
    /// <param name="color">The color for which to get a darker version</param>
    /// <param name="level">The level of darkness</param>
    /// <returns>A mix of <paramref name="color"/> and <see cref="Colors.Black"/> at the given level</returns>
    public static Color Darker(this Color color, double level = 0.5) =>
        color.Mix(Colors.Black, level);

    /// <summary>
    /// Creates a transluscent version of a color.
    /// </summary>
    /// <param name="color">The color for which to get a transluscent version</param>
    /// <param name="level">The level of transparency</param>
    /// <returns>A mix of <paramref name="color"/> and <see cref="Colors.Transparent"/> at the given level</returns>
    public static Color Transluscent(this Color color, double level = 0.5) =>
        color.Mix(Colors.Transparent, level);
}

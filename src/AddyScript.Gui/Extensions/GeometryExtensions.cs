using Avalonia;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides entension methods to some Avalonia geometry types.
/// </summary>
internal static class GeometryExtensions
{
    /// <summary>
    /// Gets a <see cref="Size"/> that suits the desired orientation.
    /// </summary>
    /// <param name="size">The <see cref="Size"/> to rotate</param>
    /// <param name="landscape">Determines whether <paramref name="size"/> should be in landscape mode or not</param>
    /// <returns>A <see cref="Size"/> rotated to match <paramref name="landscape"/></returns>
    public static Size Rotate(this Size size, bool landscape) =>
        landscape ? new Size(size.Height, size.Width) : size;

    /// <summary>
    /// Multplies an <see cref="Size"/> with a given DPI.
    /// </summary>
    /// <param name="size">The <see cref="Size"/> to multiply</param>
    /// <param name="dpi">The DPI to apply to the given size</param>
    /// <returns>A <see cref="Size"/></returns>
    public static Size Multiply(this Size size, Vector dpi) =>
        new Size(size.Width * dpi.X, size.Height * dpi.Y);

    /// <summary>
    /// Multplies an <see cref="Thickness"/> with a given DPI.
    /// </summary>
    /// <param name="thickness">The <see cref="Thickness"/> to multiply</param>
    /// <param name="dpi">The DPI to apply to the given thickness</param>
    /// <returns>A <see cref="Thickness"/></returns>
    public static Thickness Multiply(this Thickness thickness, Vector dpi) =>
        new Thickness(thickness.Left * dpi.X,
                      thickness.Top * dpi.Y,
                      thickness.Right * dpi.X,
                      thickness.Bottom * dpi.Y);
}

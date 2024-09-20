using System.Drawing;
using System.Windows.Forms;


namespace AddyScript.Gui
{
    /// <summary>
    /// A color table to customize the look of our toolbar.
    /// </summary>
    public class EditorColorTable : ProfessionalColorTable
    {
        #region Constants

        /// <summary>
        /// The window's foreground color.
        /// </summary>
        public static readonly Color WindowForeground = Color.Black;

        /// <summary>
        /// The window's background color.
        /// </summary>
        public static readonly Color WindowBackground = Color.FromArgb(248, 160, 7);

        #endregion

        #region Utility

        /// <summary>
        /// Computes the averrage of two colors.
        /// </summary>
        /// <param name="c1">The first color</param>
        /// <param name="c2">The second color</param>
        /// <returns>A <see cref="Color"/></returns>
        public static Color Middle(Color c1, Color c2)
        {
            return Color.FromArgb((c1.A + c2.A) / 2, (c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2);
        }

        /// <summary>
        /// Gets a lighter version of a color.
        /// </summary>
        /// <param name="c">The given color</param>
        /// <returns>A <see cref="Color"/></returns>
        public static Color Light(Color c)
        {
            return Middle(c, Color.White);
        }

        /// <summary>
        /// Gets a darker version of a color.
        /// </summary>
        /// <param name="c">The given color</param>
        /// <returns>A <see cref="Color"/></returns>
        public static Color Dark(Color c)
        {
            return Middle(c, Color.Black);
        }

        #endregion

        #region Override

        public override Color ToolStripGradientBegin
        {
            get { return Light(WindowBackground); }
        }

        public override Color ToolStripGradientMiddle
        {
            get { return WindowBackground; }
        }

        public override Color ToolStripGradientEnd
        {
            get { return Middle(WindowBackground, Dark(WindowBackground)); }
        }

        public override Color ToolStripBorder
        {
            get { return ToolStripGradientEnd; }
        }

        public override Color ButtonSelectedBorder
        {
            get { return WindowBackground; }
        }

        public override Color ButtonSelectedGradientBegin
        {
            get { return Light(ButtonPressedGradientBegin); }
        }

        public override Color ButtonSelectedGradientMiddle
        {
            get { return ButtonSelectedGradientBegin; }
        }

        public override Color ButtonSelectedGradientEnd
        {
            get { return ButtonSelectedGradientBegin; }
        }

        public override Color ButtonPressedGradientBegin
        {
            get { return Light(Color.Yellow); }
        }

        public override Color ButtonPressedGradientMiddle
        {
            get { return ButtonPressedGradientBegin; }
        }

        public override Color ButtonPressedGradientEnd
        {
            get { return ButtonPressedGradientBegin; }
        }

        public override Color ToolStripDropDownBackground
        {
            get { return Color.White; }
        }

        public override Color MenuBorder
        {
            get { return WindowBackground; }
        }

        public override Color MenuItemSelected
        {
            get { return ButtonSelectedGradientBegin; }
        }

        public override Color MenuItemBorder
        {
            get { return ButtonSelectedBorder; }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Light(Light(WindowBackground)); }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Light(WindowBackground); }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return WindowBackground; }
        }

        public override Color OverflowButtonGradientBegin
        {
            get { return ToolStripGradientBegin; }
        }

        public override Color OverflowButtonGradientMiddle
        {
            get { return ToolStripGradientMiddle; }
        }

        public override Color OverflowButtonGradientEnd
        {
            get { return ToolStripGradientEnd; }
        }

        public override Color SeparatorLight
        {
            get { return WindowForeground; }
        }

        public override Color SeparatorDark
        {
            get { return WindowBackground; }
        }

        #endregion
    }
}
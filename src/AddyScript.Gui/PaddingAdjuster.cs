using System.Windows.Forms;


namespace AddyScript.Gui
{
    public class PaddingAdjuster(Control target)
    {
        private readonly float leftRatio = (float)target.Padding.Left / target.Width;
        private readonly float topRatio = (float)target.Padding.Top / target.Height;
        private readonly float rightRatio = (float)target.Padding.Right / target.Width;
        private readonly float bottomRatio = (float)target.Padding.Bottom / target.Height;

        public Padding Adjust(Control target) => new(
            (int)(target.Width * leftRatio),
            (int)(target.Height * topRatio),
            (int)(target.Width * rightRatio),
            (int)(target.Height * bottomRatio)
        );
    }
}

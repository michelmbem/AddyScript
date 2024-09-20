using System.Windows.Forms;


namespace AddyScript.Gui
{
    public class PaddingAdjuster
    {
        private readonly float leftRatio;
        private readonly float topRatio;
        private readonly float rightRatio;
        private readonly float bottomRatio;

        public PaddingAdjuster(Control target)
        {
            leftRatio = (float)target.Padding.Left / target.Width;
            topRatio = (float)target.Padding.Top / target.Height;
            rightRatio = (float)target.Padding.Right / target.Width;
            bottomRatio = (float)target.Padding.Bottom / target.Height;
        }

        public Padding Adjust(Control target) => new Padding(
            (int)(target.Width * leftRatio),
            (int)(target.Height * topRatio),
            (int)(target.Width * rightRatio),
            (int)(target.Height * bottomRatio)
        );
    }
}

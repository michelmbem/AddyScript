using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using AddyScript.Plotter.Curves;
using AddyScript.Plotter.Properties;


namespace AddyScript.Plotter
{
    public partial class PlotterForm : Form
    {
        private ICurve curve;
        private Range range;
        private Window window;
        private Rectangle viewport;
        private Projector projector;
        private bool plottingEnabled;
        private int curX, curY;

        public PlotterForm()
        {
            InitializeComponent();
        }

        private bool ValidateInput()
        {
            errors.Clear();

            if (string.IsNullOrWhiteSpace(cboFunction1.Text))
            {
                errors.SetError(cboFunction1, Resources.Function1Required);
                cboFunction1.Focus();
                return false;
            }

            if (cboFunction2.Enabled && string.IsNullOrWhiteSpace(cboFunction2.Text))
            {
                errors.SetError(cboFunction2, Resources.Function1Required);
                cboFunction2.Focus();
                return false;
            }

            return true;
        }

        private void UpdateProjector()
        {
            int leftMargin = 0, bottomMargin = 0;
            if (chkAxis.Checked)
            {
                leftMargin = (int)numLeftMargin.Value / 2;
                bottomMargin = (int)numBottomMargin.Value / 2;
            }

            viewport = display.ClientRectangle;
            viewport.Inflate(-leftMargin, -bottomMargin);
            viewport.Offset(leftMargin, -bottomMargin);

            projector = new Projector(window, viewport);
        }

        private void InitPlotting()
        {
            switch (cboCurveType.SelectedIndex)
            {
                case 0:
                    curve = new CartesianCurve(new Function(cboFunction1.Text, txtParameter.Text));
                    break;
                case 1:
                    curve = new PolarCurve(new Function(cboFunction1.Text, txtParameter.Text));
                    break;
                case 2:
                    curve = new ParametricCurve(new Function(cboFunction1.Text, txtParameter.Text),
                                                new Function(cboFunction2.Text, txtParameter.Text));
                    break;
            }

            range = new Range((double)numStart.Value, (double)numEnd.Value, (double)numStep.Value);
            window = new Window((double)numLeft.Value, (double)numRight.Value, (double)numBottom.Value, (double)numTop.Value);

            UpdateProjector();
            // Just to check the correctness of the function(s):
            curve.GetPoint(range.Start);
            plottingEnabled = true;
        }

        private void MoveTo(double x, double y)
        {
            projector.Project(x, y, out curX, out curY);
        }

        private void LineTo(Graphics g, Pen p, double x, double y)
        {
            projector.Project(x, y, out int tmpX, out int tmpY);

            if (curX != int.MinValue && curY != int.MinValue && tmpX != int.MinValue && tmpY != int.MinValue)
                g.DrawLine(p, curX, curY, tmpX, tmpY);

            curX = tmpX;
            curY = tmpY;
        }

        private void DrawXLabel(Graphics g, Brush b, double x)
        {
            projector.Project(x, window.Bottom, out int boxX, out int boxY);

            SizeF size = g.MeasureString(x.ToString(), display.Font);
            var box = new RectangleF(boxX, boxY, size.Width, size.Height);

            var format = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Far
            };

            g.DrawString(x.ToString(), display.Font, b, box, format);
        }

        private void DrawYLabel(Graphics g, Brush brush, double y)
        {
            projector.Project(window.Left, y, out int boxX, out int boxY);

            SizeF size = g.MeasureString(y.ToString(), display.Font);
            var box = new RectangleF(boxX - size.Width, boxY - size.Height, size.Width, size.Height);

            var format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near
            };

            g.DrawString(y.ToString(), display.Font, brush, box, format);
        }

        private void DrawAxis(Graphics g, double dx, double dy)
        {
            using var solidPen = new Pen(display.ForeColor);
            using var dashPen = new Pen(display.ForeColor);
            using var brush = new SolidBrush(display.ForeColor);
            dashPen.DashPattern = [1F, 2F];

            foreach (double x in window.GetVerticalAxis(dx))
            {
                MoveTo(x, window.Bottom);
                LineTo(g, x == 0 ? solidPen : dashPen, x, window.Top);
                DrawXLabel(g, brush, x);
            }

            foreach (double y in window.GetHorizontalAxis(dy))
            {
                MoveTo(window.Left, y);
                LineTo(g, y == 0 ? solidPen : dashPen, window.Right, y);
                DrawYLabel(g, brush, y);
            }
        }

        private void Plot(Graphics g)
        {
            PointD pt = curve.GetPoint(range.Start);
            MoveTo(pt.X, pt.Y);

            using var newClip = new Region(viewport);
            Region oldClip = g.Clip;
            g.Clip = newClip;

            foreach (double x in range)
            {
                pt = curve.GetPoint(x);
                LineTo(g, Pens.Blue, pt.X, pt.Y);
            }

            g.Clip = oldClip;
        }

        private void PlotterForm_Load(object sender, EventArgs e)
        {
            Location = Settings.Default.WindowLocation;
            Size = Settings.Default.WindowSize;
            WindowState = Settings.Default.WindowState;

            if (Settings.Default.FunctionList1 != null)
                foreach (string function in Settings.Default.FunctionList1)
                    cboFunction1.Items.Add(function);

            if (Settings.Default.FunctionList2 != null)
                foreach (string function in Settings.Default.FunctionList2)
                    cboFunction2.Items.Add(function);

            cboCurveType.SelectedIndex = Settings.Default.CurveType;
            cboFunction1.Text = Settings.Default.Function1;
            cboFunction2.Text = Settings.Default.Function2;

            numStart.Value = Settings.Default.Start;
            numEnd.Value = Settings.Default.End;
            numStep.Value = Settings.Default.Step;
            txtParameter.Text = Settings.Default.Parameter;

            numLeft.Value = Settings.Default.Left;
            numRight.Value = Settings.Default.Right;
            numBottom.Value = Settings.Default.Bottom;
            numTop.Value = Settings.Default.Top;

            chkAxis.Checked = Settings.Default.Axis;
            numXInterval.Value = Settings.Default.XInterval;
            numYInterval.Value = Settings.Default.YInterval;
            numLeftMargin.Value = Settings.Default.LeftMargin;
            numBottomMargin.Value = Settings.Default.BottomMargin;
        }

        private void PlotterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var functionList1 = new StringCollection();
            foreach (string function in cboFunction1.Items)
                functionList1.Add(function);
            Settings.Default.FunctionList1 = functionList1;

            var functionList2 = new StringCollection();
            foreach (string function in cboFunction2.Items)
                functionList2.Add(function);
            Settings.Default.FunctionList2 = functionList2;

            Settings.Default.CurveType = cboCurveType.SelectedIndex;
            Settings.Default.Function1 = cboFunction1.Text;
            Settings.Default.Function2 = cboFunction2.Text;

            Settings.Default.Start = numStart.Value;
            Settings.Default.End = numEnd.Value;
            Settings.Default.Step = numStep.Value;
            Settings.Default.Parameter = txtParameter.Text;

            Settings.Default.Left = numLeft.Value;
            Settings.Default.Right = numRight.Value;
            Settings.Default.Bottom = numBottom.Value;
            Settings.Default.Top = numTop.Value;

            Settings.Default.Axis = chkAxis.Checked;
            Settings.Default.XInterval = numXInterval.Value;
            Settings.Default.YInterval = numYInterval.Value;
            Settings.Default.LeftMargin = numLeftMargin.Value;
            Settings.Default.BottomMargin = numBottomMargin.Value;

            switch (WindowState)
            {
                case FormWindowState.Normal:
                    Settings.Default.WindowState = WindowState;
                    Settings.Default.WindowLocation = Location;
                    Settings.Default.WindowSize = Size;
                    break;
                case FormWindowState.Maximized:
                    Settings.Default.WindowState = WindowState;
                    break;
            }

            Settings.Default.Save();
        }

        private void display_Paint(object sender, PaintEventArgs e)
        {
            if (!plottingEnabled) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (chkAxis.Checked) DrawAxis(e.Graphics, (double)numXInterval.Value, (double)numYInterval.Value);
            Plot(e.Graphics);
        }

        private void display_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateProjector();
                display.Invalidate();
            }
            catch
            {
                plottingEnabled = false;
            }
        }

        private void cboCurveType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCurveType.SelectedIndex > 1)
                cboFunction2.Enabled = true;
            else
            {
                cboFunction2.SelectedIndex = -1;
                cboFunction2.Enabled = false;
            }
        }

        private void chkAxis_CheckedChanged(object sender, EventArgs e)
        {
            numXInterval.Enabled = numYInterval.Enabled
                                 = numLeftMargin.Enabled
                                 = numBottomMargin.Enabled
                                 = chkAxis.Checked;
        }

        private void btnPlot_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                InitPlotting();
                display.Invalidate();
                if (!cboFunction1.Items.Contains(cboFunction1.Text))
                    cboFunction1.Items.Add(cboFunction1.Text);
                if (cboFunction2.Enabled && !cboFunction2.Items.Contains(cboFunction2.Text))
                    cboFunction2.Items.Add(cboFunction2.Text);
            }
            catch (Exception ex)
            {
                plottingEnabled = false;
                MessageBox.Show(ex.Message, Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            plottingEnabled = false;
            display.Invalidate();
        }

        private void picLogo_Click(object sender, EventArgs e)
        {
            using var about = new AboutBox();
            about.ShowDialog(this);
        }
    }
}

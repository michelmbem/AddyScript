namespace Plotter
{
    partial class PlotterForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlotterForm));
            this.table = new System.Windows.Forms.TableLayoutPanel();
            this.settingsPane = new System.Windows.Forms.Panel();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.grpAxis = new System.Windows.Forms.GroupBox();
            this.chkAxis = new System.Windows.Forms.CheckBox();
            this.numBottomMargin = new System.Windows.Forms.NumericUpDown();
            this.numYInterval = new System.Windows.Forms.NumericUpDown();
            this.numLeftMargin = new System.Windows.Forms.NumericUpDown();
            this.numXInterval = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.grpWindow = new System.Windows.Forms.GroupBox();
            this.numTop = new System.Windows.Forms.NumericUpDown();
            this.numRight = new System.Windows.Forms.NumericUpDown();
            this.numBottom = new System.Windows.Forms.NumericUpDown();
            this.numLeft = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cboFunction2 = new System.Windows.Forms.ComboBox();
            this.cboCurveType = new System.Windows.Forms.ComboBox();
            this.cboFunction1 = new System.Windows.Forms.ComboBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnPlot = new System.Windows.Forms.Button();
            this.lblFunction2 = new System.Windows.Forms.Label();
            this.grpRange = new System.Windows.Forms.GroupBox();
            this.txtParameter = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.numStep = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.numEnd = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numStart = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.lblCurveType = new System.Windows.Forms.Label();
            this.lblFunction1 = new System.Windows.Forms.Label();
            this.display = new System.Windows.Forms.Panel();
            this.errors = new System.Windows.Forms.ErrorProvider(this.components);
            this.table.SuspendLayout();
            this.settingsPane.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.picLogo)).BeginInit();
            this.grpAxis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.numBottomMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numYInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numLeftMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numXInterval)).BeginInit();
            this.grpWindow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.numTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numLeft)).BeginInit();
            this.grpRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.numStep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numEnd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.numStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.errors)).BeginInit();
            this.SuspendLayout();
            // 
            // table
            // 
            resources.ApplyResources(this.table, "table");
            this.table.Controls.Add(this.settingsPane, 1, 0);
            this.table.Controls.Add(this.display, 0, 0);
            this.table.Name = "table";
            // 
            // settingsPane
            // 
            this.settingsPane.Controls.Add(this.picLogo);
            this.settingsPane.Controls.Add(this.grpAxis);
            this.settingsPane.Controls.Add(this.grpWindow);
            this.settingsPane.Controls.Add(this.cboFunction2);
            this.settingsPane.Controls.Add(this.cboCurveType);
            this.settingsPane.Controls.Add(this.cboFunction1);
            this.settingsPane.Controls.Add(this.btnClear);
            this.settingsPane.Controls.Add(this.btnPlot);
            this.settingsPane.Controls.Add(this.lblFunction2);
            this.settingsPane.Controls.Add(this.grpRange);
            this.settingsPane.Controls.Add(this.lblCurveType);
            this.settingsPane.Controls.Add(this.lblFunction1);
            resources.ApplyResources(this.settingsPane, "settingsPane");
            this.settingsPane.Name = "settingsPane";
            // 
            // picLogo
            // 
            resources.ApplyResources(this.picLogo, "picLogo");
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picLogo.Name = "picLogo";
            this.picLogo.TabStop = false;
            this.picLogo.Click += new System.EventHandler(this.picLogo_Click);
            // 
            // grpAxis
            // 
            resources.ApplyResources(this.grpAxis, "grpAxis");
            this.grpAxis.Controls.Add(this.chkAxis);
            this.grpAxis.Controls.Add(this.numBottomMargin);
            this.grpAxis.Controls.Add(this.numYInterval);
            this.grpAxis.Controls.Add(this.numLeftMargin);
            this.grpAxis.Controls.Add(this.numXInterval);
            this.grpAxis.Controls.Add(this.label10);
            this.grpAxis.Controls.Add(this.label11);
            this.grpAxis.Controls.Add(this.label12);
            this.grpAxis.Controls.Add(this.label13);
            this.grpAxis.Name = "grpAxis";
            this.grpAxis.TabStop = false;
            // 
            // chkAxis
            // 
            resources.ApplyResources(this.chkAxis, "chkAxis");
            this.chkAxis.Name = "chkAxis";
            this.chkAxis.UseVisualStyleBackColor = true;
            this.chkAxis.CheckedChanged += new System.EventHandler(this.chkAxis_CheckedChanged);
            // 
            // numBottomMargin
            // 
            resources.ApplyResources(this.numBottomMargin, "numBottomMargin");
            this.numBottomMargin.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numBottomMargin.Name = "numBottomMargin";
            this.numBottomMargin.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // numYInterval
            // 
            resources.ApplyResources(this.numYInterval, "numYInterval");
            this.numYInterval.DecimalPlaces = 2;
            this.numYInterval.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numYInterval.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numYInterval.Name = "numYInterval";
            this.numYInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numLeftMargin
            // 
            resources.ApplyResources(this.numLeftMargin, "numLeftMargin");
            this.numLeftMargin.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numLeftMargin.Name = "numLeftMargin";
            this.numLeftMargin.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // numXInterval
            // 
            this.numXInterval.DecimalPlaces = 2;
            this.numXInterval.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            resources.ApplyResources(this.numXInterval, "numXInterval");
            this.numXInterval.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numXInterval.Name = "numXInterval";
            this.numXInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            // 
            // grpWindow
            // 
            resources.ApplyResources(this.grpWindow, "grpWindow");
            this.grpWindow.Controls.Add(this.numTop);
            this.grpWindow.Controls.Add(this.numRight);
            this.grpWindow.Controls.Add(this.numBottom);
            this.grpWindow.Controls.Add(this.numLeft);
            this.grpWindow.Controls.Add(this.label8);
            this.grpWindow.Controls.Add(this.label7);
            this.grpWindow.Controls.Add(this.label6);
            this.grpWindow.Controls.Add(this.label5);
            this.grpWindow.Name = "grpWindow";
            this.grpWindow.TabStop = false;
            // 
            // numTop
            // 
            resources.ApplyResources(this.numTop, "numTop");
            this.numTop.DecimalPlaces = 2;
            this.numTop.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numTop.Minimum = new decimal(new int[] {
            1000000000,
            0,
            0,
            -2147483648});
            this.numTop.Name = "numTop";
            this.numTop.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // numRight
            // 
            resources.ApplyResources(this.numRight, "numRight");
            this.numRight.DecimalPlaces = 2;
            this.numRight.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numRight.Minimum = new decimal(new int[] {
            1000000000,
            0,
            0,
            -2147483648});
            this.numRight.Name = "numRight";
            this.numRight.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // numBottom
            // 
            this.numBottom.DecimalPlaces = 2;
            resources.ApplyResources(this.numBottom, "numBottom");
            this.numBottom.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numBottom.Minimum = new decimal(new int[] {
            1000000000,
            0,
            0,
            -2147483648});
            this.numBottom.Name = "numBottom";
            this.numBottom.Value = new decimal(new int[] {
            4,
            0,
            0,
            -2147483648});
            // 
            // numLeft
            // 
            this.numLeft.DecimalPlaces = 2;
            resources.ApplyResources(this.numLeft, "numLeft");
            this.numLeft.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numLeft.Minimum = new decimal(new int[] {
            1000000000,
            0,
            0,
            -2147483648});
            this.numLeft.Name = "numLeft";
            this.numLeft.Value = new decimal(new int[] {
            4,
            0,
            0,
            -2147483648});
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // cboFunction2
            // 
            resources.ApplyResources(this.cboFunction2, "cboFunction2");
            this.cboFunction2.FormattingEnabled = true;
            this.cboFunction2.Name = "cboFunction2";
            // 
            // cboCurveType
            // 
            resources.ApplyResources(this.cboCurveType, "cboCurveType");
            this.cboCurveType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCurveType.FormattingEnabled = true;
            this.cboCurveType.Items.AddRange(new object[] {
            resources.GetString("cboCurveType.Items"),
            resources.GetString("cboCurveType.Items1"),
            resources.GetString("cboCurveType.Items2")});
            this.cboCurveType.Name = "cboCurveType";
            this.cboCurveType.SelectedIndexChanged += new System.EventHandler(this.cboCurveType_SelectedIndexChanged);
            // 
            // cboFunction1
            // 
            resources.ApplyResources(this.cboFunction1, "cboFunction1");
            this.cboFunction1.FormattingEnabled = true;
            this.cboFunction1.Name = "cboFunction1";
            // 
            // btnClear
            // 
            resources.ApplyResources(this.btnClear, "btnClear");
            this.btnClear.Name = "btnClear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnPlot
            // 
            resources.ApplyResources(this.btnPlot, "btnPlot");
            this.btnPlot.Name = "btnPlot";
            this.btnPlot.UseVisualStyleBackColor = true;
            this.btnPlot.Click += new System.EventHandler(this.btnPlot_Click);
            // 
            // lblFunction2
            // 
            resources.ApplyResources(this.lblFunction2, "lblFunction2");
            this.lblFunction2.Name = "lblFunction2";
            // 
            // grpRange
            // 
            resources.ApplyResources(this.grpRange, "grpRange");
            this.grpRange.Controls.Add(this.txtParameter);
            this.grpRange.Controls.Add(this.label9);
            this.grpRange.Controls.Add(this.numStep);
            this.grpRange.Controls.Add(this.label4);
            this.grpRange.Controls.Add(this.numEnd);
            this.grpRange.Controls.Add(this.label3);
            this.grpRange.Controls.Add(this.numStart);
            this.grpRange.Controls.Add(this.label2);
            this.grpRange.Name = "grpRange";
            this.grpRange.TabStop = false;
            // 
            // txtParameter
            // 
            resources.ApplyResources(this.txtParameter, "txtParameter");
            this.txtParameter.Name = "txtParameter";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // numStep
            // 
            this.numStep.DecimalPlaces = 3;
            this.numStep.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            resources.ApplyResources(this.numStep, "numStep");
            this.numStep.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numStep.Name = "numStep";
            this.numStep.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // numEnd
            // 
            resources.ApplyResources(this.numEnd, "numEnd");
            this.numEnd.DecimalPlaces = 2;
            this.numEnd.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numEnd.Minimum = new decimal(new int[] {
            1000000000,
            0,
            0,
            -2147483648});
            this.numEnd.Name = "numEnd";
            this.numEnd.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // numStart
            // 
            this.numStart.DecimalPlaces = 2;
            resources.ApplyResources(this.numStart, "numStart");
            this.numStart.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numStart.Minimum = new decimal(new int[] {
            1000000000,
            0,
            0,
            -2147483648});
            this.numStart.Name = "numStart";
            this.numStart.Value = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // lblCurveType
            // 
            resources.ApplyResources(this.lblCurveType, "lblCurveType");
            this.lblCurveType.Name = "lblCurveType";
            // 
            // lblFunction1
            // 
            resources.ApplyResources(this.lblFunction1, "lblFunction1");
            this.lblFunction1.Name = "lblFunction1";
            // 
            // display
            // 
            this.display.BackColor = System.Drawing.Color.Gray;
            this.display.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.display, "display");
            this.display.ForeColor = System.Drawing.Color.White;
            this.display.Name = "display";
            this.display.Paint += new System.Windows.Forms.PaintEventHandler(this.display_Paint);
            this.display.SizeChanged += new System.EventHandler(this.display_SizeChanged);
            // 
            // errors
            // 
            this.errors.ContainerControl = this;
            // 
            // PlotterForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.table);
            this.Name = "PlotterForm";
            this.Load += new System.EventHandler(this.PlotterForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PlotterForm_FormClosing);
            this.table.ResumeLayout(false);
            this.settingsPane.ResumeLayout(false);
            this.settingsPane.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.picLogo)).EndInit();
            this.grpAxis.ResumeLayout(false);
            this.grpAxis.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.numBottomMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numYInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numLeftMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numXInterval)).EndInit();
            this.grpWindow.ResumeLayout(false);
            this.grpWindow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.numTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numLeft)).EndInit();
            this.grpRange.ResumeLayout(false);
            this.grpRange.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.numStep)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numEnd)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.numStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.errors)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel table;
        private System.Windows.Forms.Panel display;
        private System.Windows.Forms.Panel settingsPane;
        private System.Windows.Forms.GroupBox grpWindow;
        private System.Windows.Forms.NumericUpDown numTop;
        private System.Windows.Forms.NumericUpDown numRight;
        private System.Windows.Forms.NumericUpDown numBottom;
        private System.Windows.Forms.NumericUpDown numLeft;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboFunction1;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnPlot;
        private System.Windows.Forms.GroupBox grpRange;
        private System.Windows.Forms.NumericUpDown numStep;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numEnd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numStart;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblFunction1;
        private System.Windows.Forms.ErrorProvider errors;
        private System.Windows.Forms.TextBox txtParameter;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox grpAxis;
        private System.Windows.Forms.NumericUpDown numBottomMargin;
        private System.Windows.Forms.NumericUpDown numYInterval;
        private System.Windows.Forms.NumericUpDown numLeftMargin;
        private System.Windows.Forms.NumericUpDown numXInterval;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.CheckBox chkAxis;
        private System.Windows.Forms.ComboBox cboFunction2;
        private System.Windows.Forms.ComboBox cboCurveType;
        private System.Windows.Forms.Label lblFunction2;
        private System.Windows.Forms.Label lblCurveType;
        private System.Windows.Forms.PictureBox picLogo;

    }
}


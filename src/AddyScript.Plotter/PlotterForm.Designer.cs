namespace AddyScript.Plotter
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlotterForm));
            table = new System.Windows.Forms.TableLayoutPanel();
            settingsPane = new System.Windows.Forms.Panel();
            picLogo = new System.Windows.Forms.PictureBox();
            grpAxis = new System.Windows.Forms.GroupBox();
            chkAxis = new System.Windows.Forms.CheckBox();
            numBottomMargin = new System.Windows.Forms.NumericUpDown();
            numYInterval = new System.Windows.Forms.NumericUpDown();
            numLeftMargin = new System.Windows.Forms.NumericUpDown();
            numXInterval = new System.Windows.Forms.NumericUpDown();
            label10 = new System.Windows.Forms.Label();
            label11 = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            grpWindow = new System.Windows.Forms.GroupBox();
            numTop = new System.Windows.Forms.NumericUpDown();
            numRight = new System.Windows.Forms.NumericUpDown();
            numBottom = new System.Windows.Forms.NumericUpDown();
            numLeft = new System.Windows.Forms.NumericUpDown();
            label8 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            cboFunction2 = new System.Windows.Forms.ComboBox();
            cboCurveType = new System.Windows.Forms.ComboBox();
            cboFunction1 = new System.Windows.Forms.ComboBox();
            btnClear = new System.Windows.Forms.Button();
            btnPlot = new System.Windows.Forms.Button();
            lblFunction2 = new System.Windows.Forms.Label();
            grpRange = new System.Windows.Forms.GroupBox();
            txtParameter = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            numStep = new System.Windows.Forms.NumericUpDown();
            label4 = new System.Windows.Forms.Label();
            numEnd = new System.Windows.Forms.NumericUpDown();
            label3 = new System.Windows.Forms.Label();
            numStart = new System.Windows.Forms.NumericUpDown();
            label2 = new System.Windows.Forms.Label();
            lblCurveType = new System.Windows.Forms.Label();
            lblFunction1 = new System.Windows.Forms.Label();
            display = new System.Windows.Forms.Panel();
            errors = new System.Windows.Forms.ErrorProvider(components);
            table.SuspendLayout();
            settingsPane.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            grpAxis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numBottomMargin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numYInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLeftMargin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numXInterval).BeginInit();
            grpWindow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numTop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numBottom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLeft).BeginInit();
            grpRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEnd).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)errors).BeginInit();
            SuspendLayout();
            // 
            // table
            // 
            resources.ApplyResources(table, "table");
            table.Controls.Add(settingsPane, 1, 0);
            table.Controls.Add(display, 0, 0);
            table.Name = "table";
            // 
            // settingsPane
            // 
            settingsPane.Controls.Add(picLogo);
            settingsPane.Controls.Add(grpAxis);
            settingsPane.Controls.Add(grpWindow);
            settingsPane.Controls.Add(cboFunction2);
            settingsPane.Controls.Add(cboCurveType);
            settingsPane.Controls.Add(cboFunction1);
            settingsPane.Controls.Add(btnClear);
            settingsPane.Controls.Add(btnPlot);
            settingsPane.Controls.Add(lblFunction2);
            settingsPane.Controls.Add(grpRange);
            settingsPane.Controls.Add(lblCurveType);
            settingsPane.Controls.Add(lblFunction1);
            resources.ApplyResources(settingsPane, "settingsPane");
            settingsPane.Name = "settingsPane";
            // 
            // picLogo
            // 
            resources.ApplyResources(picLogo, "picLogo");
            picLogo.BackColor = System.Drawing.Color.Transparent;
            picLogo.Cursor = System.Windows.Forms.Cursors.Hand;
            picLogo.Name = "picLogo";
            picLogo.TabStop = false;
            picLogo.Click += picLogo_Click;
            // 
            // grpAxis
            // 
            resources.ApplyResources(grpAxis, "grpAxis");
            grpAxis.Controls.Add(chkAxis);
            grpAxis.Controls.Add(numBottomMargin);
            grpAxis.Controls.Add(numYInterval);
            grpAxis.Controls.Add(numLeftMargin);
            grpAxis.Controls.Add(numXInterval);
            grpAxis.Controls.Add(label10);
            grpAxis.Controls.Add(label11);
            grpAxis.Controls.Add(label12);
            grpAxis.Controls.Add(label13);
            grpAxis.Name = "grpAxis";
            grpAxis.TabStop = false;
            // 
            // chkAxis
            // 
            resources.ApplyResources(chkAxis, "chkAxis");
            chkAxis.Name = "chkAxis";
            chkAxis.UseVisualStyleBackColor = true;
            chkAxis.CheckedChanged += chkAxis_CheckedChanged;
            // 
            // numBottomMargin
            // 
            resources.ApplyResources(numBottomMargin, "numBottomMargin");
            numBottomMargin.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numBottomMargin.Name = "numBottomMargin";
            numBottomMargin.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // numYInterval
            // 
            resources.ApplyResources(numYInterval, "numYInterval");
            numYInterval.DecimalPlaces = 2;
            numYInterval.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numYInterval.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numYInterval.Name = "numYInterval";
            numYInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // numLeftMargin
            // 
            resources.ApplyResources(numLeftMargin, "numLeftMargin");
            numLeftMargin.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numLeftMargin.Name = "numLeftMargin";
            numLeftMargin.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // numXInterval
            // 
            numXInterval.DecimalPlaces = 2;
            numXInterval.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            resources.ApplyResources(numXInterval, "numXInterval");
            numXInterval.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numXInterval.Name = "numXInterval";
            numXInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label10
            // 
            resources.ApplyResources(label10, "label10");
            label10.Name = "label10";
            // 
            // label11
            // 
            resources.ApplyResources(label11, "label11");
            label11.Name = "label11";
            // 
            // label12
            // 
            resources.ApplyResources(label12, "label12");
            label12.Name = "label12";
            // 
            // label13
            // 
            resources.ApplyResources(label13, "label13");
            label13.Name = "label13";
            // 
            // grpWindow
            // 
            resources.ApplyResources(grpWindow, "grpWindow");
            grpWindow.Controls.Add(numTop);
            grpWindow.Controls.Add(numRight);
            grpWindow.Controls.Add(numBottom);
            grpWindow.Controls.Add(numLeft);
            grpWindow.Controls.Add(label8);
            grpWindow.Controls.Add(label7);
            grpWindow.Controls.Add(label6);
            grpWindow.Controls.Add(label5);
            grpWindow.Name = "grpWindow";
            grpWindow.TabStop = false;
            // 
            // numTop
            // 
            resources.ApplyResources(numTop, "numTop");
            numTop.DecimalPlaces = 2;
            numTop.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numTop.Minimum = new decimal(new int[] { 1000000000, 0, 0, int.MinValue });
            numTop.Name = "numTop";
            numTop.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // numRight
            // 
            resources.ApplyResources(numRight, "numRight");
            numRight.DecimalPlaces = 2;
            numRight.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numRight.Minimum = new decimal(new int[] { 1000000000, 0, 0, int.MinValue });
            numRight.Name = "numRight";
            numRight.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // numBottom
            // 
            numBottom.DecimalPlaces = 2;
            resources.ApplyResources(numBottom, "numBottom");
            numBottom.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numBottom.Minimum = new decimal(new int[] { 1000000000, 0, 0, int.MinValue });
            numBottom.Name = "numBottom";
            numBottom.Value = new decimal(new int[] { 4, 0, 0, int.MinValue });
            // 
            // numLeft
            // 
            numLeft.DecimalPlaces = 2;
            resources.ApplyResources(numLeft, "numLeft");
            numLeft.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numLeft.Minimum = new decimal(new int[] { 1000000000, 0, 0, int.MinValue });
            numLeft.Name = "numLeft";
            numLeft.Value = new decimal(new int[] { 4, 0, 0, int.MinValue });
            // 
            // label8
            // 
            resources.ApplyResources(label8, "label8");
            label8.Name = "label8";
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.Name = "label6";
            // 
            // label5
            // 
            resources.ApplyResources(label5, "label5");
            label5.Name = "label5";
            // 
            // cboFunction2
            // 
            resources.ApplyResources(cboFunction2, "cboFunction2");
            cboFunction2.FormattingEnabled = true;
            cboFunction2.Name = "cboFunction2";
            // 
            // cboCurveType
            // 
            resources.ApplyResources(cboCurveType, "cboCurveType");
            cboCurveType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboCurveType.FormattingEnabled = true;
            cboCurveType.Items.AddRange(new object[] { resources.GetString("cboCurveType.Items"), resources.GetString("cboCurveType.Items1"), resources.GetString("cboCurveType.Items2") });
            cboCurveType.Name = "cboCurveType";
            cboCurveType.SelectedIndexChanged += cboCurveType_SelectedIndexChanged;
            // 
            // cboFunction1
            // 
            resources.ApplyResources(cboFunction1, "cboFunction1");
            cboFunction1.FormattingEnabled = true;
            cboFunction1.Name = "cboFunction1";
            // 
            // btnClear
            // 
            resources.ApplyResources(btnClear, "btnClear");
            btnClear.Name = "btnClear";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // btnPlot
            // 
            resources.ApplyResources(btnPlot, "btnPlot");
            btnPlot.Name = "btnPlot";
            btnPlot.UseVisualStyleBackColor = true;
            btnPlot.Click += btnPlot_Click;
            // 
            // lblFunction2
            // 
            resources.ApplyResources(lblFunction2, "lblFunction2");
            lblFunction2.Name = "lblFunction2";
            // 
            // grpRange
            // 
            resources.ApplyResources(grpRange, "grpRange");
            grpRange.Controls.Add(txtParameter);
            grpRange.Controls.Add(label9);
            grpRange.Controls.Add(numStep);
            grpRange.Controls.Add(label4);
            grpRange.Controls.Add(numEnd);
            grpRange.Controls.Add(label3);
            grpRange.Controls.Add(numStart);
            grpRange.Controls.Add(label2);
            grpRange.Name = "grpRange";
            grpRange.TabStop = false;
            // 
            // txtParameter
            // 
            resources.ApplyResources(txtParameter, "txtParameter");
            txtParameter.Name = "txtParameter";
            // 
            // label9
            // 
            resources.ApplyResources(label9, "label9");
            label9.Name = "label9";
            // 
            // numStep
            // 
            numStep.DecimalPlaces = 3;
            numStep.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            resources.ApplyResources(numStep, "numStep");
            numStep.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numStep.Name = "numStep";
            numStep.Value = new decimal(new int[] { 1, 0, 0, 65536 });
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.Name = "label4";
            // 
            // numEnd
            // 
            resources.ApplyResources(numEnd, "numEnd");
            numEnd.DecimalPlaces = 2;
            numEnd.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numEnd.Minimum = new decimal(new int[] { 1000000000, 0, 0, int.MinValue });
            numEnd.Name = "numEnd";
            numEnd.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // numStart
            // 
            numStart.DecimalPlaces = 2;
            resources.ApplyResources(numStart, "numStart");
            numStart.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            numStart.Minimum = new decimal(new int[] { 1000000000, 0, 0, int.MinValue });
            numStart.Name = "numStart";
            numStart.Value = new decimal(new int[] { 10, 0, 0, int.MinValue });
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // lblCurveType
            // 
            resources.ApplyResources(lblCurveType, "lblCurveType");
            lblCurveType.Name = "lblCurveType";
            // 
            // lblFunction1
            // 
            resources.ApplyResources(lblFunction1, "lblFunction1");
            lblFunction1.Name = "lblFunction1";
            // 
            // display
            // 
            display.BackColor = System.Drawing.Color.Gray;
            display.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(display, "display");
            display.ForeColor = System.Drawing.Color.White;
            display.Name = "display";
            display.SizeChanged += display_SizeChanged;
            display.Paint += display_Paint;
            // 
            // errors
            // 
            errors.ContainerControl = this;
            // 
            // PlotterForm
            // 
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            resources.ApplyResources(this, "$this");
            Controls.Add(table);
            Name = "PlotterForm";
            FormClosing += PlotterForm_FormClosing;
            Load += PlotterForm_Load;
            table.ResumeLayout(false);
            settingsPane.ResumeLayout(false);
            settingsPane.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            grpAxis.ResumeLayout(false);
            grpAxis.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numBottomMargin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numYInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLeftMargin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numXInterval).EndInit();
            grpWindow.ResumeLayout(false);
            grpWindow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numTop).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRight).EndInit();
            ((System.ComponentModel.ISupportInitialize)numBottom).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLeft).EndInit();
            grpRange.ResumeLayout(false);
            grpRange.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEnd).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)errors).EndInit();
            ResumeLayout(false);
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


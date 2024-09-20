namespace AddyScript.Gui
{
    partial class AboutBox
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
            picLogo = new System.Windows.Forms.PictureBox();
            btnOK = new System.Windows.Forms.Button();
            lblDescription = new System.Windows.Forms.Label();
            lblCopyright = new System.Windows.Forms.Label();
            lblVersion = new System.Windows.Forms.Label();
            lnkWebsite = new System.Windows.Forms.LinkLabel();
            pnlBlack = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            SuspendLayout();
            // 
            // picLogo
            // 
            resources.ApplyResources(picLogo, "picLogo");
            picLogo.Name = "picLogo";
            picLogo.TabStop = false;
            // 
            // btnOK
            // 
            resources.ApplyResources(btnOK, "btnOK");
            btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOK.Name = "btnOK";
            btnOK.UseVisualStyleBackColor = false;
            // 
            // lblDescription
            // 
            resources.ApplyResources(lblDescription, "lblDescription");
            lblDescription.Name = "lblDescription";
            // 
            // lblCopyright
            // 
            resources.ApplyResources(lblCopyright, "lblCopyright");
            lblCopyright.Name = "lblCopyright";
            // 
            // lblVersion
            // 
            resources.ApplyResources(lblVersion, "lblVersion");
            lblVersion.Name = "lblVersion";
            // 
            // lnkWebsite
            // 
            resources.ApplyResources(lnkWebsite, "lnkWebsite");
            lnkWebsite.Name = "lnkWebsite";
            lnkWebsite.TabStop = true;
            lnkWebsite.LinkClicked += linkLabel1_LinkClicked;
            // 
            // pnlBlack
            // 
            pnlBlack.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(pnlBlack, "pnlBlack");
            pnlBlack.Name = "pnlBlack";
            // 
            // AboutBox
            // 
            AcceptButton = btnOK;
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            resources.ApplyResources(this, "$this");
            Controls.Add(pnlBlack);
            Controls.Add(lnkWebsite);
            Controls.Add(lblCopyright);
            Controls.Add(lblVersion);
            Controls.Add(lblDescription);
            Controls.Add(btnOK);
            Controls.Add(picLogo);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutBox";
            ShowIcon = false;
            ShowInTaskbar = false;
            Load += AboutBox_Load;
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblCopyright;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.LinkLabel lnkWebsite;
        private System.Windows.Forms.Panel pnlBlack;
    }
}
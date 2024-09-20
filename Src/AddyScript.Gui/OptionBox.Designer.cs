namespace AddyScript.Gui
{
    partial class OptionBox
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionBox));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.searchPathTabPage = new System.Windows.Forms.TabPage();
            this.directoryList = new System.Windows.Forms.ListBox();
            this.deleteDirectoryButton = new System.Windows.Forms.Button();
            this.addDirectoryButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.referencesTabPage = new System.Windows.Forms.TabPage();
            this.assemblyTextBox = new System.Windows.Forms.TextBox();
            this.assemblyList = new System.Windows.Forms.ListBox();
            this.deleteAssemblyButton = new System.Windows.Forms.Button();
            this.addAssemblyButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.bottomPane = new System.Windows.Forms.Panel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.searchPathTabPage.SuspendLayout();
            this.referencesTabPage.SuspendLayout();
            this.bottomPane.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.searchPathTabPage);
            this.tabControl.Controls.Add(this.referencesTabPage);
            resources.ApplyResources(this.tabControl, "tabControl");
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            // 
            // searchPathTabPage
            // 
            this.searchPathTabPage.Controls.Add(this.directoryList);
            this.searchPathTabPage.Controls.Add(this.deleteDirectoryButton);
            this.searchPathTabPage.Controls.Add(this.addDirectoryButton);
            this.searchPathTabPage.Controls.Add(this.label1);
            resources.ApplyResources(this.searchPathTabPage, "searchPathTabPage");
            this.searchPathTabPage.Name = "searchPathTabPage";
            this.searchPathTabPage.UseVisualStyleBackColor = true;
            // 
            // directoryList
            // 
            resources.ApplyResources(this.directoryList, "directoryList");
            this.directoryList.FormattingEnabled = true;
            this.directoryList.Name = "directoryList";
            // 
            // deleteDirectoryButton
            // 
            resources.ApplyResources(this.deleteDirectoryButton, "deleteDirectoryButton");
            this.deleteDirectoryButton.Name = "deleteDirectoryButton";
            this.deleteDirectoryButton.UseVisualStyleBackColor = true;
            this.deleteDirectoryButton.Click += new System.EventHandler(this.deleteDirectoryButton_Click);
            // 
            // addDirectoryButton
            // 
            resources.ApplyResources(this.addDirectoryButton, "addDirectoryButton");
            this.addDirectoryButton.Name = "addDirectoryButton";
            this.addDirectoryButton.UseVisualStyleBackColor = true;
            this.addDirectoryButton.Click += new System.EventHandler(this.addDirectoryButton_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // referencesTabPage
            // 
            this.referencesTabPage.Controls.Add(this.assemblyTextBox);
            this.referencesTabPage.Controls.Add(this.assemblyList);
            this.referencesTabPage.Controls.Add(this.deleteAssemblyButton);
            this.referencesTabPage.Controls.Add(this.addAssemblyButton);
            this.referencesTabPage.Controls.Add(this.label2);
            resources.ApplyResources(this.referencesTabPage, "referencesTabPage");
            this.referencesTabPage.Name = "referencesTabPage";
            this.referencesTabPage.UseVisualStyleBackColor = true;
            // 
            // assemblyTextBox
            // 
            resources.ApplyResources(this.assemblyTextBox, "assemblyTextBox");
            this.assemblyTextBox.Name = "assemblyTextBox";
            // 
            // assemblyList
            // 
            resources.ApplyResources(this.assemblyList, "assemblyList");
            this.assemblyList.FormattingEnabled = true;
            this.assemblyList.Name = "assemblyList";
            // 
            // deleteAssemblyButton
            // 
            resources.ApplyResources(this.deleteAssemblyButton, "deleteAssemblyButton");
            this.deleteAssemblyButton.Name = "deleteAssemblyButton";
            this.deleteAssemblyButton.UseVisualStyleBackColor = true;
            this.deleteAssemblyButton.Click += new System.EventHandler(this.deleteAssemblyButton_Click);
            // 
            // addAssemblyButton
            // 
            resources.ApplyResources(this.addAssemblyButton, "addAssemblyButton");
            this.addAssemblyButton.Name = "addAssemblyButton";
            this.addAssemblyButton.UseVisualStyleBackColor = true;
            this.addAssemblyButton.Click += new System.EventHandler(this.addAssemblyButton_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // bottomPane
            // 
            this.bottomPane.Controls.Add(this.cancelButton);
            this.bottomPane.Controls.Add(this.okButton);
            resources.ApplyResources(this.bottomPane, "bottomPane");
            this.bottomPane.Name = "bottomPane";
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // OptionBox
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.cancelButton;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.bottomPane);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionBox";
            this.ShowInTaskbar = false;
            this.Load += new System.EventHandler(this.OptionBox_Load);
            this.tabControl.ResumeLayout(false);
            this.searchPathTabPage.ResumeLayout(false);
            this.searchPathTabPage.PerformLayout();
            this.referencesTabPage.ResumeLayout(false);
            this.referencesTabPage.PerformLayout();
            this.bottomPane.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage searchPathTabPage;
        private System.Windows.Forms.TabPage referencesTabPage;
        private System.Windows.Forms.Panel bottomPane;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button deleteDirectoryButton;
        private System.Windows.Forms.Button addDirectoryButton;
        private System.Windows.Forms.ListBox directoryList;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button deleteAssemblyButton;
        private System.Windows.Forms.Button addAssemblyButton;
        private System.Windows.Forms.ListBox assemblyList;
        private System.Windows.Forms.TextBox assemblyTextBox;
    }
}
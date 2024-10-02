namespace AddyScript.Gui
{
    partial class EditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorForm));
            newToolStripButton = new System.Windows.Forms.ToolStripButton();
            openToolStripButton = new System.Windows.Forms.ToolStripButton();
            printToolStripButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            cutToolStripButton = new System.Windows.Forms.ToolStripButton();
            copyToolStripButton = new System.Windows.Forms.ToolStripButton();
            pasteToolStripButton = new System.Windows.Forms.ToolStripButton();
            configureToolStripButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            toolbar = new System.Windows.Forms.ToolStrip();
            saveToolStripButton = new System.Windows.Forms.ToolStripSplitButton();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            exportToXmlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            undoToolStripButton = new System.Windows.Forms.ToolStripButton();
            redoToolStripButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            findToolStripButton = new System.Windows.Forms.ToolStripButton();
            replaceToolStripButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            indentToolStripButton = new System.Windows.Forms.ToolStripButton();
            unindentToolStripButton = new System.Windows.Forms.ToolStripButton();
            commentLinesToolStripButton = new System.Windows.Forms.ToolStripButton();
            uncommentLinesToolStripButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            runToolStripButton = new System.Windows.Forms.ToolStripSplitButton();
            runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            buildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            helpToolStripButton = new System.Windows.Forms.ToolStripSplitButton();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            aboutAddyScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            statusbar = new System.Windows.Forms.StatusStrip();
            fileNameStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            fileSizeStatuLabel = new System.Windows.Forms.ToolStripStatusLabel();
            caretStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            capsLockStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            insLockStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            numLockStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            contentPane = new System.Windows.Forms.Panel();
            scintilla = new ScintillaNET.Scintilla();
            editorMenu = new System.Windows.Forms.ContextMenuStrip(components);
            insertSnippetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            surroundWithToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            reformatCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            autocompleteIcons = new System.Windows.Forms.ImageList(components);
            markerTip = new System.Windows.Forms.ToolTip(components);
            keywordMenu = new AutocompleteMenuNS.AutocompleteMenu();
            snippetMenu = new AutocompleteMenuNS.AutocompleteMenu();
            toolbar.SuspendLayout();
            statusbar.SuspendLayout();
            contentPane.SuspendLayout();
            editorMenu.SuspendLayout();
            SuspendLayout();
            // 
            // newToolStripButton
            // 
            newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(newToolStripButton, "newToolStripButton");
            newToolStripButton.Name = "newToolStripButton";
            newToolStripButton.Click += newToolStripButton_Click;
            // 
            // openToolStripButton
            // 
            openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(openToolStripButton, "openToolStripButton");
            openToolStripButton.Name = "openToolStripButton";
            openToolStripButton.Click += openToolStripButton_Click;
            // 
            // printToolStripButton
            // 
            printToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(printToolStripButton, "printToolStripButton");
            printToolStripButton.Name = "printToolStripButton";
            printToolStripButton.Click += printToolStripButton_Click;
            // 
            // toolStripSeparator
            // 
            toolStripSeparator.Name = "toolStripSeparator";
            resources.ApplyResources(toolStripSeparator, "toolStripSeparator");
            // 
            // cutToolStripButton
            // 
            cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(cutToolStripButton, "cutToolStripButton");
            cutToolStripButton.Name = "cutToolStripButton";
            cutToolStripButton.Click += cutToolStripButton_Click;
            // 
            // copyToolStripButton
            // 
            copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(copyToolStripButton, "copyToolStripButton");
            copyToolStripButton.Name = "copyToolStripButton";
            copyToolStripButton.Click += copyToolStripButton_Click;
            // 
            // pasteToolStripButton
            // 
            pasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(pasteToolStripButton, "pasteToolStripButton");
            pasteToolStripButton.Name = "pasteToolStripButton";
            pasteToolStripButton.Click += pasteToolStripButton_Click;
            // 
            // configureToolStripButton
            // 
            configureToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(configureToolStripButton, "configureToolStripButton");
            configureToolStripButton.Name = "configureToolStripButton";
            configureToolStripButton.Click += configureToolStripButton_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(toolStripSeparator2, "toolStripSeparator2");
            // 
            // toolbar
            // 
            toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            toolbar.ImageScalingSize = new System.Drawing.Size(32, 32);
            toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { newToolStripButton, openToolStripButton, saveToolStripButton, printToolStripButton, toolStripSeparator, undoToolStripButton, redoToolStripButton, toolStripSeparator3, cutToolStripButton, copyToolStripButton, pasteToolStripButton, toolStripSeparator5, findToolStripButton, replaceToolStripButton, toolStripSeparator1, indentToolStripButton, unindentToolStripButton, commentLinesToolStripButton, uncommentLinesToolStripButton, toolStripSeparator4, runToolStripButton, configureToolStripButton, toolStripSeparator2, helpToolStripButton });
            resources.ApplyResources(toolbar, "toolbar");
            toolbar.Name = "toolbar";
            // 
            // saveToolStripButton
            // 
            saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            saveToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripMenuItem4, exportToXmlToolStripMenuItem });
            resources.ApplyResources(saveToolStripButton, "saveToolStripButton");
            saveToolStripButton.Name = "saveToolStripButton";
            saveToolStripButton.ButtonClick += saveToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            resources.ApplyResources(saveToolStripMenuItem, "saveToolStripMenuItem");
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            resources.ApplyResources(saveAsToolStripMenuItem, "saveAsToolStripMenuItem");
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            resources.ApplyResources(toolStripMenuItem4, "toolStripMenuItem4");
            // 
            // exportToXmlToolStripMenuItem
            // 
            exportToXmlToolStripMenuItem.Name = "exportToXmlToolStripMenuItem";
            resources.ApplyResources(exportToXmlToolStripMenuItem, "exportToXmlToolStripMenuItem");
            exportToXmlToolStripMenuItem.Click += exportToXmlToolStripMenuItem_Click;
            // 
            // undoToolStripButton
            // 
            undoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(undoToolStripButton, "undoToolStripButton");
            undoToolStripButton.Name = "undoToolStripButton";
            undoToolStripButton.Click += undoToolStripButton_Click;
            // 
            // redoToolStripButton
            // 
            redoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(redoToolStripButton, "redoToolStripButton");
            redoToolStripButton.Name = "redoToolStripButton";
            redoToolStripButton.Click += redoToolStripButton_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(toolStripSeparator3, "toolStripSeparator3");
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            resources.ApplyResources(toolStripSeparator5, "toolStripSeparator5");
            // 
            // findToolStripButton
            // 
            findToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(findToolStripButton, "findToolStripButton");
            findToolStripButton.Name = "findToolStripButton";
            findToolStripButton.Click += findToolStripButton_Click;
            // 
            // replaceToolStripButton
            // 
            replaceToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(replaceToolStripButton, "replaceToolStripButton");
            replaceToolStripButton.Name = "replaceToolStripButton";
            replaceToolStripButton.Click += replaceToolStripButton_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(toolStripSeparator1, "toolStripSeparator1");
            // 
            // indentToolStripButton
            // 
            indentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(indentToolStripButton, "indentToolStripButton");
            indentToolStripButton.Name = "indentToolStripButton";
            indentToolStripButton.Click += unindentToolStripButton_Click;
            // 
            // unindentToolStripButton
            // 
            unindentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(unindentToolStripButton, "unindentToolStripButton");
            unindentToolStripButton.Name = "unindentToolStripButton";
            unindentToolStripButton.Click += indentToolStripButton_Click;
            // 
            // commentLinesToolStripButton
            // 
            commentLinesToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(commentLinesToolStripButton, "commentLinesToolStripButton");
            commentLinesToolStripButton.Name = "commentLinesToolStripButton";
            commentLinesToolStripButton.Click += commentLinesToolStripButton_Click;
            // 
            // uncommentLinesToolStripButton
            // 
            uncommentLinesToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(uncommentLinesToolStripButton, "uncommentLinesToolStripButton");
            uncommentLinesToolStripButton.Name = "uncommentLinesToolStripButton";
            uncommentLinesToolStripButton.Click += uncommentLinesToolStripButton_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(toolStripSeparator4, "toolStripSeparator4");
            // 
            // runToolStripButton
            // 
            runToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            runToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { runToolStripMenuItem, buildToolStripMenuItem });
            resources.ApplyResources(runToolStripButton, "runToolStripButton");
            runToolStripButton.Name = "runToolStripButton";
            runToolStripButton.ButtonClick += runToolStripMenuItem_Click;
            // 
            // runToolStripMenuItem
            // 
            runToolStripMenuItem.Name = "runToolStripMenuItem";
            resources.ApplyResources(runToolStripMenuItem, "runToolStripMenuItem");
            runToolStripMenuItem.Click += runToolStripMenuItem_Click;
            // 
            // buildToolStripMenuItem
            // 
            buildToolStripMenuItem.Name = "buildToolStripMenuItem";
            resources.ApplyResources(buildToolStripMenuItem, "buildToolStripMenuItem");
            buildToolStripMenuItem.Click += buildToolStripMenuItem_Click;
            // 
            // helpToolStripButton
            // 
            helpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            helpToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { helpToolStripMenuItem, aboutAddyScriptToolStripMenuItem });
            resources.ApplyResources(helpToolStripButton, "helpToolStripButton");
            helpToolStripButton.Name = "helpToolStripButton";
            helpToolStripButton.ButtonClick += helpToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            resources.ApplyResources(helpToolStripMenuItem, "helpToolStripMenuItem");
            helpToolStripMenuItem.Click += helpToolStripMenuItem_Click;
            // 
            // aboutAddyScriptToolStripMenuItem
            // 
            aboutAddyScriptToolStripMenuItem.Name = "aboutAddyScriptToolStripMenuItem";
            resources.ApplyResources(aboutAddyScriptToolStripMenuItem, "aboutAddyScriptToolStripMenuItem");
            aboutAddyScriptToolStripMenuItem.Click += aboutAddyScriptToolStripMenuItem_Click;
            // 
            // statusbar
            // 
            statusbar.BackColor = System.Drawing.Color.Transparent;
            statusbar.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileNameStatusLabel, fileSizeStatuLabel, caretStatusLabel, capsLockStatusLabel, insLockStatusLabel, numLockStatusLabel });
            resources.ApplyResources(statusbar, "statusbar");
            statusbar.Name = "statusbar";
            // 
            // fileNameStatusLabel
            // 
            fileNameStatusLabel.Name = "fileNameStatusLabel";
            resources.ApplyResources(fileNameStatusLabel, "fileNameStatusLabel");
            fileNameStatusLabel.Spring = true;
            // 
            // fileSizeStatuLabel
            // 
            resources.ApplyResources(fileSizeStatuLabel, "fileSizeStatuLabel");
            fileSizeStatuLabel.Name = "fileSizeStatuLabel";
            // 
            // caretStatusLabel
            // 
            resources.ApplyResources(caretStatusLabel, "caretStatusLabel");
            caretStatusLabel.Name = "caretStatusLabel";
            // 
            // capsLockStatusLabel
            // 
            resources.ApplyResources(capsLockStatusLabel, "capsLockStatusLabel");
            capsLockStatusLabel.Name = "capsLockStatusLabel";
            // 
            // insLockStatusLabel
            // 
            resources.ApplyResources(insLockStatusLabel, "insLockStatusLabel");
            insLockStatusLabel.Name = "insLockStatusLabel";
            // 
            // numLockStatusLabel
            // 
            resources.ApplyResources(numLockStatusLabel, "numLockStatusLabel");
            numLockStatusLabel.Name = "numLockStatusLabel";
            // 
            // contentPane
            // 
            resources.ApplyResources(contentPane, "contentPane");
            contentPane.Controls.Add(scintilla);
            contentPane.Name = "contentPane";
            // 
            // scintilla
            // 
            scintilla.AutocompleteListSelectedBackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            scintilla.AutomaticFold = ScintillaNET.AutomaticFold.Show | ScintillaNET.AutomaticFold.Click | ScintillaNET.AutomaticFold.Change;
            scintilla.BorderStyle = ScintillaNET.BorderStyle.None;
            scintilla.ContextMenuStrip = editorMenu;
            resources.ApplyResources(scintilla, "scintilla");
            scintilla.IndentationGuides = ScintillaNET.IndentView.LookBoth;
            scintilla.LexerName = "cpp";
            scintilla.MouseDwellTime = 500;
            scintilla.Name = "scintilla";
            scintilla.SelectionBackColor = System.Drawing.Color.Gainsboro;
            scintilla.UseTabs = true;
            scintilla.CharAdded += scintilla_CharAdded;
            scintilla.DwellEnd += scintilla_DwellEnd;
            scintilla.DwellStart += scintilla_DwellStart;
            scintilla.UpdateUI += scintilla_UpdateUI;
            scintilla.TextChanged += scintilla_TextChanged;
            scintilla.MouseDown += scintilla_MouseDown;
            // 
            // editorMenu
            // 
            editorMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            editorMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { insertSnippetToolStripMenuItem, surroundWithToolStripMenuItem, toolStripMenuItem1, undoToolStripMenuItem, redoToolStripMenuItem, toolStripMenuItem2, cutToolStripMenuItem, copyToolStripMenuItem, pasteToolStripMenuItem, deleteToolStripMenuItem, toolStripMenuItem3, selectAllToolStripMenuItem, reformatCodeToolStripMenuItem });
            editorMenu.Name = "editorMenu";
            resources.ApplyResources(editorMenu, "editorMenu");
            // 
            // insertSnippetToolStripMenuItem
            // 
            insertSnippetToolStripMenuItem.Name = "insertSnippetToolStripMenuItem";
            resources.ApplyResources(insertSnippetToolStripMenuItem, "insertSnippetToolStripMenuItem");
            insertSnippetToolStripMenuItem.Click += insertSnippetToolStripMenuItem_Click;
            // 
            // surroundWithToolStripMenuItem
            // 
            resources.ApplyResources(surroundWithToolStripMenuItem, "surroundWithToolStripMenuItem");
            surroundWithToolStripMenuItem.Name = "surroundWithToolStripMenuItem";
            surroundWithToolStripMenuItem.Click += surroundWithToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // undoToolStripMenuItem
            // 
            resources.ApplyResources(undoToolStripMenuItem, "undoToolStripMenuItem");
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.Click += undoToolStripButton_Click;
            // 
            // redoToolStripMenuItem
            // 
            resources.ApplyResources(redoToolStripMenuItem, "redoToolStripMenuItem");
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.Click += redoToolStripButton_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(toolStripMenuItem2, "toolStripMenuItem2");
            // 
            // cutToolStripMenuItem
            // 
            resources.ApplyResources(cutToolStripMenuItem, "cutToolStripMenuItem");
            cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            cutToolStripMenuItem.Click += cutToolStripButton_Click;
            // 
            // copyToolStripMenuItem
            // 
            resources.ApplyResources(copyToolStripMenuItem, "copyToolStripMenuItem");
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Click += copyToolStripButton_Click;
            // 
            // pasteToolStripMenuItem
            // 
            resources.ApplyResources(pasteToolStripMenuItem, "pasteToolStripMenuItem");
            pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            pasteToolStripMenuItem.Click += pasteToolStripButton_Click;
            // 
            // deleteToolStripMenuItem
            // 
            resources.ApplyResources(deleteToolStripMenuItem, "deleteToolStripMenuItem");
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(toolStripMenuItem3, "toolStripMenuItem3");
            // 
            // selectAllToolStripMenuItem
            // 
            selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            resources.ApplyResources(selectAllToolStripMenuItem, "selectAllToolStripMenuItem");
            selectAllToolStripMenuItem.Click += selectAllToolStripMenuItem_Click;
            // 
            // reformatCodeToolStripMenuItem
            // 
            reformatCodeToolStripMenuItem.Name = "reformatCodeToolStripMenuItem";
            resources.ApplyResources(reformatCodeToolStripMenuItem, "reformatCodeToolStripMenuItem");
            reformatCodeToolStripMenuItem.Click += reformatCodeToolStripMenuItem_Click;
            // 
            // autocompleteIcons
            // 
            autocompleteIcons.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            autocompleteIcons.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("autocompleteIcons.ImageStream");
            autocompleteIcons.TransparentColor = System.Drawing.Color.Magenta;
            autocompleteIcons.Images.SetKeyName(0, "statement");
            autocompleteIcons.Images.SetKeyName(1, "class");
            autocompleteIcons.Images.SetKeyName(2, "constant");
            autocompleteIcons.Images.SetKeyName(3, "function");
            autocompleteIcons.Images.SetKeyName(4, "operator");
            autocompleteIcons.Images.SetKeyName(5, "object");
            autocompleteIcons.Images.SetKeyName(6, "snippet");
            // 
            // markerTip
            // 
            markerTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Error;
            markerTip.ToolTipTitle = "Erreurs sur cette ligne:";
            // 
            // keywordMenu
            // 
            keywordMenu.AutoPopup = false;
            keywordMenu.Colors = (AutocompleteMenuNS.Colors)resources.GetObject("keywordMenu.Colors");
            keywordMenu.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            keywordMenu.ImageList = autocompleteIcons;
            keywordMenu.MaximumSize = new System.Drawing.Size(100, 200);
            keywordMenu.TargetControlWrapper = null;
            // 
            // snippetMenu
            // 
            snippetMenu.AutoPopup = false;
            snippetMenu.Colors = (AutocompleteMenuNS.Colors)resources.GetObject("snippetMenu.Colors");
            snippetMenu.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            snippetMenu.ImageList = autocompleteIcons;
            snippetMenu.MaximumSize = new System.Drawing.Size(140, 200);
            snippetMenu.TargetControlWrapper = null;
            // 
            // EditorForm
            // 
            AllowDrop = true;
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            resources.ApplyResources(this, "$this");
            Controls.Add(contentPane);
            Controls.Add(statusbar);
            Controls.Add(toolbar);
            KeyPreview = true;
            Name = "EditorForm";
            FormClosing += TestForm_FormClosing;
            Load += TestForm_Load;
            SizeChanged += TestForm_SizeChanged;
            DragDrop += TestForm_DragDrop;
            DragEnter += TestForm_DragEnter;
            KeyUp += TestForm_KeyUp;
            toolbar.ResumeLayout(false);
            toolbar.PerformLayout();
            statusbar.ResumeLayout(false);
            statusbar.PerformLayout();
            contentPane.ResumeLayout(false);
            editorMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStripButton newToolStripButton;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton printToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripButton cutToolStripButton;
        private System.Windows.Forms.ToolStripButton copyToolStripButton;
        private System.Windows.Forms.ToolStripButton pasteToolStripButton;
        private System.Windows.Forms.ToolStripButton configureToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStrip toolbar;
        private System.Windows.Forms.ToolStripButton undoToolStripButton;
        private System.Windows.Forms.ToolStripButton redoToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.StatusStrip statusbar;
        private System.Windows.Forms.ToolStripStatusLabel fileNameStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel fileSizeStatuLabel;
        private System.Windows.Forms.ToolStripStatusLabel caretStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel capsLockStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel insLockStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel numLockStatusLabel;
        private System.Windows.Forms.ToolStripButton findToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton replaceToolStripButton;
        private System.Windows.Forms.Panel contentPane;
        private System.Windows.Forms.ImageList autocompleteIcons;
        private System.Windows.Forms.ToolStripSplitButton saveToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip editorMenu;
        private System.Windows.Forms.ToolStripMenuItem insertSnippetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem surroundWithToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripSplitButton helpToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutAddyScriptToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToXmlToolStripMenuItem;
        private System.Windows.Forms.ToolTip markerTip;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton commentLinesToolStripButton;
        private System.Windows.Forms.ToolStripButton uncommentLinesToolStripButton;
        private System.Windows.Forms.ToolStripButton unindentToolStripButton;
        private System.Windows.Forms.ToolStripButton indentToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem reformatCodeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSplitButton runToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem buildToolStripMenuItem;
        private ScintillaNET.Scintilla scintilla;
        private AutocompleteMenuNS.AutocompleteMenu keywordMenu;
        private AutocompleteMenuNS.AutocompleteMenu snippetMenu;
    }
}
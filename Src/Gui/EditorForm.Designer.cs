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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorForm));
			this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.printToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.copyToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.pasteToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.configureToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolbar = new System.Windows.Forms.ToolStrip();
			this.saveToolStripButton = new System.Windows.Forms.ToolStripSplitButton();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.exportToXmlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.redoToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.findToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.replaceToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.indentToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.unindentToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.commentLinesToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.uncommentLinesToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.runToolStripButton = new System.Windows.Forms.ToolStripSplitButton();
			this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.buildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripButton = new System.Windows.Forms.ToolStripSplitButton();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutAddyScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusbar = new System.Windows.Forms.StatusStrip();
			this.fileNameStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.fileSizeStatuLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.caretStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.capsLockStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.insLockStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.numLockStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.contentPane = new System.Windows.Forms.Panel();
			this.sciEditor = new ScintillaNet.Scintilla();
			this.editorMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.insertSnippetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.surroundWithToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reformatCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoCompleteIcons = new System.Windows.Forms.ImageList(this.components);
			this.markerTip = new System.Windows.Forms.ToolTip(this.components);
			this.toolbar.SuspendLayout();
			this.statusbar.SuspendLayout();
			this.contentPane.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.sciEditor)).BeginInit();
			this.editorMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// newToolStripButton
			// 
			this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.newToolStripButton, "newToolStripButton");
			this.newToolStripButton.Name = "newToolStripButton";
			this.newToolStripButton.Click += new System.EventHandler(this.newToolStripButton_Click);
			// 
			// openToolStripButton
			// 
			this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.openToolStripButton, "openToolStripButton");
			this.openToolStripButton.Name = "openToolStripButton";
			this.openToolStripButton.Click += new System.EventHandler(this.openToolStripButton_Click);
			// 
			// printToolStripButton
			// 
			this.printToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.printToolStripButton, "printToolStripButton");
			this.printToolStripButton.Name = "printToolStripButton";
			this.printToolStripButton.Click += new System.EventHandler(this.printToolStripButton_Click);
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			resources.ApplyResources(this.toolStripSeparator, "toolStripSeparator");
			// 
			// cutToolStripButton
			// 
			this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.cutToolStripButton, "cutToolStripButton");
			this.cutToolStripButton.Name = "cutToolStripButton";
			this.cutToolStripButton.Click += new System.EventHandler(this.cutToolStripButton_Click);
			// 
			// copyToolStripButton
			// 
			this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.copyToolStripButton, "copyToolStripButton");
			this.copyToolStripButton.Name = "copyToolStripButton";
			this.copyToolStripButton.Click += new System.EventHandler(this.copyToolStripButton_Click);
			// 
			// pasteToolStripButton
			// 
			this.pasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.pasteToolStripButton, "pasteToolStripButton");
			this.pasteToolStripButton.Name = "pasteToolStripButton";
			this.pasteToolStripButton.Click += new System.EventHandler(this.pasteToolStripButton_Click);
			// 
			// configureToolStripButton
			// 
			this.configureToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.configureToolStripButton, "configureToolStripButton");
			this.configureToolStripButton.Name = "configureToolStripButton";
			this.configureToolStripButton.Click += new System.EventHandler(this.configureToolStripButton_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			// 
			// toolbar
			// 
			this.toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolbar.ImageScalingSize = new System.Drawing.Size(32, 32);
			this.toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.printToolStripButton,
            this.toolStripSeparator,
            this.undoToolStripButton,
            this.redoToolStripButton,
            this.toolStripSeparator3,
            this.cutToolStripButton,
            this.copyToolStripButton,
            this.pasteToolStripButton,
            this.toolStripSeparator5,
            this.findToolStripButton,
            this.replaceToolStripButton,
            this.toolStripSeparator1,
            this.indentToolStripButton,
            this.unindentToolStripButton,
            this.commentLinesToolStripButton,
            this.uncommentLinesToolStripButton,
            this.toolStripSeparator4,
            this.runToolStripButton,
            this.configureToolStripButton,
            this.toolStripSeparator2,
            this.helpToolStripButton});
			resources.ApplyResources(this.toolbar, "toolbar");
			this.toolbar.Name = "toolbar";
			// 
			// saveToolStripButton
			// 
			this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.saveToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripMenuItem4,
            this.exportToXmlToolStripMenuItem});
			resources.ApplyResources(this.saveToolStripButton, "saveToolStripButton");
			this.saveToolStripButton.Name = "saveToolStripButton";
			this.saveToolStripButton.ButtonClick += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			resources.ApplyResources(this.saveToolStripMenuItem, "saveToolStripMenuItem");
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			resources.ApplyResources(this.saveAsToolStripMenuItem, "saveAsToolStripMenuItem");
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			resources.ApplyResources(this.toolStripMenuItem4, "toolStripMenuItem4");
			// 
			// exportToXmlToolStripMenuItem
			// 
			this.exportToXmlToolStripMenuItem.Name = "exportToXmlToolStripMenuItem";
			resources.ApplyResources(this.exportToXmlToolStripMenuItem, "exportToXmlToolStripMenuItem");
			this.exportToXmlToolStripMenuItem.Click += new System.EventHandler(this.exportToXmlToolStripMenuItem_Click);
			// 
			// undoToolStripButton
			// 
			this.undoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.undoToolStripButton, "undoToolStripButton");
			this.undoToolStripButton.Name = "undoToolStripButton";
			this.undoToolStripButton.Click += new System.EventHandler(this.undoToolStripButton_Click);
			// 
			// redoToolStripButton
			// 
			this.redoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.redoToolStripButton, "redoToolStripButton");
			this.redoToolStripButton.Name = "redoToolStripButton";
			this.redoToolStripButton.Click += new System.EventHandler(this.redoToolStripButton_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
			// 
			// findToolStripButton
			// 
			this.findToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.findToolStripButton, "findToolStripButton");
			this.findToolStripButton.Name = "findToolStripButton";
			this.findToolStripButton.Click += new System.EventHandler(this.findToolStripButton_Click);
			// 
			// replaceToolStripButton
			// 
			this.replaceToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.replaceToolStripButton, "replaceToolStripButton");
			this.replaceToolStripButton.Name = "replaceToolStripButton";
			this.replaceToolStripButton.Click += new System.EventHandler(this.replaceToolStripButton_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			// 
			// indentToolStripButton
			// 
			this.indentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.indentToolStripButton, "indentToolStripButton");
			this.indentToolStripButton.Name = "indentToolStripButton";
			this.indentToolStripButton.Click += new System.EventHandler(this.unindentToolStripButton_Click);
			// 
			// unindentToolStripButton
			// 
			this.unindentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.unindentToolStripButton, "unindentToolStripButton");
			this.unindentToolStripButton.Name = "unindentToolStripButton";
			this.unindentToolStripButton.Click += new System.EventHandler(this.indentToolStripButton_Click);
			// 
			// commentLinesToolStripButton
			// 
			this.commentLinesToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.commentLinesToolStripButton, "commentLinesToolStripButton");
			this.commentLinesToolStripButton.Name = "commentLinesToolStripButton";
			this.commentLinesToolStripButton.Click += new System.EventHandler(this.commentLinesToolStripButton_Click);
			// 
			// uncommentLinesToolStripButton
			// 
			this.uncommentLinesToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.uncommentLinesToolStripButton, "uncommentLinesToolStripButton");
			this.uncommentLinesToolStripButton.Name = "uncommentLinesToolStripButton";
			this.uncommentLinesToolStripButton.Click += new System.EventHandler(this.uncommentLinesToolStripButton_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
			// 
			// runToolStripButton
			// 
			this.runToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.runToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolStripMenuItem,
            this.buildToolStripMenuItem});
			resources.ApplyResources(this.runToolStripButton, "runToolStripButton");
			this.runToolStripButton.Name = "runToolStripButton";
			this.runToolStripButton.ButtonClick += new System.EventHandler(this.runToolStripMenuItem_Click);
			// 
			// runToolStripMenuItem
			// 
			this.runToolStripMenuItem.Name = "runToolStripMenuItem";
			resources.ApplyResources(this.runToolStripMenuItem, "runToolStripMenuItem");
			this.runToolStripMenuItem.Click += new System.EventHandler(this.runToolStripMenuItem_Click);
			// 
			// buildToolStripMenuItem
			// 
			this.buildToolStripMenuItem.Name = "buildToolStripMenuItem";
			resources.ApplyResources(this.buildToolStripMenuItem, "buildToolStripMenuItem");
			this.buildToolStripMenuItem.Click += new System.EventHandler(this.buildToolStripMenuItem_Click);
			// 
			// helpToolStripButton
			// 
			this.helpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.helpToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem,
            this.aboutAddyScriptToolStripMenuItem});
			resources.ApplyResources(this.helpToolStripButton, "helpToolStripButton");
			this.helpToolStripButton.Name = "helpToolStripButton";
			this.helpToolStripButton.ButtonClick += new System.EventHandler(this.helpToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
			this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
			// 
			// aboutAddyScriptToolStripMenuItem
			// 
			this.aboutAddyScriptToolStripMenuItem.Name = "aboutAddyScriptToolStripMenuItem";
			resources.ApplyResources(this.aboutAddyScriptToolStripMenuItem, "aboutAddyScriptToolStripMenuItem");
			this.aboutAddyScriptToolStripMenuItem.Click += new System.EventHandler(this.aboutAddyScriptToolStripMenuItem_Click);
			// 
			// statusbar
			// 
			this.statusbar.BackColor = System.Drawing.Color.Transparent;
			this.statusbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileNameStatusLabel,
            this.fileSizeStatuLabel,
            this.caretStatusLabel,
            this.capsLockStatusLabel,
            this.insLockStatusLabel,
            this.numLockStatusLabel});
			resources.ApplyResources(this.statusbar, "statusbar");
			this.statusbar.Name = "statusbar";
			// 
			// fileNameStatusLabel
			// 
			this.fileNameStatusLabel.Name = "fileNameStatusLabel";
			resources.ApplyResources(this.fileNameStatusLabel, "fileNameStatusLabel");
			this.fileNameStatusLabel.Spring = true;
			// 
			// fileSizeStatuLabel
			// 
			resources.ApplyResources(this.fileSizeStatuLabel, "fileSizeStatuLabel");
			this.fileSizeStatuLabel.Name = "fileSizeStatuLabel";
			// 
			// caretStatusLabel
			// 
			resources.ApplyResources(this.caretStatusLabel, "caretStatusLabel");
			this.caretStatusLabel.Name = "caretStatusLabel";
			// 
			// capsLockStatusLabel
			// 
			resources.ApplyResources(this.capsLockStatusLabel, "capsLockStatusLabel");
			this.capsLockStatusLabel.Name = "capsLockStatusLabel";
			// 
			// insLockStatusLabel
			// 
			resources.ApplyResources(this.insLockStatusLabel, "insLockStatusLabel");
			this.insLockStatusLabel.Name = "insLockStatusLabel";
			// 
			// numLockStatusLabel
			// 
			resources.ApplyResources(this.numLockStatusLabel, "numLockStatusLabel");
			this.numLockStatusLabel.Name = "numLockStatusLabel";
			// 
			// contentPane
			// 
			resources.ApplyResources(this.contentPane, "contentPane");
			this.contentPane.Controls.Add(this.sciEditor);
			this.contentPane.Name = "contentPane";
			// 
			// sciEditor
			// 
			this.sciEditor.Caret.CurrentLineBackgroundColor = System.Drawing.Color.Lavender;
			this.sciEditor.Caret.HighlightCurrentLine = true;
			this.sciEditor.ConfigurationManager.CustomLocation = "Scintilla.xml";
			this.sciEditor.ConfigurationManager.Language = "addyscript";
			this.sciEditor.ContextMenuStrip = this.editorMenu;
			resources.ApplyResources(this.sciEditor, "sciEditor");
			this.sciEditor.Indentation.ShowGuides = true;
			this.sciEditor.Indentation.TabWidth = 4;
			this.sciEditor.IsBraceMatching = true;
			this.sciEditor.Margins.Margin0.Width = 32;
			this.sciEditor.Margins.Margin2.Width = 16;
			this.sciEditor.Name = "sciEditor";
			this.sciEditor.Scrolling.EndAtLastLine = false;
			this.sciEditor.Scrolling.HorizontalWidth = 1;
			this.sciEditor.Styles.BraceBad.BackColor = System.Drawing.SystemColors.Window;
			this.sciEditor.Styles.BraceBad.FontName = "Verdana";
			this.sciEditor.Styles.BraceLight.BackColor = System.Drawing.SystemColors.Window;
			this.sciEditor.Styles.BraceLight.FontName = "Verdana";
			this.sciEditor.Styles.ControlChar.FontName = "Verdana";
			this.sciEditor.Styles.Default.FontName = "Verdana";
			this.sciEditor.Styles.IndentGuide.FontName = "Verdana";
			this.sciEditor.Styles.LastPredefined.FontName = "Verdana";
			this.sciEditor.Styles.LineNumber.FontName = "Verdana";
			this.sciEditor.Styles.Max.FontName = "Verdana";
			this.sciEditor.TextInserted += new System.EventHandler<ScintillaNet.TextModifiedEventArgs>(this.sciEditor_TextLengthChanged);
			this.sciEditor.TextDeleted += new System.EventHandler<ScintillaNet.TextModifiedEventArgs>(this.sciEditor_TextLengthChanged);
			this.sciEditor.CharAdded += new System.EventHandler<ScintillaNet.CharAddedEventArgs>(this.sciEditor_CharAdded);
			this.sciEditor.SelectionChanged += new System.EventHandler(this.sciEditor_SelectionChanged);
			this.sciEditor.DwellStart += new System.EventHandler<ScintillaNet.ScintillaMouseEventArgs>(this.sciEditor_DwellStart);
			this.sciEditor.DwellEnd += new System.EventHandler<ScintillaNet.ScintillaMouseEventArgs>(this.sciEditor_DwellEnd);
			this.sciEditor.MouseDown += new System.Windows.Forms.MouseEventHandler(this.sciEditor_MouseDown);
			// 
			// editorMenu
			// 
			this.editorMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.insertSnippetToolStripMenuItem,
            this.surroundWithToolStripMenuItem,
            this.toolStripMenuItem1,
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripMenuItem2,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.toolStripMenuItem3,
            this.selectAllToolStripMenuItem,
            this.reformatCodeToolStripMenuItem});
			this.editorMenu.Name = "editorMenu";
			resources.ApplyResources(this.editorMenu, "editorMenu");
			// 
			// insertSnippetToolStripMenuItem
			// 
			this.insertSnippetToolStripMenuItem.Name = "insertSnippetToolStripMenuItem";
			resources.ApplyResources(this.insertSnippetToolStripMenuItem, "insertSnippetToolStripMenuItem");
			this.insertSnippetToolStripMenuItem.Click += new System.EventHandler(this.insertSnippetToolStripMenuItem_Click);
			// 
			// surroundWithToolStripMenuItem
			// 
			resources.ApplyResources(this.surroundWithToolStripMenuItem, "surroundWithToolStripMenuItem");
			this.surroundWithToolStripMenuItem.Name = "surroundWithToolStripMenuItem";
			this.surroundWithToolStripMenuItem.Click += new System.EventHandler(this.surroundWithToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
			// 
			// undoToolStripMenuItem
			// 
			resources.ApplyResources(this.undoToolStripMenuItem, "undoToolStripMenuItem");
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripButton_Click);
			// 
			// redoToolStripMenuItem
			// 
			resources.ApplyResources(this.redoToolStripMenuItem, "redoToolStripMenuItem");
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripButton_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
			// 
			// cutToolStripMenuItem
			// 
			resources.ApplyResources(this.cutToolStripMenuItem, "cutToolStripMenuItem");
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripButton_Click);
			// 
			// copyToolStripMenuItem
			// 
			resources.ApplyResources(this.copyToolStripMenuItem, "copyToolStripMenuItem");
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripButton_Click);
			// 
			// pasteToolStripMenuItem
			// 
			resources.ApplyResources(this.pasteToolStripMenuItem, "pasteToolStripMenuItem");
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripButton_Click);
			// 
			// deleteToolStripMenuItem
			// 
			resources.ApplyResources(this.deleteToolStripMenuItem, "deleteToolStripMenuItem");
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			resources.ApplyResources(this.selectAllToolStripMenuItem, "selectAllToolStripMenuItem");
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.selectAllToolStripMenuItem_Click);
			// 
			// reformatCodeToolStripMenuItem
			// 
			this.reformatCodeToolStripMenuItem.Name = "reformatCodeToolStripMenuItem";
			resources.ApplyResources(this.reformatCodeToolStripMenuItem, "reformatCodeToolStripMenuItem");
			this.reformatCodeToolStripMenuItem.Click += new System.EventHandler(this.reformatCodeToolStripMenuItem_Click);
			// 
			// autoCompleteIcons
			// 
			this.autoCompleteIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("autoCompleteIcons.ImageStream")));
			this.autoCompleteIcons.TransparentColor = System.Drawing.Color.Transparent;
			this.autoCompleteIcons.Images.SetKeyName(0, "statement");
			this.autoCompleteIcons.Images.SetKeyName(1, "class");
			this.autoCompleteIcons.Images.SetKeyName(2, "constant");
			this.autoCompleteIcons.Images.SetKeyName(3, "function");
			this.autoCompleteIcons.Images.SetKeyName(4, "operator");
			this.autoCompleteIcons.Images.SetKeyName(5, "object");
			// 
			// markerTip
			// 
			this.markerTip.IsBalloon = true;
			this.markerTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Error;
			this.markerTip.ToolTipTitle = "Erreurs sur cette ligne:";
			// 
			// EditorForm
			// 
			this.AllowDrop = true;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.contentPane);
			this.Controls.Add(this.statusbar);
			this.Controls.Add(this.toolbar);
			this.KeyPreview = true;
			this.Name = "EditorForm";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TestForm_FormClosing);
			this.Load += new System.EventHandler(this.TestForm_Load);
			this.SizeChanged += new System.EventHandler(this.TestForm_SizeChanged);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.TestForm_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.TestForm_DragEnter);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TestForm_KeyUp);
			this.toolbar.ResumeLayout(false);
			this.toolbar.PerformLayout();
			this.statusbar.ResumeLayout(false);
			this.statusbar.PerformLayout();
			this.contentPane.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.sciEditor)).EndInit();
			this.editorMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

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
        private ScintillaNet.Scintilla sciEditor;
        private System.Windows.Forms.ImageList autoCompleteIcons;
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
    }
}
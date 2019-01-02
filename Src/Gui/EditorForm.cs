#region 'using' directives

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using ScintillaNet;

using AddyScript.Gui.CallTips;
using AddyScript.Gui.Properties;
using AddyScript.Gui.Utilities;

#endregion

namespace AddyScript.Gui
{
    public partial class EditorForm : Form
    {
        #region Constants

        // Some key constants
        private const int VK_CAPITAL = 0x14;
        private const int VK_INSERT = 0x2D;
        private const int VK_NUMLOCK = 0x90;

        #endregion

        #region Fields

        // Some fields
        private CallTipInfo callTipInfo;
        private string lastErrorMessage;
        private string filePath;
        private string dndFilePath;

        #endregion

        #region Constructor

        /// <summary>
        /// The constructor
        /// </summary>
        public EditorForm(string argument)
        {
            InitializeComponent();

            // Customize the window
            base.ForeColor = EditorColorTable.WindowForeground;
            base.BackColor = EditorColorTable.WindowBackground;

            // Define the dwell time
            sciEditor.NativeInterface.SetMouseDwellTime(500);
            
            // Register an image list for the autocomplete list
            sciEditor.AutoComplete.RegisterImages(autoCompleteIcons, Color.Magenta);
            
            // Register a 'red bullet' icon for the first marker
            var redBullet = new Bitmap(GetType(), "red_bullet.bmp");
            sciEditor.Markers[0].Symbol = MarkerSymbol.PixMap;
            sciEditor.Markers[0].SetImage(redBullet, Color.Magenta);

            // Load the script indicated by the command line argument if any
            if (string.IsNullOrEmpty(argument))
                Reset();
            else
                Open(argument);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The path to the last opened or saved file
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;

                if (string.IsNullOrEmpty(filePath))
                {
                    fileNameStatusLabel.Text = Resources.NewScript;
                    Text = "AddyScript";
                }
                else
                {
                    fileNameStatusLabel.Text = value;
                    Text = Path.GetFileName(value) + " - AddyScript";
                }
            }
        }

        /// <summary>
        /// Gets if the script has been saved or not
        /// </summary>
        public bool Saved { get; private set; }

        #endregion

        #region P/Invoke

        /// <summary> 
        /// Gets the status of a specific key on the keyboard. 
        /// </summary> 
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int keyCode);

        #endregion

        #region Utility

        /// <summary>
        /// Resets the environment.
        /// </summary>
        public void Reset()
        {
            sciEditor.Text = string.Empty;
            FilePath = null;
            UpdateWindowBars();
            Saved = true;
        }

        /// <summary>
        /// Loads a script into the editor.
        /// </summary>
        /// <param name="path"></param>
        public void Open(string path)
        {
            using (StreamReader reader = File.OpenText(path))
            {
                sciEditor.Text = reader.ReadToEnd();
                FilePath = path;
                UpdateWindowBars();
            }

            Saved = true;
        }

        /// <summary>
        /// Saves a script to a file.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            using (StreamWriter writer = File.CreateText(path))
            {
                writer.Write(sciEditor.Text);
            }

            FilePath = path;
            Saved = true;
        }

        /// <summary>
        /// Prompts the user to save the script before leaving.
        /// </summary>
        /// <returns>true to continue; false to cancel the current action</returns>
        public bool PromptToSave()
        {
            DialogResult answer = MessageBox.Show(Resources.PromptToSave,
                                                  Text,
                                                  MessageBoxButtons.YesNoCancel,
                                                  MessageBoxIcon.Question);

            switch (answer)
            {
                case DialogResult.Yes:
                    saveToolStripMenuItem_Click(null, null);
                    break;
                case DialogResult.Cancel:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Updates some items in the toolbar and the statusbar.
        /// </summary>
        private void UpdateWindowBars()
        {
            sciEditor.UndoRedo.EmptyUndoBuffer();
            UpdateUndoRedoFileSize();
            UpdateCutCopyCaretInfo();
        }

        /// <summary>
        /// Updates the 'Undo', 'Redo' toolbar buttons as well as
        ///  the part of the status bar where the file size is displayed.
        /// </summary>
        private void UpdateUndoRedoFileSize()
        {
            undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = sciEditor.UndoRedo.CanUndo;
            redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = sciEditor.UndoRedo.CanRedo;

            fileSizeStatuLabel.Text = string.Format(Resources.TextLength, sciEditor.TextLength);
        }

        /// <summary>
        /// Updates the 'Cut', 'Copy' toolbar buttons as well as
        /// the part of the status bar where the caret info are shown.
        /// </summary>
        private void UpdateCutCopyCaretInfo()
        {
            surroundWithToolStripMenuItem.Enabled = deleteToolStripMenuItem.Enabled
                                                  = cutToolStripMenuItem.Enabled
                                                  = copyToolStripMenuItem.Enabled
                                                  = cutToolStripButton.Enabled
                                                  = copyToolStripButton.Enabled
                                                  = sciEditor.Selection.Length > 0;

            caretStatusLabel.Text = string.Format(Resources.CaretStatus,
                                                  sciEditor.Caret.LineNumber + 1,
                                                  sciEditor.GetColumn(sciEditor.Caret.Position) + 1,
                                                  sciEditor.Selection.Length);
        }

        /// <summary>
        /// Verifies that the mouse pointer is on a marker or not.
        /// </summary>
        /// <param name="x">The X co-ordonate of the mouse pointer</param>
        /// <param name="y">The Y co-ordonate of the mouse pointer</param>
        /// <returns><b>true</b> is the mouse is on a marker. <b>false</b> otherwise</returns>
        private bool IsMouseOnMarker(int x, int y)
        {
            if (x <= sciEditor.Margins.Margin0.Width ||
                x > sciEditor.Margins.Margin0.Width + sciEditor.Margins.Margin1.Width)
                return false;

            int position = sciEditor.PositionFromPoint(x, y);
            Line line = sciEditor.Lines.FromPosition(position);
            return line.GetMarkers().Count > 0;
        }

        /// <summary>
        /// Verifies that a given position is between the boundaries of a comment or a string.
        /// </summary>
        /// <param name="position">The given position</param>
        /// <returns><b>true</b> if the caret is in a comment or a string literal. <b>false</b> otherwise</returns>
        private bool InCommentOrString(int position)
        {
            string style = sciEditor.Styles.GetStyleNameAt(position);
            string prevStyle = sciEditor.Styles.GetStyleNameAt(position - 1);
            char ch = sciEditor.CharAt(position);

            switch (style)
            {
                case "COMMENT":
                case "COMMENTDOC":
                    return ch != '/' || sciEditor.CharAt(position - 1) != '*';
                case "COMMENTLINE":
                case "COMMENTLINEDOC":
                    return ch != '\n';
                case "CHARACTER":
                    return ch != '\'' || prevStyle != style || sciEditor.CharAt(position - 1) == '\\';
                case "STRING":
                    return ch != '"' || prevStyle != style || sciEditor.CharAt(position - 1) == '\\';
                case "STRINGEOL":
                    return ch != '\n' || prevStyle != style;
                case "VERBATIM":
                    return (ch != '"' && ch != '\'') ||
                           sciEditor.CharAt(position - 1) != ch ||
                           sciEditor.CharAt(position - 2) == ch; // Note: Not perfect!
            }

            return false;
        }

        /// <summary>
        /// Gets the "word" at the caret position
        /// </summary>
        /// <returns>A string</returns>
        public string GetCurrentWord()
        {
            return sciEditor.GetWordFromPosition(sciEditor.CurrentPos);
        }

        /// <summary>
        /// Retrieves the "word" at the left of the current position.
        /// </summary>
        /// <returns>A string</returns>
        private string GetWordAtLeft()
        {
            int pos = sciEditor.CurrentPos;
            sciEditor.CurrentPos = pos - 2;
            sciEditor.NativeInterface.WordLeft();
            string word = sciEditor.GetWordFromPosition(sciEditor.CurrentPos);
            sciEditor.CurrentPos = pos;
            return word;
        }

        /// <summary>
        /// Pushes a new CallTipInfo on top of the stack.
        /// </summary>
        /// <param name="cti">The new CallTipInfo</param>
        private void PushCallTipInfo(CallTipInfo cti)
        {
            cti.Parent = callTipInfo;
            callTipInfo = cti;
        }

        /// <summary>
        /// Shows a call tip according to the current callTipInfo.
        /// </summary>
        private void ShowCallTip()
        {
            sciEditor.CallTip.Show(callTipInfo.Text);
            if (callTipInfo.ActiveParameter == null) return;
            sciEditor.NativeInterface.CallTipSetHlt(callTipInfo.ActiveParameter.Start,
                                                    callTipInfo.ActiveParameter.End);
        }

        /// <summary>
        /// Sends an error message to the console.
        /// </summary>
        /// <param name="sx">The error to be printed</param>
        private void ReportError(ScriptException sx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(lastErrorMessage = sx.Message);
            Console.ResetColor();

            if (!string.IsNullOrEmpty(sx.FileName))
            {
                Console.Write(Resources.File);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(sx.FileName);
                Console.ResetColor();
                Console.Write(Resources.Comma);
            }

            Console.Write(Resources.Line);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(sx.ScriptElement.Start.LineNumber + 1);
            Console.ResetColor();

            Console.WriteLine(Resources.PressAnyKeyToReturn);
            Console.ReadKey(true);

            if (sx.FileName != filePath) return;

            sciEditor.Caret.Goto(sx.ScriptElement.Start.Offset);
            sciEditor.Markers[0].AddInstanceTo(sciEditor.Caret.LineNumber);
            sciEditor.GetRange(sx.ScriptElement.Start.Offset, sx.ScriptElement.End.Offset).SetIndicator(0);
        }

        /// <summary>
        /// Clear any error indicator on the editor
        /// </summary>
        private void ClearErrors()
        {
            lastErrorMessage = null;
            sciEditor.Markers.DeleteAll();
            sciEditor.GetRange().ClearIndicator(0);
        }

        private void IndentSelection(int indentation)
        {
            int startingLineNumber = sciEditor.Selection.Range.StartingLine.Number;
            int endingLineNumber = sciEditor.Selection.Range.EndingLine.Number;

            sciEditor.UndoRedo.BeginUndoAction();
            for (int i = startingLineNumber; i <= endingLineNumber; ++i)
                sciEditor.Lines[i].Indentation += indentation * sciEditor.Indentation.TabWidth;
            sciEditor.UndoRedo.EndUndoAction();
        }

        private float ScrollingRatio()
        {
            return 0.9F * (sciEditor.Zoom + 11);
        }

        #endregion

        #region Event handlers

        #region Application's events

        /// <summary>
        /// Handles the Appication's Idle event
        /// </summary>
        /// <param name="sender">Well, the Application itself</param>
        /// <param name="e">A dummy object</param>
        private void Application_Idle(object sender, EventArgs e)
        {
            pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = sciEditor.Clipboard.CanPaste;

            bool insLock = (GetKeyState(VK_INSERT) & 0xFFF) != 0;
            insLockStatusLabel.Text = insLock ? Resources.InsLoc : Resources.InsUnloc;

            bool capsLock = (GetKeyState(VK_CAPITAL) & 0xFFF) != 0;
            capsLockStatusLabel.Text = capsLock ? Resources.CapsLoc : string.Empty;

            bool numLock = (GetKeyState(VK_NUMLOCK) & 0xFFF) != 0;
            numLockStatusLabel.Text = numLock ? Resources.NumLoc : string.Empty;

            int hw = 0;

            foreach (var line in sciEditor.Lines.VisibleLines)
                hw = Math.Max(hw, (int)(ScrollingRatio() * line.Length));

            if (hw > sciEditor.Scrolling.HorizontalWidth)
                sciEditor.Scrolling.HorizontalWidth = hw;
        }

        #endregion

        #region Form's events

        private void TestForm_Load(object sender, EventArgs e)
        {
            Application.Idle += Application_Idle;

            if (!string.IsNullOrEmpty(filePath) &&
                Settings.Default.WindowSettings != null &&
                Settings.Default.WindowSettings.ContainsKey(filePath))
            {
                WindowSettings ws = Settings.Default.WindowSettings[filePath];

                sciEditor.Zoom = ws.Zoom;
                sciEditor.CurrentPos = ws.PositionInText;
                sciEditor.Caret.EnsureVisible();

                WindowState = ws.WindowState;
                if (WindowState == FormWindowState.Normal)
                {
                    Location = ws.Location;
                    Size = ws.Size;
                }
            }
        }

        private void TestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!(Saved || PromptToSave()))
            {
                e.Cancel = true;
                return;
            }

            if (!string.IsNullOrEmpty(filePath) &&
                WindowState != FormWindowState.Minimized)
            {
                if (Settings.Default.WindowSettings == null)
                    Settings.Default.WindowSettings = new WindowSettingsDictionary();
                Settings.Default.WindowSettings[filePath] =
                    new WindowSettings(WindowState, Location, Size, sciEditor.Zoom, sciEditor.CurrentPos);
                Settings.Default.Save();
            }

            Application.Idle -= Application_Idle;
        }

        private void TestForm_SizeChanged(object sender, EventArgs e)
        {
            contentPane.Padding = new Padding(Math.Max(Width / 40, Height / 40));
        }

        private void TestForm_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.N:
                    if (!e.Control) break;
                    newToolStripButton_Click(null, null);
                    e.SuppressKeyPress = true;
                    break;
                case Keys.O:
                    if (!e.Control) break;
                    openToolStripButton_Click(null, null);
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void TestForm_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            var fileExt = Path.GetExtension(files[0]).ToLower();

            switch (fileExt)
            {
                case ".add":
                case ".txt":
                    e.Effect = DragDropEffects.Copy;
                    dndFilePath = files[0];
                    break;
            }
        }

        private void TestForm_DragDrop(object sender, DragEventArgs e)
        {
            Open(dndFilePath);
        }

        #endregion

        #region Toolbar's buttons events

        private void newToolStripButton_Click(object sender, EventArgs e)
        {
            EditorAppContext.OpenForm(string.Empty);
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = Resources.FileDialogFilter;
                dialog.Title = Resources.OpenFileDialogTitle;
                dialog.Multiselect = true;

                if (dialog.ShowDialog() != DialogResult.OK) return;

                foreach (string fileName in dialog.FileNames)
                    EditorAppContext.OpenForm(fileName);

                if (string.IsNullOrEmpty(filePath) && sciEditor.TextLength <= 0)
                    Close();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
                saveAsToolStripMenuItem_Click(null, null);
            else
                Save(FilePath);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = Resources.FileDialogFilter;
                dialog.Title = Resources.SaveFileDialogTitle;

                if (dialog.ShowDialog() != DialogResult.OK) return;

                Save(dialog.FileName);
            }
        }

        private void exportToXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = Resources.XmlFileFilter;
                    dialog.Title = Resources.ExportXmlTitle;
                    if (!string.IsNullOrEmpty(filePath))
                        dialog.FileName = Path.ChangeExtension(Path.GetFileName(filePath),
                                                               dialog.DefaultExt);

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var program = ScriptEngine.ParseString(sciEditor.Text);
                        ScriptEngine.ExportXml(program, dialog.FileName);
                        Process.Start(dialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.ErrorMessageTitle,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(filePath))
                sciEditor.Printing.PrintDocument.DocumentName = Path.GetFileName(filePath);

            sciEditor.Printing.Print();
        }

        private void undoToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.UndoRedo.Undo();
        }

        private void redoToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.UndoRedo.Redo();
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.Clipboard.Cut();
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.Clipboard.Copy();
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.Clipboard.Paste();
        }

        private void findToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.FindReplace.ShowFind();
        }

        private void replaceToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.FindReplace.ShowReplace();
        }

        private void indentToolStripButton_Click(object sender, EventArgs e)
        {
            IndentSelection(1);
        }

        private void unindentToolStripButton_Click(object sender, EventArgs e)
        {
            IndentSelection(-1);
        }

        private void commentLinesToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.Lexing.LineComment();
        }

        private void uncommentLinesToolStripButton_Click(object sender, EventArgs e)
        {
            sciEditor.Lexing.LineUncomment();
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /****************************************************************************
             * Parses the script and then runs it.
             * *************************************************************************/
            if (!(Saved || string.IsNullOrEmpty(filePath)))
                Save(filePath);

            try
            {
                ClearErrors();
                Hide();
                Console.Clear();
                Console.Title = Text;
                
                var context = EditorAppContext.GetScriptContext();

                if (Saved & sciEditor.TextLength > 0)
                    ScriptEngine.ExecuteFile(filePath, context);
                else
                    ScriptEngine.ExecuteString(sciEditor.Text, context);
            }
            catch (ScriptException ex)
            {
                ReportError(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.Read();
            }
            finally
            {
                Console.Title += " [Done]";
                Show();
                Activate();
            }
        }

        private void buildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Resources.NotImplemented, Resources.MissingFunctionality,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void configureToolStripButton_Click(object sender, EventArgs e)
        {
            using (var options = new OptionBox())
            {
                options.ShowDialog(this);
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "AddyScript.chm");
        }

        private void aboutAddyScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutBox())
            {
                about.ShowDialog(this);
            }
        }

        #endregion

        #region Context menu items events

        private void insertSnippetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sciEditor.Snippets.ShowSnippetList();
        }

        private void surroundWithToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sciEditor.Snippets.ShowSurroundWithList();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sciEditor.Selection.Clear();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sciEditor.Selection.SelectAll();
        }

        private void reformatCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //try
            //{
                var program = ScriptEngine.ParseString(sciEditor.Text);
                sciEditor.Text = ScriptEngine.GenerateCode(program);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, Resources.ErrorMessageTitle,
            //                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }

        #endregion

        #region Scintilla control's events

        private void sciEditor_TextLengthChanged(object sender, TextModifiedEventArgs e)
        {
            UpdateUndoRedoFileSize();
            Saved = sciEditor.TextLength <= 0 && string.IsNullOrEmpty(filePath);
        }

        private void sciEditor_SelectionChanged(object sender, EventArgs e)
        {
            UpdateCutCopyCaretInfo();
        }

        private void sciEditor_CharAdded(object sender, CharAddedEventArgs e)
        {
            /****************************************************************************
             * Tries to popup an autocomplete list or to display a calltip
             * *************************************************************************/
            if (InCommentOrString(sciEditor.CurrentPos - 2)) return;

            if (char.IsLetterOrDigit(e.Ch))
            {
                string word = GetCurrentWord();

                foreach (string keyword in sciEditor.AutoComplete.List)
                {
                    if (!keyword.StartsWith(word)) continue;
                    sciEditor.AutoComplete.Show();
                    break;
                }
            }
            else
                switch (e.Ch)
                {
                    case '(':
                        {
                            string word = GetWordAtLeft();
                            if (!CallTipProvider.IsDefined(word)) break;
                            PushCallTipInfo(CallTipProvider.GetCallTipInfo(word));
                            ShowCallTip();
                        }
                        break;
                    case ',':
                        if (callTipInfo == null) break;
                        sciEditor.CallTip.Hide();
                        if (!callTipInfo.NextParameter()) break;
                        ShowCallTip();
                        break;
                    case ')':
                        if (callTipInfo == null) break;
                        sciEditor.CallTip.Hide();
                        if ((callTipInfo = callTipInfo.Parent) == null) break;
                        ShowCallTip();
                        break;
                }
        }

        private void sciEditor_DwellStart(object sender, ScintillaMouseEventArgs e)
        {
            if (IsMouseOnMarker(e.X, e.Y))
                markerTip.Show(lastErrorMessage, sciEditor, e.X - 20, e.Y - 80);
            else if (sciEditor.NativeInterface.IndicatorValueAt(0, e.Position) != 0)
                sciEditor.CallTip.Show(lastErrorMessage, e.Position);
            else
            {
                if (InCommentOrString(e.Position)) return;

                string word = sciEditor.GetWordFromPosition(e.Position);
                if (!CallTipProvider.IsDefined(word)) return;

                callTipInfo = CallTipProvider.GetCallTipInfo(word);
                sciEditor.CallTip.Show(callTipInfo.Text, e.Position);
            }
        }

        private void sciEditor_DwellEnd(object sender, ScintillaMouseEventArgs e)
        {
            markerTip.Hide(sciEditor);
            sciEditor.CallTip.Hide();
            callTipInfo = null;
        }

        private void sciEditor_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            int position = sciEditor.PositionFromPoint(e.X, e.Y);

            if (!sciEditor.Selection.Range.PositionInRange(position))
                sciEditor.CurrentPos = position;
        }

        #endregion

        #endregion
    }
}
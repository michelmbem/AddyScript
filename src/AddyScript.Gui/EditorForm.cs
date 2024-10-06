using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using AddyScript.Gui.Autocomplete;
using AddyScript.Gui.CallTips;
using AddyScript.Gui.Properties;
using AddyScript.Gui.Utilities;

using AutocompleteMenuNS;
using ScintillaNET;
using ScintillaNET_FindReplaceDialog;
using ScintillaPrinting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;


namespace AddyScript.Gui
{
    public partial class EditorForm : Form
    {
        #region Constants

        // Default window title and help link
        private const string TITLE_BASE = "AddyScript";
        private const string HELP_LINK = "https://github.com/michelmbem/AddyScript/blob/master/docs/README.md";

        // Some key constants
        private const int VK_CAPITAL = 0x14;
        private const int VK_INSERT = 0x2D;
        private const int VK_NUMLOCK = 0x90;

        // Styling constants
        private const int NUMBER_MARGIN = 0;
        private const int ERROR_MARGIN = 1;
        private const int FOLDING_MARGIN = 2;
        private const int ERROR_MARKER = 0;
        private const int ERROR_INDICATOR = 0;
        private const int DEFAULT_MARGIN_WIDTH = 15;

        #endregion

        #region Fields

        // Readonly fields
        private readonly PaddingAdjuster paddingAdjuster;
        private readonly Printing printing;
        private readonly FindReplace findReplace;

        // Mutable fields
        private string filePath;
        private bool saved;

        private CallTipInfo callTipInfo;
        private int caretPosition;
        private int scriptLength;
        private string errorMessage;
        private string dndFilePath;

        #endregion

        #region Constructor

        /// <summary>
        /// The constructor
        /// </summary>
        public EditorForm(string argument)
        {
            InitializeComponent();
            InitializeStyling();
            InitializeAutocomplete();

            // Initialize the paddingAdjustment object
            paddingAdjuster = new PaddingAdjuster(contentPane);

            // Initialize the printing object
            printing = new Printing(scintilla);

            // Initialize the findReplace object
            findReplace = new FindReplace { Scintilla = scintilla };
            findReplace.Window.StartPosition = FormStartPosition.Manual;
            findReplace.KeyPressed += findReplace_KeyPressed;

            // Customize window's appearance
            ForeColor = EditorColorTable.WindowForeground;
            BackColor = EditorColorTable.WindowBackground;

            // Load the script indicated by the supplied command line argument if any
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
            get => filePath;
            set
            {
                filePath = value;

                if (string.IsNullOrEmpty(filePath))
                {
                    fileNameStatusLabel.Text = Resources.NewScript;
                    Text = TITLE_BASE;
                }
                else
                {
                    fileNameStatusLabel.Text = value;
                    Text = $"{Path.GetFileName(value)} - {TITLE_BASE}";
                }
            }
        }

        /// <summary>
        /// Gets if the script was saved or not
        /// </summary>
        public bool Saved
        {
            get => saved;
            private set
            {
                saved = value;

                if (value && Text.EndsWith('*'))
                    Text = Text[..^1];
                else if (!(value || Text.EndsWith('*')))
                    Text += "*";
            }
        }

        #endregion

        #region P/Invoke

        /// <summary> 
        /// Gets the status of a specific key on the keyboard. 
        /// </summary> 
        [LibraryImport("user32")]
        private static partial short GetKeyState(int keyCode);

        #endregion

        #region Initialization

        private void InitializeStyling()
        {
            // Common properties programatically set
            scintilla.CaretLineBackColor = EditorColorTable.Light(Color.LightSkyBlue);

            // DataItem properties
            scintilla.SetProperty("fold", "1"); // Enable folding
            scintilla.SetProperty("fold.comment", "1"); // Enable folding on multiline comments
            scintilla.SetProperty("lexer.cpp.backquoted.strings", "1"); // Allow string literals enclosed in backticks
            scintilla.SetProperty("lexer.cpp.escape.sequence", "1"); // Enable the styling of escape sequences in string literals

            // Define keywords
            scintilla.SetKeywords(0, @"
                abstract as blob bool break case catch class closure complex const constructor contains continue date decimal
                default do else endswith event extern false final finally float for foreach function goto if import in int is
                list long map matches new not null object operator private property protected public queue rational read resource
                return set stack startswith static string super switch this throw true try tuple typeof var void while with write
                yield
            ");

            scintilla.SetKeywords(1, @"
                abs acos asin atan atan2 ceil chr cos cosh deg2rad E eval exp floor format I log log10 log2 max MAXDATE MAXFLOAT
                MAXINT min MINDATE MINFLOAT MININT NAN NINFINITY NEWLINE now ord pack PI PINFINITY print println rad2deg rand
                randint readln round sign sin sinh sqrt tan tanh trunc unpack
            ");

            // Define styles
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Courier New";
            scintilla.Styles[Style.Default].Size = 12;
            scintilla.StyleClearAll();

            scintilla.Styles[Style.Cpp.Word].ForeColor = Color.Blue;
            scintilla.Styles[Style.Cpp.Word].Bold = true;

            scintilla.Styles[Style.Cpp.Word2].ForeColor = Color.DarkViolet;

            scintilla.Styles[Style.Cpp.Operator].ForeColor = Color.Navy;

            scintilla.Styles[Style.Cpp.StringRaw].ForeColor = Color.DarkCyan;

            scintilla.Styles[Style.Cpp.EscapeSequence].ForeColor = Color.Olive;

            scintilla.Styles[Style.BraceLight].ForeColor = Color.Red;
            scintilla.Styles[Style.BraceLight].BackColor = Color.WhiteSmoke;

            scintilla.Styles[Style.BraceBad].ForeColor = Color.WhiteSmoke;
            scintilla.Styles[Style.BraceBad].BackColor = Color.Red;

            int[] numberStyles = [Style.Cpp.Number, Style.Cpp.UserLiteral];

            foreach (int style in numberStyles)
            {
                scintilla.Styles[style].ForeColor = Color.Firebrick;
            }

            int[] textStyles = [Style.Cpp.Character, Style.Cpp.String, Style.Cpp.StringEol, Style.Cpp.Verbatim];

            foreach (int style in textStyles)
            {
                scintilla.Styles[style].ForeColor = Color.Green;
            }

            int[] commentStyles = [Style.Cpp.Comment, Style.Cpp.CommentLine, Style.Cpp.CommentDoc];

            foreach (int style in commentStyles)
            {
                scintilla.Styles[style].Size = scintilla.Styles[Style.Default].Size - 2;
                scintilla.Styles[style].Italic = true;
                scintilla.Styles[style].ForeColor = Color.Gray;
            }

            int[] marginStyles = [Style.LineNumber, Style.IndentGuide];

            foreach (int style in marginStyles)
            {
                scintilla.Styles[style].ForeColor = Color.Gray;
                scintilla.Styles[style].BackColor = Color.WhiteSmoke;
            }

            // Define margins
            Margin lineNumbers = scintilla.Margins[NUMBER_MARGIN];
            lineNumbers.Type = MarginType.Number;
            lineNumbers.Mask = 0;
            lineNumbers.Sensitive = true;

            Margin errors = scintilla.Margins[ERROR_MARGIN];
            errors.Type = MarginType.Symbol;
            errors.Mask = 1 << ERROR_MARKER;
            errors.Sensitive = true;
            errors.Width = DEFAULT_MARGIN_WIDTH;

            Margin codeFolding = scintilla.Margins[FOLDING_MARGIN];
            codeFolding.Type = MarginType.Symbol;
            codeFolding.Mask = Marker.MaskFolders;
            codeFolding.Sensitive = true;
            codeFolding.Width = DEFAULT_MARGIN_WIDTH;

            // Define an error marker
            var redBullet = new Bitmap(GetType(), "red_bullet.bmp");
            redBullet.MakeTransparent(Color.Magenta);
            scintilla.Markers[ERROR_MARKER].DefineRgbaImage(redBullet);

            // Define an error indicator
            Indicator indicator = scintilla.Indicators[ERROR_INDICATOR];
            indicator.Style = IndicatorStyle.Squiggle;
            indicator.ForeColor = Color.Red;

            // Configure folding markers with respective symbols
            scintilla.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            scintilla.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            scintilla.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            scintilla.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Styles for all folding markers
            for (int marker = Marker.FolderEnd; marker <= Marker.FolderOpen; ++marker)
            {
                scintilla.Markers[marker].SetForeColor(Color.Transparent);
                scintilla.Markers[marker].SetBackColor(Color.Gray);
            }

            // Disable specific key combinations as they are handled by the form
            scintilla.AssignCmdKey(Keys.Control | Keys.N, Command.Null);
            scintilla.AssignCmdKey(Keys.Control | Keys.O, Command.Null);
            scintilla.AssignCmdKey(Keys.Control | Keys.P, Command.Null);
            scintilla.AssignCmdKey(Keys.Control | Keys.F, Command.Null);
            scintilla.AssignCmdKey(Keys.Control | Keys.H, Command.Null);
            scintilla.AssignCmdKey(Keys.Control | Keys.Alt | Keys.Add, Command.Null);
            scintilla.AssignCmdKey(Keys.Control | Keys.Alt | Keys.Subtract, Command.Null);

            // Automaticcaly compute the line numbers margin width
            UpdateNumberMarginWidth();
        }

        private void InitializeAutocomplete()
        {
            // Set the TargetControlWrapper property of both autocomple menus
            keywordMenu.TargetControlWrapper = snippetMenu.TargetControlWrapper = new ScintillaWrapper(scintilla);

            // Keywords menu
            string keywords = @"
                abs?3 abstract?0 acos?3 as?0 asin?3 atan?3 atan2?3 blob?0 bool?1 break?0 case?0 catch?0 ceil?3 chr?3
                class?0 closure?1 complex?1 const?0 constructor?0 contains?4 continue?0 cos?3 cosh?3 date?1 decimal?1
                default?0 deg2rad?3 do?0 E?2 else?0 endswith?4 eval?3 event?0 exp?3 extern?0 false?2 final?0 finally?0
                float?1 floor?3 for?0 foreach?0 format?3 function?0 goto?0 I?2 if?0 import?0 in?4 int?1 is?4 list?1 log?3
                log10?3 log2?3 long?1 map?1 matches?4 max?3 MAXDATE?2 MAXFLOAT?2 MAXINT?2 min?3 MINDATE?2 MINFLOAT?2 MININT?2
                NAN?2 new?4 NEWLINE?2 NINFINITY?2 not?0 now?3 null?2 object?1 operator?0 ord?3 pack?3 PI?2 PINFINITY?2 print?3
                println?3 private?0 property?0 protected?0 public?0 queue?1 rad2deg?3 rand?3 randint?3 rational?1 read?5
                readln?3 resource?1 return?0 round?3 set?1 sign?3 sin?3 sinh?3 sqrt?3 stack?1 startswith?4 static?0 string?1
                super?0 switch?0 tan?3 tanh?3 this?5 throw?0 true?2 trunc?3 try?0 tuple?0 typeof?4 unpack?3 var?0 void?1 while?0
                with?0 write?5 yield?0
            ";

            foreach (string keyword in Regex.Split(keywords, @"\s+"))
            {
                if (keyword.Length <= 0) continue;

                string[] parts = keyword.Split('?');
                int imageIndex = int.Parse(parts[1]);
                keywordMenu.AddItem(new KeywordItem(parts[0], imageIndex, imageIndex == 3));
            }

            // Code snippets menu
            string[][] snippets = [
                ["if", "if (^) "],
                ["else", "else ^"],
                ["ifb", "if (^)\n{\n}"],
                ["elseb", "else\n{\t^\n}"],
                ["switch", "switch (^)\n{\n\tcase $label$:\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}"],
                ["for", "for (^;;)\n{\n}"],
                ["foreach", "foreach (^ in $sequence$)\n{\n}"],
                ["while", "while (^)\n{\n}"],
                ["do", "do\n{\n\t^\n} while ($condition$);"],
                ["try", "try\n{\n\t^\n}\ncatch (e)\n{\n}"],
                ["tryf", "try\n{\n\t^\n}\nfinally\n{\n}"],
                ["tcf", "try\n{\n\t^\n}\ncatch (e)\n{\n}\nfinally\n{\n}"],
                ["tryres", "try (^)\n{\n\t\n}"],
                ["function", "function $fname$(^)\n{\n}"],
                ["class", "class $cname$\n{\n}"],
                ["import", "import ^;"],
                ["impas", "import ^ as $alias$;"],
            ];

            foreach (string[] snippet in snippets)
            {
                snippetMenu.AddItem(new CodeSnippetItem(snippet[1], snippet[0], 6));
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Escapes a string that's intended to be used as a command line argument.
        /// </summary>
        /// <param name="arg">The string to escape</param>
        /// <returns><paramref name="arg"/> wrapped with double quotes with duplicated double quotes inside</returns>
        private static string EscapeCmdLineArg(string arg)
        {
            return $"\"{arg.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Checks if a <see cref="KeyEventArgs"/> instance matches the given configuration.
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs"/> to check</param>
        /// <param name="key">The expected <see cref="Keys"/> member</param>
        /// <param name="control">Tells whether the Control key should be pressed or not</param>
        /// <param name="alt">Tells whether the Alt key should be pressed or not</param>
        /// <param name="shift">Tells whether the Shift key should be pressed or not</param>
        /// <returns><b>true</b> is <paramref name="e"/> matches the configuration. <b>false</b> otherwise</returns>
        private static bool IsHotKey(KeyEventArgs e, Keys key, bool control = false, bool alt = false, bool shift = false)
        {
            return e.KeyCode == key && e.Control == control && e.Alt == alt && e.Shift == shift;
        }

        /// <summary>
        /// Checks if a character is a brace in the broad sense of the word.
        /// </summary>
        /// <param name="c">The character to test</param>
        /// <returns>A boolean</returns>
        private static bool IsBrace(int c)
        {
            return c switch
            {
                '(' or ')' or '[' or ']' or '{' or '}' or '<' or '>' => true,
                _ => false,
            };
        }

        /// <summary>
        /// Resets the environment.
        /// </summary>
        public void Reset()
        {
            scintilla.ClearAll();
            FilePath = null;
            Saved = true;
            scriptLength = 0;
            UpdateWindowBars();
        }

        /// <summary>
        /// Loads a script into the editor.
        /// </summary>
        /// <param name="path"></param>
        public void Open(string path)
        {
            scintilla.Text = File.ReadAllText(path);
            FilePath = path;
            Saved = true;
            scriptLength = scintilla.TextLength;
            UpdateWindowBars();
        }

        /// <summary>
        /// Saves a script to a file.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllText(path, scintilla.Text);
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
            scintilla.EmptyUndoBuffer();
            UpdateUndoRedoFileSize();
            UpdateCutCopyCaretInfo();
        }

        /// <summary>
        /// Updates the 'Undo', 'Redo' toolbar buttons as well as
        ///  the part of the status bar where the file size is displayed.
        /// </summary>
        private void UpdateUndoRedoFileSize()
        {
            undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = scintilla.CanUndo;
            redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = scintilla.CanRedo;
            buildToolStripMenuItem.Enabled = runToolStripMenuItem.Enabled = runToolStripButton.Enabled = scriptLength > 0;

            fileSizeStatuLabel.Text = string.Format(Resources.TextLength, scriptLength);

            if (!scintilla.CanUndo) Saved = true;
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
                                                  = scintilla.SelectedText.Length > 0;

            caretStatusLabel.Text = string.Format(Resources.CaretStatus,
                                                  scintilla.CurrentLine + 1,
                                                  scintilla.GetColumn(caretPosition) + 1,
                                                  scintilla.SelectedText.Length);
        }

        /// <summary>
        /// Dynamically computes de width of the line numbers margin.
        /// </summary>
        private void UpdateNumberMarginWidth()
        {
            string lastLineNumber = scintilla.Lines.Count.ToString();
            int marginWidth = 4 + scintilla.TextWidth(Style.LineNumber, lastLineNumber);
            scintilla.Margins[NUMBER_MARGIN].Width = Math.Max(marginWidth, DEFAULT_MARGIN_WIDTH);
        }

        /// <summary>
        /// Verifies that the mouse pointer is on a marker or not.
        /// </summary>
        /// <param name="x">The X co-ordonate of the mouse pointer</param>
        /// <param name="y">The Y co-ordonate of the mouse pointer</param>
        /// <returns><b>true</b> is the mouse is on a marker. <b>false</b> otherwise</returns>
        private bool IsMouseOnMarker(int x, int y)
        {
            if (x <= scintilla.Margins[NUMBER_MARGIN].Width ||
                x > scintilla.Margins[NUMBER_MARGIN].Width + scintilla.Margins[ERROR_MARGIN].Width)
                return false;

            int position = scintilla.CharPositionFromPoint(x, y);
            int line = scintilla.LineFromPosition(position);
            uint markerFlags = scintilla.Lines[line].MarkerGet();
            uint errMarkerMask = scintilla.Margins[ERROR_MARGIN].Mask;
            return (markerFlags & errMarkerMask) == errMarkerMask;
        }

        /// <summary>
        /// Verifies that the given position is on an indicator or not.
        /// </summary>
        /// <param name="indicator">The indicator's identifier</param>
        /// <param name="position">The X co-ordonate of the mouse pointer</param>
        /// <returns><b>true</b> is the given position is on an indicator. <b>false</b> otherwise</returns>
        private bool IsPositionOnIndicator(int position, int indicator)
        {
            int indicatorMask = 1 << indicator;
            uint indicatorFlags = scintilla.IndicatorAllOnFor(position);
            return (indicatorFlags & indicatorMask) == indicatorMask;
        }

        /// <summary>
        /// Verifies that a given position is between the boundaries of a comment or a string.
        /// </summary>
        /// <param name="position">The given position</param>
        /// <returns><b>true</b> if the caret is in a comment or a string literal. <b>false</b> otherwise</returns>
        private bool InCommentOrString(int position)
        {
            int style = scintilla.GetStyleAt(position);
            int prevStyle = scintilla.GetStyleAt(position - 1);
            int ch = scintilla.GetCharAt(position);
            int prevCh = scintilla.GetCharAt(position - 1);

            switch (style)
            {
                case Style.Cpp.Comment:
                case Style.Cpp.CommentDoc:
                    return ch != '/' || prevCh != '*';
                case Style.Cpp.CommentLine:
                case Style.Cpp.CommentLineDoc:
                    return ch != '\n';
                case Style.Cpp.Character:
                    return ch != '\'' || prevStyle != style || prevCh == '\\';
                case Style.Cpp.String:
                    return ch != '"' || prevStyle != style || prevCh == '\\';
                case Style.Cpp.StringEol:
                    return ch != '\n' || prevStyle != style;
                case Style.Cpp.Verbatim:
                    int prevPrevCh = scintilla.GetCharAt(position - 2);
                    return (ch != '"' && ch != '\'') || ch != prevCh || ch != prevPrevCh; // Note: Not perfect!
            }

            return false;
        }

        /// <summary>
        /// Gets the "word" at the caret position
        /// </summary>
        /// <returns>A string</returns>
        public string GetCurrentWord()
        {
            return scintilla.GetWordFromPosition(caretPosition);
        }

        /// <summary>
        /// Retrieves the "word" at the left of the current position.
        /// </summary>
        /// <returns>A string</returns>
        private string GetWordAtLeft()
        {
            int wordStart = scintilla.WordStartPosition(caretPosition, true);
            int prevWordStart = scintilla.WordStartPosition(wordStart - 1, true);
            return scintilla.GetWordFromPosition(prevWordStart);
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
            scintilla.CallTipShow(caretPosition, callTipInfo.Text);
            if (callTipInfo.ActiveParameter == null) return;
            scintilla.CallTipSetHlt(callTipInfo.ActiveParameter.Start, callTipInfo.ActiveParameter.End);
        }

        /// <summary>
        /// Renders an error with a marker and an indicator.
        /// </summary>
        /// <param name="element">The <see cref="ScriptElement"/> on which an error occured</param>
        private void ReportError(ScriptElement element)
        {
            scintilla.Lines[element.Start.LineNumber].MarkerAdd(ERROR_MARKER);
            scintilla.IndicatorCurrent = ERROR_INDICATOR;
            scintilla.IndicatorFillRange(element.Start.Offset, element.Length);
        }

        /// <summary>
        /// Clear any error indicator on the editor
        /// </summary>
        private void ClearErrors()
        {
            errorMessage = null;
            scintilla.MarkerDeleteAll(ERROR_MARKER);
            scintilla.IndicatorClearRange(0, scriptLength);
        }

        /// <summary>
        /// Selects a range of line in the Scintilla control.
        /// </summary>
        /// <param name="first">The first line number</param>
        /// <param name="last">The last line number</param>
        private void SelectLines(int first, int last)
        {
            scintilla.SetSelection(scintilla.Lines[first].Position, scintilla.Lines[last].EndPosition - 2);
        }

        /// <summary>
        /// Applies a smart indentation to the current line.
        /// </summary>
        private void SmartIndent()
        {
            scintilla.BeginUndoAction();

            Line currentLine = scintilla.Lines[scintilla.CurrentLine];
            Line previousLine = scintilla.Lines[scintilla.CurrentLine - 1];
            currentLine.Indentation = previousLine.Indentation;

            string prevLineText = previousLine.Text.TrimEnd();
            if (!(prevLineText.Length <= 0 || prevLineText.EndsWith(',') ||
                  prevLineText.EndsWith(';') || prevLineText.EndsWith('}')))
            {
                scintilla.InsertText(currentLine.IndentPosition, "\t");
            }

            scintilla.SetSelection(currentLine.IndentPosition, currentLine.IndentPosition);
            scintilla.EndUndoAction();
        }

        /// <summary>
        /// Hightlight matching braces.
        /// </summary>
        private void HighlightMatchingBraces()
        {
            // Has the caret changed position?
            int bracePos1 = -1;

            // Is there a brace to the left or right?
            if (caretPosition > 0 && IsBrace(scintilla.GetCharAt(caretPosition - 1)))
                bracePos1 = caretPosition - 1;
            else if (IsBrace(scintilla.GetCharAt(caretPosition)))
                bracePos1 = caretPosition;

            if (bracePos1 >= 0)
            {
                // Find the matching brace
                int bracePos2 = scintilla.BraceMatch(bracePos1);

                if (bracePos2 == Scintilla.InvalidPosition)
                {
                    scintilla.BraceBadLight(bracePos1);
                    scintilla.HighlightGuide = 0;
                }
                else
                {
                    scintilla.BraceHighlight(bracePos1, bracePos2);
                    scintilla.HighlightGuide = scintilla.GetColumn(bracePos1);
                }
            }
            else
            {
                // Turn off brace matching
                scintilla.BraceHighlight(Scintilla.InvalidPosition, Scintilla.InvalidPosition);
                scintilla.HighlightGuide = 0;
            }
        }

        /// <summary>
        /// Centers the find/replace window on this form
        /// </summary>
        private void CenterFindReplaceWindow()
        {
            Size deltaSize = Size - findReplace.Window.Size;
            findReplace.Window.Location = new Point(Left + deltaSize.Width / 2, Top + deltaSize.Height / 2);
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
            pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = scintilla.CanPaste;

            bool insLock = (GetKeyState(VK_INSERT) & 0xFFF) != 0;
            insLockStatusLabel.Text = insLock ? Resources.InsLoc : Resources.InsUnloc;

            bool capsLock = (GetKeyState(VK_CAPITAL) & 0xFFF) != 0;
            capsLockStatusLabel.Text = capsLock ? Resources.CapsLoc : string.Empty;

            bool numLock = (GetKeyState(VK_NUMLOCK) & 0xFFF) != 0;
            numLockStatusLabel.Text = numLock ? Resources.NumLoc : string.Empty;
        }

        #endregion

        #region Form's events

        private void TestForm_Load(object sender, EventArgs e)
        {
            Application.Idle += Application_Idle;

            if (!(string.IsNullOrEmpty(filePath) || Settings.Default.WindowSettings == null) &&
                Settings.Default.WindowSettings.TryGetValue(filePath, out WindowSettings ws))
            {
                scintilla.Zoom = ws.Zoom;
                caretPosition = ws.PositionInText;
                scintilla.SetSelection(caretPosition, caretPosition);
                scintilla.ScrollCaret();
                HighlightMatchingBraces();

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
                if (Settings.Default.WindowSettings == null) Settings.Default.WindowSettings = [];
                
                Settings.Default.WindowSettings[filePath] =
                    new WindowSettings(WindowState, Location, Size, scintilla.Zoom, caretPosition);
                
                Settings.Default.Save();
            }

            Application.Idle -= Application_Idle;
        }

        private void TestForm_SizeChanged(object sender, EventArgs e)
        {
            contentPane.Padding = paddingAdjuster.Adjust(contentPane);
        }

        private void TestForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsHotKey(e, Keys.N, true))
                newToolStripButton_Click(null, null);
            else if (IsHotKey(e, Keys.O, true))
                openToolStripButton_Click(null, null);
            else if (IsHotKey(e, Keys.P, true))
                printToolStripButton_Click(null, null);
            else if (IsHotKey(e, Keys.F, true))
                findToolStripButton_Click(null, null);
            else if (IsHotKey(e, Keys.H, true))
                replaceToolStripButton_Click(null, null);
            else if (IsHotKey(e, Keys.Add, true, true))
                commentLinesToolStripButton_Click(null, null);
            else if (IsHotKey(e, Keys.Subtract, true, true))
                uncommentLinesToolStripButton_Click(null, null);
            else
                return;

            e.SuppressKeyPress = true;
        }

        private void TestForm_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var fileExt = Path.GetExtension(files[0]).ToLower();

            if (fileExt == ".add" || fileExt == ".txt")
            {
                e.Effect = DragDropEffects.Copy;
                dndFilePath = files[0];
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
            var dialog = new OpenFileDialog
            {
                Title = Resources.OpenFileDialogTitle,
                Filter = Resources.FileDialogFilter,
                Multiselect = true,
            };

            using (dialog)
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;

                foreach (string fileName in dialog.FileNames)
                    EditorAppContext.OpenForm(fileName);

                if (string.IsNullOrEmpty(filePath) && scriptLength <= 0)
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
            var dialog = new SaveFileDialog
            {
                Title = Resources.SaveFileDialogTitle,
                Filter = Resources.FileDialogFilter,
            };

            using (dialog)
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    Save(dialog.FileName);
            }
        }

        private void exportToXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = Resources.ExportXmlTitle,
                    Filter = Resources.XmlFileFilter,
                };

                if (!string.IsNullOrEmpty(filePath))
                    dialog.FileName = Path.GetFileNameWithoutExtension(filePath) + dialog.DefaultExt;

                using (dialog)
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    var program = ScriptEngine.ParseString(scintilla.Text);
                    ScriptEngine.ExportXml(program, dialog.FileName);
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.ErrorMessageTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(filePath))
                printing.PrintDocument.DocumentName = Path.GetFileName(filePath);

            printing.Print();
        }

        private void undoToolStripButton_Click(object sender, EventArgs e)
        {
            scintilla.Undo();
        }

        private void redoToolStripButton_Click(object sender, EventArgs e)
        {
            scintilla.Redo();
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            scintilla.Cut();
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            scintilla.Copy();
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e)
        {
            scintilla.Paste();
        }

        private void findToolStripButton_Click(object sender, EventArgs e)
        {
            CenterFindReplaceWindow();
            findReplace.ShowFind();
        }

        private void replaceToolStripButton_Click(object sender, EventArgs e)
        {
            CenterFindReplaceWindow();
            findReplace.ShowReplace();
        }

        private void indentToolStripButton_Click(object sender, EventArgs e)
        {
            SendKeys.Send("{TAB}");
        }

        private void unindentToolStripButton_Click(object sender, EventArgs e)
        {
            SendKeys.Send("+{TAB}");
        }

        private void commentLinesToolStripButton_Click(object sender, EventArgs e)
        {
            int firstSelLine = scintilla.LineFromPosition(scintilla.SelectionStart);
            int lastSelLine = scintilla.LineFromPosition(scintilla.SelectionEnd);

            scintilla.BeginUndoAction();
            
            for (int selLine = firstSelLine; selLine <= lastSelLine; ++selLine)
                scintilla.InsertText(scintilla.Lines[selLine].Position, "//");
            
            SelectLines(firstSelLine, lastSelLine);
            scintilla.EndUndoAction();
        }

        private void uncommentLinesToolStripButton_Click(object sender, EventArgs e)
        {
            int firstSelLine = scintilla.LineFromPosition(scintilla.SelectionStart);
            int lastSelLine = scintilla.LineFromPosition(scintilla.SelectionEnd);

            scintilla.BeginUndoAction();
            
            for (int selLine = firstSelLine; selLine <= lastSelLine; ++selLine)
            {
                Line line = scintilla.Lines[selLine];
                if (line.Text.TrimStart().StartsWith("//"))
                {
                    int slashPos = line.Text.IndexOf('/');
                    scintilla.DeleteRange(line.Position + slashPos, 2);
                }
            }
            
            SelectLines(firstSelLine, lastSelLine);
            scintilla.EndUndoAction();
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /****************************************************************************
             * Parsing and running the script is delegated to asis.
             * *************************************************************************/
            string scriptPath;
            if (string.IsNullOrEmpty(filePath))
            {
                scriptPath = Path.ChangeExtension(Path.GetTempFileName(), ".add");
                File.WriteAllText(scriptPath, scintilla.Text);
            }
            else
            {
                scriptPath = filePath;
                if (!Saved) Save(scriptPath);
            }

            var argsBuilder = new StringBuilder();
            argsBuilder.Append("-f ").Append(EscapeCmdLineArg(scriptPath));

            foreach (var directory in EditorAppContext.Directories)
                argsBuilder.Append(" -d ").Append(EscapeCmdLineArg(directory));

            foreach (var assemblyName in EditorAppContext.Assemblies)
                argsBuilder.Append(" -r ").Append(EscapeCmdLineArg(assemblyName));

            string logPath = Path.ChangeExtension(Path.GetTempFileName(), ".log");
            argsBuilder.Append(" -l ").Append(EscapeCmdLineArg(logPath));

            ClearErrors();

            try
            {
                Process asis = Process.Start("asis", argsBuilder.ToString());
                asis.WaitForExit();

                if (asis.ExitCode <= 0) return;

                using var logReader = File.OpenText(logPath);
                if (logReader.ReadLine() != scriptPath) return;

                string[] parts = logReader.ReadLine().Split(',');
                var start = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

                parts = logReader.ReadLine().Split(',');
                var end = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

                errorMessage = logReader.ReadLine();
                ReportError(new ScriptElement(start, end));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.ErrorMessageTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (scriptPath != filePath) File.Delete(scriptPath);
                if (File.Exists(logPath)) File.Delete(logPath);
                Activate();
            }
        }

        private void buildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                Resources.NotImplemented,
                Resources.MissingFunctionality,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
            Process.Start(new ProcessStartInfo(HELP_LINK) { UseShellExecute = true });
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
            snippetMenu.Show(scintilla, !snippetMenu.Visible);
        }

        private void surroundWithToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //scintilla.Snippets.ShowSurroundWithList();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scintilla.Clear();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scintilla.SelectAll();
        }

        private void reformatCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var program = ScriptEngine.ParseString(scintilla.Text);
                scintilla.Text = ScriptEngine.GenerateCode(program);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.ErrorMessageTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Scintilla control's events

        private void scintilla_TextChanged(object sender, EventArgs e)
        {
            // Update the line numbers margin width
            UpdateNumberMarginWidth();

            // Handle text length changes
            if (scriptLength != scintilla.TextLength)
            {
                scriptLength = scintilla.TextLength;
                Saved = scriptLength <= 0 && string.IsNullOrEmpty(filePath);
                UpdateUndoRedoFileSize();
            }
        }

        private void scintilla_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            int newCaretPosition = scintilla.CurrentPosition;

            if (newCaretPosition != caretPosition)
            {
                caretPosition = newCaretPosition;
                UpdateCutCopyCaretInfo();
                HighlightMatchingBraces();
            }
        }

        private void scintilla_CharAdded(object sender, CharAddedEventArgs e)
        {
            /****************************************************************************
             * Tries to popup an keywordMenu menu or to display a calltip
             * *************************************************************************/
            if (InCommentOrString(caretPosition)) return;

            if (char.IsLetterOrDigit((char)e.Char))
            {
                string word = GetCurrentWord();

                foreach (string keyword in keywordMenu.Items)
                    if (keyword.StartsWith(word))
                    {
                        keywordMenu.Show(scintilla, !keywordMenu.Visible);
                        return;
                    }

                if (keywordMenu.Visible) keywordMenu.Close();
            }
            else
            {
                if (keywordMenu.Visible) keywordMenu.Close();

                switch (e.Char)
                {
                    case '(':
                        {
                            string word = GetWordAtLeft();
                            if (!CallTipProvider.IsDefined(word)) return;
                            PushCallTipInfo(CallTipProvider.GetCallTipInfo(word));
                            ShowCallTip();
                            return;
                        }
                    case ',':
                        if (callTipInfo == null) return;
                        scintilla.CallTipCancel();
                        if (!callTipInfo.NextParameter()) return;
                        ShowCallTip();
                        return;
                    case ')':
                        if (callTipInfo == null) return;
                        scintilla.CallTipCancel();
                        if ((callTipInfo = callTipInfo.Parent) == null) return;
                        ShowCallTip();
                        return;
                    case '\n':
                        SmartIndent();
                        return;
                }
            }
        }

        private void scintilla_DwellStart(object sender, DwellEventArgs e)
        {
            if (IsMouseOnMarker(e.X, e.Y))
                markerTip.Show(errorMessage, scintilla, e.X, e.Y - 45);
            else if (IsPositionOnIndicator(e.Position, ERROR_INDICATOR))
                scintilla.CallTipShow(e.Position, errorMessage);
            else if (e.Position >= 0 && !InCommentOrString(e.Position))
            {
                string word = scintilla.GetWordFromPosition(e.Position);
                if (!CallTipProvider.IsDefined(word)) return;

                callTipInfo = CallTipProvider.GetCallTipInfo(word);
                scintilla.CallTipShow(e.Position, callTipInfo.Text);
            }
        }

        private void scintilla_DwellEnd(object sender, DwellEventArgs e)
        {
            markerTip.Hide(scintilla);
            scintilla.CallTipCancel();
            callTipInfo = null;
        }

        private void scintilla_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            int position = scintilla.CharPositionFromPoint(e.X, e.Y);
            Console.WriteLine($"Style at mouse position {scintilla.GetStyleAt(position)}");
            Selection selection = scintilla.Selections[scintilla.MainSelection];

            if (position < selection.Start || position > selection.End)
                scintilla.SetSelection(position, position);
        }

        private void findReplace_KeyPressed(object sender, KeyEventArgs e)
        {
            if (IsHotKey(e, Keys.F, true))
                findReplace.ShowFind();
            else if (IsHotKey(e, Keys.H, true))
                findReplace.ShowReplace();
            else if (IsHotKey(e, Keys.I, true))
                findReplace.ShowIncrementalSearch();
            else if (IsHotKey(e, Keys.G, true))
                using (var goTo = new GoTo(scintilla))
                    goTo.ShowGoToDialog();
            else if (IsHotKey(e, Keys.F3))
                findReplace.Window.FindNext();
            else if (IsHotKey(e, Keys.F3, shift: true))
                findReplace.Window.FindPrevious();
            else
                return;

            e.SuppressKeyPress = true;
        }

        #endregion

        #endregion
    }
}
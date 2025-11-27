using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AddyScript.Gui.CodeCompletion;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Indentation.CSharp;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SR = AddyScript.Gui.Properties.Resources;
using MBI = MsBox.Avalonia.Enums.Icon;

namespace AddyScript.Gui;

public partial class MainWindow : Window
{
    #region Fields

    private readonly string TITLE_BASE = AssemblyInfo.Title;
    private const string HELP_LINK = "https://github.com/michelmbem/AddyScript/blob/master/docs/README.md";

    private readonly BraceFoldingStrategy foldingStrategy = new();
    private FoldingManager foldingManager;
    private CompletionWindow completionWindow;

    private string filePath;
    private bool saved;

    #endregion

    #region Initialization

    public MainWindow()
    {
        InitializeComponent();
        InitializeStyling();
    }

    private void InitializeStyling()
    {
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("AddyScript");
        
        Editor.Options.AllowToggleOverstrikeMode = true;
        Editor.Options.EnableTextDragDrop = true;
        Editor.Options.ShowBoxForControlCharacters = true;
        Editor.Options.ColumnRulerPositions = [80, 120];
        Editor.Options.HighlightCurrentLine = true;

        Editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(Editor.Options);
        Editor.TextArea.Caret.PositionChanged += EditorCaretPositionChanged;
        Editor.TextArea.SelectionChanged += EditorSelectionChanged;
        Editor.TextArea.TextEntering += EditorTextEntering;
        Editor.TextArea.TextEntered += EditorTextEntered;
    }

    private void InitializeCodeCompletion()
    {
        completionWindow = new CompletionWindow(Editor.TextArea);
        var completionData = completionWindow.CompletionList.CompletionData;
        
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
            completionData.Add(new KeywordData(parts[0], imageIndex));
        }
    }

    private void InitializeFolding()
    {
        if (foldingManager != null)
            FoldingManager.Uninstall(foldingManager);

        foldingManager = FoldingManager.Install(Editor.TextArea);
        foldingStrategy.UpdateFoldings(foldingManager, Editor.Document);
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
                FileNameStatusLabel.Content = "Untitled";
                Title = TITLE_BASE;
            }
            else
            {
                FileNameStatusLabel.Content = value;
                Title = $"{Path.GetFileName(value)} - {TITLE_BASE}";
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

            var title = Title ?? string.Empty;
            if (value && title.EndsWith('*'))
                Title = title[..^1];
            else if (!(value || title.EndsWith('*')))
                Title += "*";
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Escapes a string intended to be used as a command line argument.
    /// </summary>
    /// <param name="arg">The string to escape</param>
    /// <returns><paramref name="arg"/> wrapped with double quotes with duplicated double quotes inside</returns>
    private static string EscapeCmdLineArg(string arg) => $"\"{arg.Replace("\"", "\"\"")}\"";

    /// <summary>
    /// Checks if a <see cref="KeyEventArgs"/> instance matches the given configuration.
    /// </summary>
    /// <param name="e">The <see cref="KeyEventArgs"/> to check</param>
    /// <param name="key">The expected <see cref="Key"/> member</param>
    /// <param name="modifiers">Tells whether one or any of the Control/Alt/System/Shift keys should be pressed or not</param>
    /// <returns><b>true</b> is <paramref name="e"/> matches the configuration. <b>false</b> otherwise</returns>
    private static bool IsHotKey(KeyEventArgs e, Key key, KeyModifiers modifiers = KeyModifiers.None) =>
        e.Key == key && (e.KeyModifiers & modifiers) == modifiers;

    /// <summary>
    /// Checks if a character is a brace in the broad sense of the word.
    /// </summary>
    /// <param name="c">The character to test</param>
    /// <returns>A boolean</returns>
    private static bool IsBrace(int c) => c switch
    {
        '(' or ')' or '[' or ']' or '{' or '}' => true,
        _ => false,
    };

    /// <summary>
    /// Resets the environment.
    /// </summary>
    public void Reset()
    {
        var document = new TextDocument();
        document.Changed += EditorDocumentChanged;
        Editor.Document = document;
        FilePath = null;
        Saved = true;
        InitializeFolding();
        UpdateWindowBars();
    }

    /// <summary>
    /// Loads a script into the editor.
    /// </summary>
    /// <param name="path"></param>
    public void Open(string path)
    {
        var document = new TextDocument(File.ReadAllText(path));
        document.Changed += EditorDocumentChanged;
        Editor.Document = document;
        FilePath = path;
        Saved = true;
        InitializeFolding();
        UpdateWindowBars();
    }

    /// <summary>
    /// Saves a script to a file.
    /// </summary>
    /// <param name="path"></param>
    public void Save(string path)
    {
        File.WriteAllText(path, Editor.Document.Text);
        FilePath = path;
        Saved = true;
    }

    private void CloseIfEmpty()
    {
        if (Saved && Editor.Document.TextLength <= 0)
            Close();
    }

    private async Task OpenAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = SR.OpenFileDialogTitle,
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType(SR.FileDialogFilter) { Patterns = ["*.add", "*.txt"] },
                    FilePickerFileTypes.All
                ]
            });

        if (files.Count > 0)
        {
            App.OpenWindow(files[0].Path.LocalPath);
            CloseIfEmpty();
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(filePath))
            await SaveAsAsync();
        else
            Save(FilePath);
    }

    private async Task SaveAsAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = SR.SaveFileDialogTitle,
            DefaultExtension = ".add",
            FileTypeChoices =
            [
                new FilePickerFileType(SR.FileDialogFilter) { Patterns = ["*.add", "*.txt"] },
                FilePickerFileTypes.All
            ]
        });

        if (file is not null)
            Save(file.Path.LocalPath);
    }

    private async Task ExportXmlAsync()
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = SR.ExportXmlTitle,
                DefaultExtension = ".xml",
                FileTypeChoices =
                [
                    new FilePickerFileType(SR.XmlFileFilter) { Patterns = ["*.xml"] },
                    FilePickerFileTypes.All
                ],
                SuggestedFileName = !string.IsNullOrEmpty(filePath)
                    ? Path.GetFileNameWithoutExtension(filePath) + ".xml"
                    : "untitled.xml"
            });

            if (file is null) return;

            var program = ScriptEngine.ParseString(Editor.Text);
            ScriptEngine.ExportXml(program, file.Path.LocalPath);
            Process.Start(new ProcessStartInfo(file.Path.LocalPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            await MessageBoxManager
                .GetMessageBoxStandard(SR.ErrorMessageTitle, ex.Message, ButtonEnum.Ok, MBI.Error)
                .ShowAsync();
        }
    }

    /// <summary>
    /// Prompts the user to save the script before leaving.
    /// </summary>
    /// <returns>true to continue; false to cancel the current action</returns>
    private async Task<bool> PromptToSave()
    {
        var answer = await MessageBoxManager
            .GetMessageBoxStandard(Title!, SR.PromptToSave, ButtonEnum.YesNoCancel, MBI.Question)
            .ShowAsync();

        switch (answer)
        {
            case ButtonResult.Yes:
                await SaveAsync();
                break;
            case ButtonResult.Cancel:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Updates some items in the toolbar and the statusbar.
    /// </summary>
    private void UpdateWindowBars()
    {
        Editor.Document.UndoStack.ClearAll();
        UpdateUndoRedoFileSize();
        UpdateCutCopyCaretInfo();
    }

    /// <summary>
    /// Updates the 'Undo', 'Redo' toolbar buttons as well as
    ///  the part of the status bar where the file size is displayed.
    /// </summary>
    private void UpdateUndoRedoFileSize()
    {
        ToolbarUndoButton.IsEnabled = UndoMenuItem.IsEnabled = Editor.CanUndo;
        ToolbarRedoButton.IsEnabled = RedoMenuItem.IsEnabled = Editor.CanRedo;
        ToolbarRunButton.IsEnabled = Editor.Document.TextLength > 0;

        TextLengthStatusLabel.Content = string.Format(SR.TextLength, Editor.Document.TextLength);

        if (!Editor.CanUndo) Saved = true;
    }

    /// <summary>
    /// Updates the 'Cut', 'Copy' toolbar buttons as well as
    /// the part of the status bar where the caret info are shown.
    /// </summary>
    private void UpdateCutCopyCaretInfo()
    {
        ToolbarCutButton.IsEnabled = CutMenuItem.IsEnabled = Editor.CanCut;
        ToolbarCopyButton.IsEnabled = CopyMenuItem.IsEnabled = Editor.CanCopy;

        CaretStatusLabel.Content = string.Format(
            SR.CaretStatus,
            Editor.TextArea.Caret.Line,
            Editor.TextArea.Caret.Column,
            Editor.TextArea.Selection.Length);
    }

    /// <summary>
    /// Verifies that a given position is between the boundaries of a comment or a string.
    /// </summary>
    /// <param name="position">The given position</param>
    /// <returns><b>true</b> if the caret is in a comment or a string literal. <b>false</b> otherwise</returns>
    private bool InCommentOrString(int position)
    {
        return false;
    }

    private void OpenSearchPanel(bool replaceMode)
    {
        var selection = Editor.TextArea.Selection;
        Editor.SearchPanel.SearchPattern = selection.IsEmpty || selection.IsMultiline
            ? string.Empty
            : selection.GetText();
        Editor.SearchPanel.IsReplaceMode = replaceMode;
        Editor.SearchPanel.Open();
    }

    private void ReportError(string errorMessage, ScriptLocation start, ScriptLocation end)
    {
        // TODO: Highlight the error in the editor
        Console.WriteLine($@"{errorMessage} @{start}:{end}");
    }

    #endregion

    #region Event handlers

    #region Window events

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.AwaitWithPriority(
            new Task(() => ToolbarPasteButton.IsEnabled = Editor.CanPaste),
            DispatcherPriority.ApplicationIdle);
        
        InitializeCodeCompletion();
        
        Editor.TextArea.Focus();
    }

    private async void WindowClosing(object sender, WindowClosingEventArgs e)
    {
        if (Saved) return;

        e.Cancel = true;

        if (await PromptToSave())
        {
            Closing -= WindowClosing;
            Close();
        }
    }

    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (IsHotKey(e, Key.I, KeyModifiers.Meta))
        {
            InsertSnippetMenuItemClick(null, null);
            e.Handled = true;
        }
        else if (IsHotKey(e, Key.I, KeyModifiers.Meta | KeyModifiers.Shift))
        {
            SurroundWithMenuItemClick(null, null);
            e.Handled = true;
        }
    }

    #endregion

    #region Toolbar events

    public void ToolbarNewButtonClick(object sender, RoutedEventArgs e)
    {
        App.OpenWindow();
        CloseIfEmpty();
    }

    public void ToolbarOpenButtonClick(object sender, RoutedEventArgs e)
    {
        _ = OpenAsync();
    }

    public void ToolbarSaveButtonClick(object sender, RoutedEventArgs e)
    {
        _ = SaveAsync();
    }

    private void ToolbarSaveAsMenuItemClick(object sender, RoutedEventArgs e)
    {
        _ = SaveAsAsync();
    }

    private void ToolbarExportXmlMenuItemClick(object sender, RoutedEventArgs e)
    {
        _ = ExportXmlAsync();
    }

    public void ToolbarPrintButtonClick(object sender, RoutedEventArgs e)
    {
        MessageBoxManager
            .GetMessageBoxStandard(Title!, SR.MissingFunctionality, ButtonEnum.Ok, MBI.Warning)
            .ShowAsync();
    }

    public void ToolbarUndoButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Undo();
    }

    public void ToolbarRedoButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Redo();
    }

    public void ToolbarCutButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Cut();
    }

    public void ToolbarCopyButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Copy();
    }

    public void ToolbarPasteButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Paste();
    }

    private void ToolbarFindButtonClick(object sender, RoutedEventArgs e)
    {
        OpenSearchPanel(false);
    }

    private void ToolbarReplaceButtonClick(object sender, RoutedEventArgs e)
    {
        OpenSearchPanel(true);
    }

    private void ToolbarOutdentButtonClick(object sender, RoutedEventArgs e)
    {
        var selection = Editor.TextArea.Selection;

        if (!selection.IsMultiline) return;

        for (var i = selection.StartPosition.Line - 1; i < selection.EndPosition.Line; ++i)
        {
            var line = Editor.Document.Lines[i];
            if (Editor.Document.GetCharAt(line.Offset) == '\t')
                Editor.Document.Remove(line.Offset, 1);
        }
    }

    private void ToolbarIndentButtonClick(object sender, RoutedEventArgs e)
    {
        var selection = Editor.TextArea.Selection;

        if (!selection.IsMultiline) return;

        for (var i = selection.StartPosition.Line - 1; i < selection.EndPosition.Line; ++i)
        {
            var line = Editor.Document.Lines[i];
            Editor.Document.Insert(line.Offset, "\t");
        }
    }

    private void ToolbarCommentButtonClick(object sender, RoutedEventArgs e)
    {
        var selection = Editor.TextArea.Selection;

        if (!selection.IsMultiline) return;

        for (var i = selection.StartPosition.Line - 1; i < selection.EndPosition.Line; ++i)
        {
            var line = Editor.Document.Lines[i];
            Editor.Document.Insert(line.Offset, "//");
        }
    }

    private void ToolbarUncommentButtonClick(object sender, RoutedEventArgs e)
    {
        var selection = Editor.TextArea.Selection;

        if (!selection.IsMultiline) return;

        for (var i = selection.StartPosition.Line - 1; i < selection.EndPosition.Line; ++i)
        {
            var line = Editor.Document.Lines[i];
            if (Editor.Document.GetCharAt(line.Offset) == '/' && Editor.Document.GetCharAt(line.Offset + 1) == '/')
                Editor.Document.Remove(line.Offset, 2);
        }
    }

    public void ToolbarRunButtonClick(object sender, RoutedEventArgs e)
    {
        /****************************************************************************
         * Parsing and running the script is delegated to asis.
         * *************************************************************************/
        string scriptPath;
        if (string.IsNullOrEmpty(filePath))
        {
            scriptPath = Path.ChangeExtension(Path.GetTempFileName(), ".add");
            File.WriteAllText(scriptPath, Editor.Text);
        }
        else
        {
            scriptPath = filePath;
            if (!Saved) Save(scriptPath);
        }

        var argsBuilder = new StringBuilder();
        argsBuilder.Append("-f ").Append(EscapeCmdLineArg(scriptPath));

        foreach (var directory in App.SearchPaths)
            argsBuilder.Append(" -d ").Append(EscapeCmdLineArg(directory));

        foreach (var assemblyName in App.References)
            argsBuilder.Append(" -r ").Append(EscapeCmdLineArg(assemblyName));

        var logPath = Path.ChangeExtension(Path.GetTempFileName(), ".log");
        argsBuilder.Append(" -l ").Append(EscapeCmdLineArg(logPath));

        //ClearErrors();

        try
        {
            var asis = Process.Start("asis", argsBuilder.ToString());
            asis!.WaitForExit();

            if (asis.ExitCode <= 0) return;

            using var logReader = File.OpenText(logPath);
            if (logReader.ReadLine() != scriptPath) return;

            var parts = logReader.ReadLine()!.Split(',');
            var start = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

            parts = logReader.ReadLine()!.Split(',');
            var end = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

            var errorMessage = logReader.ReadLine();
            ReportError(errorMessage, start, end);
            ;
        }
        catch (Exception ex)
        {
            MessageBoxManager
                .GetMessageBoxStandard(SR.ErrorMessageTitle, ex.Message, ButtonEnum.Ok, MBI.Error)
                .ShowAsync();
        }
        finally
        {
            if (scriptPath != filePath) File.Delete(scriptPath);
            if (File.Exists(logPath)) File.Delete(logPath);
            Activate();
        }
    }

    public async void ToolbarConfigButtonClick(object sender, RoutedEventArgs e)
    {
        var optionDialog = new OptionDialog();
        if (await optionDialog.ShowDialog<bool>(this))
        {
            App.SearchPaths = [..optionDialog.SearchPaths];
            App.References = [..optionDialog.References];
        }
    }

    public void ToolbarHelpButtonClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(HELP_LINK) { UseShellExecute = true });
    }

    private async void ToolbarHelpAboutMenuItemClick(object sender, RoutedEventArgs e)
    {
        var aboutBox = new AboutBox();
        await aboutBox.ShowDialog<bool>(this);
    }

    #endregion

    #region Editor events

    private void EditorCaretPositionChanged(object sender, EventArgs e)
    {
        UpdateCutCopyCaretInfo();
    }

    private void EditorSelectionChanged(object sender, EventArgs e)
    {
        UpdateCutCopyCaretInfo();
    }

    private void EditorDocumentChanged(object sender, DocumentChangeEventArgs e)
    {
        Saved = Editor.Document.TextLength <= 0 && string.IsNullOrEmpty(filePath);
        foldingStrategy.UpdateFoldings(foldingManager, Editor.Document);
        UpdateUndoRedoFileSize();
    }

    private void EditorTextEntering(object sender, TextInputEventArgs e)
    {
        Console.WriteLine($@"Text entering: {e.Text}");
    }

    private void EditorTextEntered(object sender, TextInputEventArgs e)
    {
        Console.WriteLine($"Text entered: {e.Text}");
        if (InCommentOrString(Editor.CaretOffset)) return;

        if (Regex.IsMatch(e.Text, @"^[a-zA-Z_]$"))
        {
            Console.WriteLine("Word character entered");
            var keywords = completionWindow.CompletionList.CompletionData;
            foreach (var keyword in keywords)
                if (keyword.Text.StartsWith(e.Text))
                {
                    Console.WriteLine($"Matches with keyword: {keyword.Text}");
                    completionWindow.Show();
                    return;
                }

            if (completionWindow.IsOpen) completionWindow.Close();
        }
        // TODO: else try to show calltip
    }

    #endregion

    #region Editor menu events

    private void InsertSnippetMenuItemClick(object sender, RoutedEventArgs e)
    {
        MessageBoxManager
            .GetMessageBoxStandard(Title!, SR.MissingFunctionality, ButtonEnum.Ok, MBI.Warning)
            .ShowAsync();
    }

    private void SurroundWithMenuItemClick(object sender, RoutedEventArgs e)
    {
        MessageBoxManager
            .GetMessageBoxStandard(Title!, SR.MissingFunctionality, ButtonEnum.Ok, MBI.Warning)
            .ShowAsync();
    }

    private void DeleteMenuItemClick(object sender, RoutedEventArgs e)
    {
        Editor.Delete();
    }

    private void SelectAllMenuItemClick(object sender, RoutedEventArgs e)
    {
        Editor.Select(0, Editor.Document.TextLength);
    }

    private void ReformatMenuItemClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var document = Editor.Document;
            var program = ScriptEngine.ParseString(document.Text);
            document.Text = ScriptEngine.GenerateCode(program);
        }
        catch (Exception ex)
        {
            MessageBoxManager
                .GetMessageBoxStandard(Title!, ex.Message, ButtonEnum.Ok, MBI.Warning)
                .ShowAsync();
        }
    }

    #endregion

    #endregion
}
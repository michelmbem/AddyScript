using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddyScript.Gui.Autocomplete;
using AddyScript.Gui.CallTips;
using AddyScript.Gui.Markers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MBI = MsBox.Avalonia.Enums.Icon;
using SR = AddyScript.Gui.Properties.Resources;

namespace AddyScript.Gui;

public partial class MainWindow : Window
{
    #region Fields

    private const string HELP_LINK = App.REPO_URL + "/blob/master/docs/README.md";

    private readonly MarkerMargin markerMargin = new();
    private readonly FoldingStrategy foldingStrategy = new();
    private readonly Stack<CallTipInfo> callTipStack = [];

    private FoldingManager foldingManager;
    private TextMarkerService textMarkerService;
    private CompletionWindow completionWindow;
    private OverloadInsightWindow insightWindow;
    private DispatcherTimer timer;

    private string filePath;
    private bool saved;

    #endregion

    #region Initialization

    public MainWindow()
    {
        InitializeComponent();
        InitializeStyling();
        InitializeTimer();
    }

    private void InitializeStyling()
    {
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("AddyScript");
        
        TextEditorOptions options = Editor.Options;
        options.AllowToggleOverstrikeMode = true;
        options.EnableTextDragDrop = true;
        options.ShowBoxForControlCharacters = true;
        options.ColumnRulerPositions = [80, 120];
        options.HighlightCurrentLine = true;

        TextArea textArea = Editor.TextArea;
        textArea.LeftMargins.Insert(0, markerMargin);
        textArea.IndentationStrategy = new CSharpIndentationStrategy(options);
        textArea.Caret.PositionChanged += EditorCaretPositionChanged;
        textArea.SelectionChanged += EditorSelectionChanged;
        textArea.TextEntering += EditorTextEntering;
        textArea.TextEntered += EditorTextEntered;
        
        textMarkerService = new TextMarkerService(Editor);
        TextView textView = textArea.TextView;
        textView.BackgroundRenderers.Add(textMarkerService);
        textView.PointerMoved += EditorTextViewPointerMoved;
        textView.PointerExited += (s, e) => ToolTip.SetIsOpen(Editor, false);
    }

    private void InitializeTimer()
    {
        timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        
        timer.Tick += (s, e) =>
            ToolbarPasteButton.IsEnabled = PasteMenuItem.IsEnabled = Editor.CanPaste;
        
        timer.Start();
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
                Title = AssemblyInfo.Title;
            }
            else
            {
                FileNameStatusLabel.Content = value;
                Title = $"{Path.GetFileName(value)} - {AssemblyInfo.Title}";
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

    /// <summary>
    /// Gets the current CallTipInfo
    /// </summary>
    private CallTipInfo CurrentCallTip => callTipStack.Peek();

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

    /// <summary>
    /// Closes the window if the document is empty and unchanged.
    /// </summary>
    private void CloseIfEmpty()
    {
        if (Saved && Editor.Document.TextLength == 0)
            Close();
    }

    /// <summary>
    /// Displays an Open File dialog and opens the selected file.
    /// </summary>
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

    /// <summary>
    /// Saves the script. If no file is associated, displays a Save File dialog.
    /// </summary>
    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(filePath))
            await SaveAsAsync();
        else
            Save(FilePath);
    }

    /// <summary>
    /// Displays a Save File dialog and saves the script to the selected file.
    /// </summary>
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

    /// <summary>
    /// Exports the script as an XML representation.
    /// </summary>
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
        SurroundWithMenuItem.IsEnabled = !Editor.TextArea.Selection.IsEmpty;

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
        // Retrieves the highlighter
        var highlighter = Editor.TextArea.GetService(typeof(IHighlighter)) as IHighlighter;
        if (highlighter == null) return false;

        // Retrieves the highlighted line
        DocumentLine line = Editor.Document.GetLineByOffset(position);
        // Apply highlighting to the line
        HighlightedLine highlightedLine = highlighter.HighlightLine(line.LineNumber);

        // Check if line's coloration at the given position is a comment or a string
        return highlightedLine.Sections.Any(s =>
            s.Offset <= position &&
            position < s.Offset + s.Length &&
            (s.Color?.Name == "Comment" || s.Color?.Name == "String")
        );
    }

    /// <summary>
    /// Retrieves the "word" at the caret position.
    /// </summary>
    /// <returns>A string</returns>
    private string GetCurrentWord()
    {
        int caretOffset = Editor.CaretOffset;

        // Edge case: caret at the beginning of the document
        if (caretOffset <= 0) return string.Empty;

        TextDocument document = Editor.Document;

        // Find the start of the current word
        int wordStart = TextUtilities.GetNextCaretPosition(
            document,
            caretOffset,
            LogicalDirection.Backward,
            CaretPositioningMode.WordStart);

        return wordStart < 0 || wordStart >= caretOffset
             ? string.Empty
             : document.GetText(wordStart, caretOffset - wordStart);
    }

    /// <summary>
    /// Gets the "word" that precedes the "word" at caret position.
    /// </summary>
    /// <returns>A string</returns>
    public string GetWordAtLeft()
    {
        TextDocument document = Editor.Document;

        // Find the start of the current word
        int wordStart = TextUtilities.GetNextCaretPosition(
            document,
            Editor.CaretOffset,
            LogicalDirection.Backward,
            CaretPositioningMode.WordStart);

        // Find the start of the previous word
        wordStart = TextUtilities.GetNextCaretPosition(
            document,
            wordStart - 1,
            LogicalDirection.Backward,
            CaretPositioningMode.WordStart);

        // Find the end of the previous word
        int wordEnd = TextUtilities.GetNextCaretPosition(
            document,
            wordStart,
            LogicalDirection.Forward,
            CaretPositioningMode.WordBorder);

        return (wordStart < 0 || wordEnd < 0 || wordEnd <= wordStart)
             ? string.Empty
             : document.GetText(wordStart, wordEnd - wordStart);
    }

    /// <summary>
    /// Opens the completion window with the given data.
    /// </summary>
    /// <typeparam name="T">The type of the completion data</typeparam>
    /// <param name="completionData">The completion data to display in the menu</param>
    private void ShowCompletionWindow<T>(List<T> completionData)
        where T : ICompletionData
    {
        completionWindow = new CompletionWindow(Editor.TextArea);
        
        foreach (var dataItem in completionData)
            completionWindow.CompletionList.CompletionData.Add(dataItem);

        completionWindow.Closed += (s, e) => completionWindow = null;
        completionWindow.Show();
    }

    /// <summary>
    /// Checks if the completion window is open.
    /// </summary>
    /// <returns><b>true</b> if the completion window is non-null and visible. <b>false</b> otherwise</returns>
    private bool IsCompletionWindowOpen() => completionWindow?.IsOpen == true;

    /// <summary>
    /// Pushes a new CallTipInfo on top of the stack.
    /// </summary>
    /// <param name="callTip">The new CallTipInfo</param>
    private void PushCallTip(CallTipInfo callTip)
    {
        callTipStack.Push(callTip);
        callTip.Reset();
    }

    /// <summary>
    /// Pops a CallTipInfo from the stack.
    /// </summary>
    /// <returns><b>true</b> if there is still at least one CallTipInfo in the stack. <b>false</b> otherwise</returns>
    private bool PopCallTip()
    {
        callTipStack.Pop();
        return callTipStack.Count > 0;
    }

    /// <summary>
    /// Shows a call tip according to the current callTipInfo.
    /// </summary>
    private void ShowCallTip()
    {
        insightWindow = new OverloadInsightWindow(Editor.TextArea)
        {
            Provider = new SimpleOverloadProvider(CurrentCallTip)
        };

        insightWindow.Closed += (s, e) =>  insightWindow = null;
        insightWindow.Show();
    }

    /// <summary>
    /// Opens the search panel in either search or replace mode.
    /// </summary>
    /// <param name="replaceMode">Whether the search panel should be open in replace mode or not</param>
    private void OpenSearchPanel(bool replaceMode)
    {
        Editor.SearchPanel?.Uninstall();
        var searchPanel = SearchPanel.Install(Editor);
        var selection = Editor.TextArea.Selection;
        searchPanel.SearchPattern = selection.IsEmpty || selection.IsMultiline ? string.Empty : selection.GetText();
        searchPanel.IsReplaceMode = replaceMode;
        searchPanel.Open();
    }

    /// <summary>
    /// Displays an error message related to a script execution.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="start">Initial location of the erroneous symbol in code</param>
    /// <param name="end">Ending location of the erroneous symbol in code</param>
    private void ReportError(string errorMessage, ScriptLocation start, ScriptLocation end)
    {
        markerMargin.AddMarker(start.LineNumber + 1, errorMessage);
        textMarkerService.AddMarker(new(start.Offset, end.Offset) { ToolTip = errorMessage });
    }
    
    /// <summary>
    /// Deletes all error markers.
    /// </summary>
    private void ClearErrors()
    {
        markerMargin.ClearMarkers();
        textMarkerService.ClearMarkers();
    }

    #endregion

    #region Event handlers

    #region Window events

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
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
        else if (IsHotKey(e, Key.R, KeyModifiers.Meta))
        {
            ReformatMenuItemClick(null, null);
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

        var argsBuilder = new StringBuilder().Append("-f ").Append(EscapeCmdLineArg(scriptPath));

        foreach (var directory in App.SearchPaths)
            argsBuilder.Append(" -d ").Append(EscapeCmdLineArg(directory));

        foreach (var assemblyName in App.References)
            argsBuilder.Append(" -r ").Append(EscapeCmdLineArg(assemblyName));

        var logPath = Path.ChangeExtension(Path.GetTempFileName(), ".log");
        argsBuilder.Append(" -l ").Append(EscapeCmdLineArg(logPath));

        ClearErrors();

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
        TextDocument document = Editor.Document;
        foldingStrategy.UpdateFoldings(foldingManager, document);
        Saved = document.TextLength == 0 && string.IsNullOrEmpty(filePath);
        UpdateUndoRedoFileSize();
    }

    private void EditorTextEntering(object sender, TextInputEventArgs e)
    {
        // unimplemented
    }

    private void EditorTextEntered(object sender, TextInputEventArgs e)
    {
        /****************************************************************************
        * Tries to popup an keywordMenu menu or to display a calltip
        * *************************************************************************/

        if (InCommentOrString(Editor.CaretOffset)) return;

        char firstChar = e.Text![0];

        if (char.IsLetterOrDigit(firstChar))
        {
            if (IsCompletionWindowOpen()) return;

            var matchedKeywords = KeywordData.AllMatching(GetCurrentWord());
            if (matchedKeywords.Count == 0) return;

            ShowCompletionWindow(matchedKeywords);
        }
        else
        {
            if (IsCompletionWindowOpen()) completionWindow.Close();

            switch (firstChar)
            {
                case '(':
                {
                    string wordAtLeft = GetWordAtLeft();
                    if (!CallTipProvider.IsDefined(wordAtLeft)) return;
                    PushCallTip(CallTipProvider.GetCallTipInfo(wordAtLeft));
                    ShowCallTip();
                    return;
                }
                case ',':
                    if (callTipStack.Count == 0) return;
                    insightWindow.Close();
                    if (!CurrentCallTip.NextParameter()) return;
                    ShowCallTip();
                    return;
                case ')':
                    if (callTipStack.Count == 0) return;
                    insightWindow.Close();
                    if (!PopCallTip()) return;
                    ShowCallTip();
                    return;
            }
        }
    }

    private void EditorTextViewPointerMoved(object sender, PointerEventArgs e)
    {
        if (sender is not TextView { VisualLinesValid: true } textView) return;

        // convert mouse → document offset
        var vp = textView.GetPosition(e.GetPosition(textView));

        if (vp.HasValue)
        {
            var marker = textMarkerService.GetMarkerAt(vp);
            
            if (marker == null)
                ToolTip.SetIsOpen(Editor, false);
            else
            {
                ToolTip.SetTip(Editor, marker.ToolTip);
                ToolTip.SetIsOpen(Editor, true);
            }
        }
        else
            ToolTip.SetIsOpen(Editor, false);
    }

    #endregion

    #region Editor menu events

    private void InsertSnippetMenuItemClick(object sender, RoutedEventArgs e)
    {
        ShowCompletionWindow(CodeSnippetData.All);
    }

    private void SurroundWithMenuItemClick(object sender, RoutedEventArgs e)
    {
        ShowCompletionWindow(SurroundCodeData.All);
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
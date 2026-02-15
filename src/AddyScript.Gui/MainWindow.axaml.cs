using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AddyScript.Gui.Autocomplete;
using AddyScript.Gui.CallTips;
using AddyScript.Gui.Configuration;
using AddyScript.Gui.Extensions;
using AddyScript.Gui.Markers;
using AddyScript.Gui.Terminal;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
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

public enum CompletionStage
{
    None,
    InProgress,
    Complete
}

public partial class MainWindow : Window
{
    #region Fields

    private const string WIKI_LINK = "https://michelmbem.github.io/AddyScript/";

    private static readonly string WorkDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AssemblyInfo.Title);

    private readonly MarkerMargin markerMargin = new();
    private readonly FoldingStrategy foldingStrategy = new();
    private readonly Stack<CallTipInfo> callTipStack = [];

    private FoldingManager foldingManager;
    private TextMarkerService textMarkerService;
    private CompletionWindow completionWindow;
    private OverloadInsightWindow insightWindow;
    private SearchPanel searchPanel;
    private DispatcherTimer idleTimer;
    private DispatcherTimer dwellTimer;
    private TextViewPosition? hoverPosition;
    private CompletionStage completionStage;
    private bool updateFolding;

    private string filePath;
    private bool saved;

    #endregion

    #region Initialization

    public MainWindow()
    {
        InitializeComponent();
        InitializeStyling();
        InitializeTimers();
    }

    private void InitializeStyling()
    {
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("AddyScript");

        TextEditorOptions options = Editor.Options;
        options.AllowToggleOverstrikeMode = true;
        options.EnableTextDragDrop = true;
        options.ShowBoxForControlCharacters = true;
        options.ColumnRulerPositions = [120];
        options.ShowColumnRulers = true;
        options.HighlightCurrentLine = true;

        TextArea textArea = Editor.TextArea;
        textArea.LeftMargins.Insert(1, markerMargin); // markers between line numbers and folding
        textArea.IndentationStrategy = new CSharpIndentationStrategy(options);
        textArea.SelectionBrush = new SolidColorBrush(Colors.SkyBlue.Transluscent(0.2));
        textArea.Caret.PositionChanged += EditorCaretPositionChanged;
        textArea.SelectionChanged += EditorSelectionChanged;
        textArea.TextEntering += EditorTextEntering;
        textArea.TextEntered += EditorTextEntered;

        textMarkerService = new TextMarkerService(Editor);
        TextView textView = textArea.TextView;
        textView.BackgroundRenderers.Add(textMarkerService);
        textView.PointerMoved += EditorTextViewPointerMoved;
        textView.PointerExited += EditorTextViewOnPointerExited;

        ApplyOptions(App.Options.Editor);
    }

    private void InitializeTimers()
    {
        // Clipboard monitoring timer
        idleTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        idleTimer.Tick += EditorIdle;
        idleTimer.Start();

        // Dwell timer
        dwellTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        dwellTimer.Tick += EditorDwell;
    }

    private void InitializeFolding()
    {
        if (foldingManager != null)
            FoldingManager.Uninstall(foldingManager);

        foldingManager = FoldingManager.Install(Editor.TextArea);
        foldingStrategy.UpdateFoldings(foldingManager, Editor.Document);
        updateFolding = false;
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

            if (string.IsNullOrWhiteSpace(filePath))
            {
                FileNameStatusItem.Text = SR.Untitled;
                Title = $"{SR.Untitled} - {AssemblyInfo.Title}";
            }
            else
            {
                FileNameStatusItem.Text = filePath;
                Title = $"{Path.GetFileName(filePath)} - {AssemblyInfo.Title}";
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

    /// <summary>
    /// Gets or sets the configuration of this window.
    /// </summary>
    private WindowSettings Settings
    {
        get => new(WindowState, Position.X, Position.Y, Width, Height, Editor.CaretOffset);
        set
        {
            if (value.State != WindowState.Minimized)
                WindowState = value.State;

            if (WindowState == WindowState.Normal)
            {
                Position = new(value.X, value.Y);
                Width = value.Width;
                Height = value.Height;
            }

            if (value.CaretOffset < Editor.Document.TextLength)
            {
                Editor.CaretOffset = value.CaretOffset;
                Editor.TextArea.Caret.BringCaretToView();
            }
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Creates a new instance of the TextDocument class using the contents of the specified file path.
    /// </summary>
    /// <remarks>
    /// The returned document is initialized with an event handler for change notifications. The
    /// caller is responsible for managing the document's lifecycle.
    /// </remarks>
    /// <param name="path">
    /// The path to the file whose contents will be loaded into the document. If the path is null, empty, or the file
    /// does not exist, an empty document is created.
    /// </param>
    /// <returns>
    /// A TextDocument containing the contents of the specified file, or an empty document if the file is not found or
    /// the path is invalid.
    /// </returns>
    private TextDocument CreateDocument(string path)
    {
        var content = !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                    ? File.ReadAllText(path)
                    : string.Empty;

        var document = new TextDocument(content);
        document.Changed += EditorDocumentChanged;
        return document;
    }

    /// <summary>
    /// Opens the specified file and loads its contents into the editor or resets the editor to its initial state.
    /// </summary>
    /// <remarks>
    /// After calling this method, the editor's document is replaced with the contents of the
    /// specified file if any, or simply cleared if the file was unspecified not found.
    /// The editor state is updated accordingly. Any unsaved changes in the previous document will
    /// be lost. This method resets the saved state and updates window UI elements to reflect the newly opened
    /// file.
    /// </remarks>
    /// <param name="path">The path to the file to open, or <b>null</b> for a reset.</param>
    public void Open(string path)
    {
        Editor.Document = CreateDocument(path);
        FilePath = path;
        Saved = true;

        InitializeFolding();
        UpdateWindowBars();
    }

    /// <summary>
    /// Saves the current document to the specified file path.
    /// </summary>
    /// <remarks>
    /// If the file at the specified path already exists, it will be overwritten. After a successful
    /// save, the document's state is updated to reflect the new file path and saved status.
    /// </remarks>
    /// <param name="path">The file system path where the document will be saved. Cannot be null or empty.</param>
    public void Save(string path)
    {
        File.WriteAllText(path, Editor.Document.Text);
        FilePath = path;
        Saved = true;
    }

    /// <summary>
    /// Applies the specified editor options to the current editor instance.
    /// </summary>
    /// <param name="options">The set of editor options to apply. If null, no changes are made.</param>
    private void ApplyOptions(EditorOptions options)
    {
        if (options == null) return;

        Editor.FontFamily = options.FontFamily;
        Editor.FontSize = options.FontSize;
        Editor.WordWrap = options.WordWrap;
        Editor.ShowLineNumbers = options.ShowLineNumbers;
        Editor.Options.ShowEndOfLine = Editor.Options.ShowTabs
                                     = Editor.Options.ShowSpaces
                                     = options.ShowWhitespace;
        Editor.Options.HighlightCurrentLine = options.HighlightCurrentLine;
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
    /// Prompts the user to save changes and processes the user's response asynchronously.
    /// </summary>
    /// <remarks>
    /// This method displays a dialog box asking the user whether to save changes. If the user
    /// selects Yes, the changes are saved before continuing. If the user selects Cancel, the operation is
    /// aborted.
    /// </remarks>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <see langword="true"/> if the user chooses
    /// to continue (either by saving or not saving); <see langword="false"/> if the user cancels the operation.
    /// </returns>
    private async Task<bool> PromptToSave()
    {
        var answer = await MessageBoxManager
            .GetMessageBoxStandard(Title!, SR.PromptToSave, ButtonEnum.YesNoCancel, MBI.Question)
            .ShowAsync();

        switch (answer)
        {
            case ButtonResult.Yes:
                ToolbarSaveButtonClick(null, null);
                break;
            case ButtonResult.Cancel:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Updates the state of the window's UI bars to reflect the current document state.
    /// </summary>
    /// <remarks>
    /// Call this method after making changes to the document to ensure that undo/redo controls and
    /// caret-related UI elements are updated accordingly.
    /// </remarks>
    private void UpdateWindowBars()
    {
        Editor.Document.UndoStack.ClearAll();
        UpdateUndoRedoFileSize();
        UpdateCutCopyCaretInfo();
    }

    /// <summary>
    /// Updates the enabled state of undo, redo, and run toolbar buttons and menu items, and refreshes the text length
    /// status label based on the current state of the editor.
    /// </summary>
    /// <remarks>
    /// This method should be called after any operation that may affect the editor's undo or redo
    /// availability, or the document's text length. It also updates the saved state if no further undo operations are
    /// possible.
    /// </remarks>
    private void UpdateUndoRedoFileSize()
    {
        ToolbarUndoButton.IsEnabled = UndoMenuItem.IsEnabled = Editor.CanUndo;
        ToolbarRedoButton.IsEnabled = RedoMenuItem.IsEnabled = Editor.CanRedo;
        ToolbarRunButton.IsEnabled = Editor.Document.TextLength > 0;

        TextLengthStatusItem.Text = string.Format(SR.TextLength, Editor.Document.TextLength);

        if (!Editor.CanUndo) Saved = true;
    }

    /// <summary>
    /// Updates the enabled state of cut, copy, and related UI elements, and refreshes the caret status display based on
    /// the current editor selection and caret position.
    /// </summary>
    /// <remarks>
    /// Call this method after changes to the editor selection or caret position to ensure that the
    /// cut, copy, and surround menu items, as well as the caret status label, accurately reflect the current editor
    /// state.
    /// </remarks>
    private void UpdateCutCopyCaretInfo()
    {
        ToolbarCutButton.IsEnabled = CutMenuItem.IsEnabled = Editor.CanCut;
        ToolbarCopyButton.IsEnabled = CopyMenuItem.IsEnabled = Editor.CanCopy;
        SurroundWithMenuItem.IsEnabled = !Editor.TextArea.Selection.IsEmpty;

        CaretStatusItem.Text = string.Format(
            SR.CaretStatus,
            Editor.TextArea.Caret.Line,
            Editor.TextArea.Caret.Column,
            Editor.TextArea.Selection.Length);
    }

    /// <summary>
    /// Determines whether the specified character position is within a comment or string literal in the editor's
    /// document.
    /// </summary>
    /// <remarks>
    /// This method relies on the current syntax highlighting state of the editor. If syntax
    /// highlighting is unavailable, the method returns false.
    /// </remarks>
    /// <param name="position">The zero-based character offset within the document to check.</param>
    /// <returns>true if the specified position is inside a comment or string literal; otherwise, false.</returns>
    private bool InCommentOrString(int position)
    {
        // Retrieves the highlighter
        if (Editor.TextArea.GetService(typeof(IHighlighter)) is not IHighlighter highlighter)
            return false;

        // Retrieves the highlighted line
        DocumentLine line = Editor.Document.GetLineByOffset(position);
        // Apply highlighting to the line
        HighlightedLine highlightedLine = highlighter.HighlightLine(line.LineNumber);

        // Check if line's coloration at the given position is a comment or a string
        return highlightedLine.Sections.Any(section =>
            section.Offset <= position &&
            position < section.Offset + section.Length &&
            section.Color?.Name is "Comment" or "String" or "DateTime");
    }

    /// <summary>
    /// Retrieves the word immediately preceding the caret position in the editor.
    /// </summary>
    /// <returns>
    /// A string containing the word directly before the caret. Returns an empty string if the caret is at the beginning
    /// of the document or not positioned after a word.
    /// </returns>
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
    /// Retrieves the second word to the left of the caret position in the editor.
    /// </summary>
    /// <returns>
    /// A string containing the second word to the left of the caret position.
    /// Returns an empty string if there is no word to the left or if that word is the first.
    /// </returns>
    private string GetWordAtLeft()
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

        return wordStart < 0 || wordEnd < 0 || wordEnd <= wordStart
             ? string.Empty
             : document.GetText(wordStart, wordEnd - wordStart);
    }

    /// <summary>
    /// Retrieves the word in the document that contains the specified character offset.
    /// </summary>
    /// <param name="offset">
    /// The zero-based character offset within the document for which to find the containing word.
    /// Must be greater than 0 and less than the document's text length.
    /// </param>
    /// <returns>
    /// A string containing the word at the specified offset, or an empty string if the offset
    /// is at the start or end of the document, or if no word is found at the offset.
    /// </returns>
    private string GetWordAtOffset(int offset)
    {
        TextDocument document = Editor.Document;

        // Edge case: caret at the beginning of the document
        if (offset <= 0 || offset >= document.TextLength)
            return string.Empty;

        // Find the start of the word
        int wordStart = TextUtilities.GetNextCaretPosition(
            document,
            offset,
            LogicalDirection.Backward,
            CaretPositioningMode.WordStart);

        // Find the end of the previous word
        int wordEnd = TextUtilities.GetNextCaretPosition(
            document,
            wordStart,
            LogicalDirection.Forward,
            CaretPositioningMode.WordBorder);

        return wordStart < 0 || wordEnd < 0 || wordEnd <= wordStart
             ? string.Empty
             : document.GetText(wordStart, wordEnd - wordStart);
    }

    /// <summary>
    /// Displays a completion window populated with the specified completion data in the editor's text area.
    /// </summary>
    /// <remarks>
    /// The completion window allows users to select from the provided completion items. If a
    /// completion window is already open, it will be replaced by the new one.
    /// </remarks>
    /// <typeparam name="T">The type of completion data to display. Must implement the ICompletionData interface.</typeparam>
    /// <param name="completionData">The collection of completion data items to display in the completion window. Cannot be null.</param>
    private void ShowCompletionWindow<T>(IEnumerable<T> completionData)
        where T : ICompletionData
    {
        completionWindow = new CompletionWindow(Editor.TextArea);

        foreach (var dataItem in completionData)
        {
            completionWindow.CompletionList.CompletionData.Add(dataItem);
        }

        completionWindow.Closed += (_, _) =>
        {
            completionWindow = null;

            if (completionStage == CompletionStage.InProgress)
                completionStage = CompletionStage.Complete;
        };

        completionWindow.Show();
    }

    /// <summary>
    /// Completes the insertion of a keyword by displaying a calltip window
    /// if the inserted word is a builtin function name.
    /// </summary>
    private void CompleteKeywordInsertion()
    {
        completionStage = CompletionStage.None;

        string word = GetCurrentWord();
        if (word != "(") return;

        word = GetWordAtLeft();
        if (!CallTipProvider.IsDefined(word)) return;

        var callTip = CallTipProvider.GetCallTip(word);
        PushCallTip(callTip);
        ShowCallTip();
    }

    /// <summary>
    /// Determines whether the completion window is currently open.
    /// </summary>
    /// <returns><see langword="true"/> if the completion window is open; otherwise, <see langword="false"/>.</returns>
    private bool IsCompletionWindowOpen() => completionWindow?.IsOpen == true;

    /// <summary>
    /// Adds the specified call tip to the call tip stack and resets its state for reuse.
    /// </summary>
    /// <param name="callTip">The call tip information to be pushed onto the stack. Cannot be null.</param>
    private void PushCallTip(CallTipInfo callTip)
    {
        callTipStack.Push(callTip);
        callTip.Reset();
    }

    /// <summary>
    /// Removes the most recent call tip from the stack.
    /// </summary>
    /// <returns>true if there are remaining call tips on the stack after the removal; otherwise, false.</returns>
    private bool PopCallTip()
    {
        callTipStack.Pop();
        return callTipStack.Count > 0;
    }

    /// <summary>
    /// Displays a call tip window showing overload information for the current method or function at the caret
    /// position.
    /// </summary>
    /// <remarks>
    /// The call tip window provides parameter and overload details to assist with code completion
    /// and editing. If a call tip window is already open, it will be replaced with the new information.
    /// </remarks>
    private void ShowCallTip()
    {
        insightWindow = new OverloadInsightWindow(Editor.TextArea)
        {
            Provider = new SimpleOverloadProvider(CurrentCallTip)
        };

        insightWindow.Closed += (s, e) => insightWindow = null;
        insightWindow.Show();
    }

    /// <summary>
    /// Displays an informational popup with the specified header and text.
    /// </summary>
    /// <param name="header">The text to display as the header of the popup. Cannot be null.</param>
    /// <param name="text">The informational message to display in the popup. Cannot be null.</param>
    private void ShowInfoPopup(string header, string text)
    {
        dwellTimer.Stop();

        PopupHeader.Text = header;
        PopupText.Text = text;

        InfoPopup.PlacementTarget = Editor;
        InfoPopup.IsOpen = true;
    }

    /// <summary>
    /// Opens the search panel in the editor, optionally enabling replace mode based on the specified parameter.
    /// </summary>
    /// <param name="replaceMode">true to open the search panel in replace mode; otherwise, false to open it in search-only mode.</param>
    private void OpenSearchPanel(bool replaceMode)
    {
        if (searchPanel == null)
        {
            Editor.SearchPanel.Uninstall();
            searchPanel = SearchPanel.Install(Editor);
        }

        var selection = Editor.TextArea.Selection;
        searchPanel.SearchPattern = selection.IsEmpty || selection.IsMultiline ? string.Empty : selection.GetText();
        searchPanel.IsReplaceMode = replaceMode;
        searchPanel.Open();
    }

    /// <summary>
    /// Reports an error by marking the specified range in the script and displaying an error message to the user.
    /// </summary>
    /// <param name="errorMessage">The error message to display for the marked script range. Cannot be null.</param>
    /// <param name="start">The starting location in the script where the error is reported. Specifies the beginning of the error range.</param>
    /// <param name="end">The ending location in the script where the error is reported. Specifies the end of the error range.</param>
    private void ReportError(string errorMessage, ScriptLocation start, ScriptLocation end)
    {
        markerMargin.AddMarker(start.LineNumber + 1, errorMessage);
        textMarkerService.AddMarker(new(start.Offset, end.Offset) { ToolTip = errorMessage });
    }

    /// <summary>
    /// Clears all error markers from the editor view.
    /// </summary>
    /// <remarks>
    /// Call this method to remove any visual error indicators currently displayed in the editor.
    /// This is typically used to reset the error state after errors have been resolved or dismissed.
    /// </remarks>
    private void ClearErrors()
    {
        markerMargin.ClearMarkers();
        textMarkerService.ClearMarkers();
        Editor.TextArea.TextView.Repaint();
    }

    #endregion

    #region Event handlers

    #region Window events

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var settings = ConfigurationManager.LoadWindowSettings(filePath);
        if (settings != null) Settings = settings;
    }

    private void WindowActivated(object sender, EventArgs e)
    {
        Editor.TextArea.Focus();
    }

    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        var control = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

        if (e.IsHotKey(Key.S, control | KeyModifiers.Shift))
            ToolbarSaveAsMenuItemClick(null, null);
        else if (e.IsHotKey(Key.P, control | KeyModifiers.Shift))
            ToolbarPrintToPdfMenuItemClick(null, null);
        else if (e.IsHotKey(Key.I, control | KeyModifiers.Shift))
            SurroundWithMenuItemClick(null, null);
        else if (e.IsHotKey(Key.I, control)) // order matters for Ctrl+Shift+I and Ctrl+I
            InsertSnippetMenuItemClick(null, null);
        else if (e.IsHotKey(Key.R, control))
            ReformatMenuItemClick(null, null);
        else
            return;

        e.Handled = true;
    }

    private async void WindowClosing(object sender, WindowClosingEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
            ConfigurationManager.SaveWindowSettings(filePath, Settings);

        if (Saved) return;

        e.Cancel = true;
        if (!await PromptToSave()) return;

        Closing -= WindowClosing;
        Close();
    }

    #endregion

    #region Toolbar events

    private void ToolbarNewButtonClick(object sender, RoutedEventArgs e)
    {
        App.OpenWindow();
        CloseIfEmpty();
    }

    private async void ToolbarOpenButtonClick(object sender, RoutedEventArgs e)
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


        if (files.Count == 0) return;

        foreach (var file in files)
            App.OpenWindow(file.Path.LocalPath);

        CloseIfEmpty();
    }

    private void ToolbarSaveButtonClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            ToolbarSaveAsMenuItemClick(null, null);
        else
            Save(filePath);
    }

    private async void ToolbarSaveAsMenuItemClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
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

    private async void ToolbarExportXmlMenuItemClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = SR.ExportBoxTitle,
                DefaultExtension = ".xml",
                FileTypeChoices =
                [
                    new FilePickerFileType(SR.XmlFileFilter) { Patterns = ["*.xml"] },
                    FilePickerFileTypes.All
                ],
                SuggestedFileName = !string.IsNullOrWhiteSpace(FilePath)
                    ? Path.GetFileNameWithoutExtension(FilePath) + ".xml"
                    : $"{SR.Untitled}.xml"
            });

        if (file is null) return;

        try
        {
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

    private async void ToolbarPrintButtonClick(object sender, RoutedEventArgs e)
    {
        var pdfPath = Path.Combine(WorkDir, Path.ChangeExtension(Path.GetRandomFileName(), ".pdf"));
        Directory.CreateDirectory(WorkDir);

        try
        {
            var printing = new PdfPrinting(Editor);
            await printing.PrintToPrinter(pdfPath, FileNameStatusItem.Text);
        }
        catch (Exception ex)
        {
            await MessageBoxManager
                  .GetMessageBoxStandard(SR.ErrorMessageTitle, ex.Message, ButtonEnum.Ok, MBI.Error)
                  .ShowAsync();
        }
        finally
        {
            if (File.Exists(pdfPath)) File.Delete(pdfPath);
        }
    }

    private async void ToolbarPrintToPdfMenuItemClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = SR.PrintPdfTitle,
                DefaultExtension = ".pdf",
                FileTypeChoices =
                [
                    new FilePickerFileType(SR.PortableDocumentFormat) { Patterns = ["*.pdf"] },
                    FilePickerFileTypes.All
                ],
                SuggestedFileName = !string.IsNullOrWhiteSpace(FilePath)
                    ? Path.GetFileNameWithoutExtension(FilePath) + ".pdf"
                    : $"{SR.Untitled}.pdf"
            });

        if (file is null) return;

        try
        {
            var printing = new PdfPrinting(Editor);
            await printing.PrintToPdf(file.Path.LocalPath, FileNameStatusItem.Text);

            Process.Start(new ProcessStartInfo(file.Path.LocalPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            await MessageBoxManager
                  .GetMessageBoxStandard(SR.ErrorMessageTitle, ex.Message, ButtonEnum.Ok, MBI.Error)
                  .ShowAsync();
        }
    }

    private void ToolbarUndoButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Undo();
    }

    private void ToolbarRedoButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Redo();
    }

    private void ToolbarCutButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Cut();
    }

    private void ToolbarCopyButtonClick(object sender, RoutedEventArgs e)
    {
        Editor.Copy();
    }

    private void ToolbarPasteButtonClick(object sender, RoutedEventArgs e)
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

    private void ToolbarIndentButtonClick(object sender, RoutedEventArgs e)
    {
        var document = Editor.Document;
        var selection = Editor.TextArea.Selection;

        if (selection.IsMultiline)
        {
            document.BeginUpdate();

            for (var i = selection.StartPosition.Line; i <= selection.EndPosition.Line; ++i)
                document.IndentLine(i);

            document.EndUpdate();
        }
        else
            document.IndentLine(Editor.TextArea.Caret.Line);
    }

    private void ToolbarOutdentButtonClick(object sender, RoutedEventArgs e)
    {
        var document = Editor.Document;
        var selection = Editor.TextArea.Selection;

        if (selection.IsMultiline)
        {
            document.BeginUpdate();

            for (var i = selection.StartPosition.Line; i <= selection.EndPosition.Line; ++i)
                document.OutdentLine(i);

            document.EndUpdate();
        }
        else
            document.OutdentLine(Editor.TextArea.Caret.Line);
    }

    private void ToolbarCommentButtonClick(object sender, RoutedEventArgs e)
    {
        var document = Editor.Document;
        var selection = Editor.TextArea.Selection;

        if (selection.IsMultiline)
        {
            document.BeginUpdate();

            for (var i = selection.StartPosition.Line; i <= selection.EndPosition.Line; ++i)
                document.CommentLine(i);

            document.EndUpdate();
        }
        else
            document.CommentLine(Editor.TextArea.Caret.Line);
    }

    private void ToolbarUncommentButtonClick(object sender, RoutedEventArgs e)
    {
        var document = Editor.Document;
        var selection = Editor.TextArea.Selection;

        if (selection.IsMultiline)
        {
            document.BeginUpdate();

            for (var i = selection.StartPosition.Line; i <= selection.EndPosition.Line; ++i)
                document.UncommentLine(i);

            document.EndUpdate();
        }
        else
            document.UncommentLine(Editor.TextArea.Caret.Line);
    }

    private async void ToolbarRunButtonClick(object sender, RoutedEventArgs e)
    {
        /****************************************************************************
         * Parsing and running the script is delegated to asis.
         * *************************************************************************/

        var scriptPath = Path.Combine(WorkDir, Path.ChangeExtension(Path.GetRandomFileName(), ".add"));
        Directory.CreateDirectory(WorkDir);
        await File.WriteAllTextAsync(scriptPath, Editor.Text);

        var logPath = Path.Combine(WorkDir, Path.ChangeExtension(Path.GetRandomFileName(), ".log"));
        List<string> argsList = ["-f", scriptPath, "-l", logPath];

        var culture = App.Options?.Culture;
        if (culture != null)
        {
            argsList.Add("-c");
            argsList.Add(culture.Name);
        }

        foreach (var directory in App.Options.SearchPaths)
        {
            argsList.Add("-d");
            argsList.Add(directory);
        }

        foreach (var assemblyName in App.Options.References)
        {
            argsList.Add("-r");
            argsList.Add(assemblyName);
        }

        ClearErrors();

        try
        {
            int exitCode = await TerminalLauncher.LaunchTerminal(
                this,
                $"{AssemblyInfo.Title} Terminal [{FileNameStatusItem.Text}]",
                "./asis",
                [.. argsList]);

            if (exitCode == 0 || !File.Exists(logPath)) return;

            using var logReader = File.OpenText(logPath);
            if (await logReader.ReadLineAsync() != scriptPath) return;

            var parts = (await logReader.ReadLineAsync())!.Split(',');
            var start = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

            parts = (await logReader.ReadLineAsync())!.Split(',');
            var end = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

            var errorMessage = await logReader.ReadLineAsync();
            ReportError(errorMessage, start, end);
        }
        catch (Exception ex)
        {
            await MessageBoxManager
                .GetMessageBoxStandard(SR.ErrorMessageTitle, ex.Message, ButtonEnum.Ok, MBI.Error)
                .ShowAsync();
        }
        finally
        {
            File.Delete(scriptPath);
            if (File.Exists(logPath)) File.Delete(logPath);
            Activate();
        }
    }

    private async void ToolbarConfigButtonClick(object sender, RoutedEventArgs e)
    {
        var culture = App.Options.Culture;
        var optionDialog = new OptionDialog { Options = App.Options.Clone() };
        if (!await optionDialog.ShowDialog<bool>(this)) return;

        App.Options = optionDialog.Options;
        ConfigurationManager.SaveOptions(App.Options);

        foreach (var window in App.Windows)
        {
            window.ApplyOptions(App.Options.Editor);
        }

        if (Equals(culture, App.Options.Culture)) return;

        await MessageBoxManager
              .GetMessageBoxStandard(SR.InfoMessageTitle, SR.LanguageChangeOnRestart, ButtonEnum.Ok, MBI.Info)
              .ShowAsync();
    }

    private void ToolbarHelpButtonClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(WIKI_LINK) { UseShellExecute = true });
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
        updateFolding = true;
        Saved = Editor.Document.TextLength == 0 && string.IsNullOrEmpty(filePath);
        UpdateUndoRedoFileSize();
    }

    private void EditorTextEntering(object sender, TextInputEventArgs e)
    {
        /**************************************************************************************
         * Tries to wrap the selection (if any) in a pair of containers like quotes or brackets
         * ***********************************************************************************/

        var selection = Editor.TextArea.Selection;
        if (selection.IsEmpty) return;

        char leftWrapper = e.Text![0];
        char rightWrapper = leftWrapper switch
                            {
                                '\'' or '"' or '|' => leftWrapper,
                                '(' => ')',
                                '[' => ']',
                                '{' => '}',
                                _ => '\0',
                            };

        if (rightWrapper == '\0') return;

        selection.ReplaceSelectionWithText($"{leftWrapper}{selection.GetText()}{rightWrapper}");
        e.Handled = true;
    }

    private void EditorTextEntered(object sender, TextInputEventArgs e)
    {
        /**********************************************************************************
         * Tries to display a completion window populated with keywords or a calltip window
         * ********************************************************************************/

        int caretOffset = Editor.CaretOffset;
        if (caretOffset > 0 && InCommentOrString(caretOffset - 1)) return;

        char firstChar = e.Text![0];
        if (char.IsLetterOrDigit(firstChar))
        {
            if (IsCompletionWindowOpen()) return;

            var matchedKeywords = KeywordCompletionData.AllMatching(GetCurrentWord());
            if (matchedKeywords.Count == 0) return;

            completionStage = CompletionStage.InProgress;
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
                    PushCallTip(CallTipProvider.GetCallTip(wordAtLeft));
                    ShowCallTip();
                    return;
                }
                case ',':
                    if (callTipStack.Count == 0) return;
                    insightWindow?.Close();
                    if (!CurrentCallTip.NextParameter()) return;
                    ShowCallTip();
                    return;
                case ')':
                    if (callTipStack.Count == 0) return;
                    insightWindow?.Close();
                    if (!PopCallTip()) return;
                    ShowCallTip();
                    return;
            }
        }
    }

    private void EditorTextViewPointerMoved(object sender, PointerEventArgs e)
    {
        // convert mouse → document offset
        var vp = Editor.GetPositionFromPoint(e.GetPosition(Editor));

        // show/hide tooltip if a text marker is present at hover position
        if (vp == null)
            ToolTip.SetIsOpen(Editor, false);
        else
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

        if (vp == hoverPosition) return;

        hoverPosition = vp;
        InfoPopup.IsOpen = false;
        dwellTimer.Stop();
        dwellTimer.Start();
    }

    private void EditorTextViewOnPointerExited(object sender, PointerEventArgs e)
    {
        // hide tooltip
        ToolTip.SetIsOpen(Editor, false);

        // reset hover position
        hoverPosition = null;
    }

    private void EditorIdle(object sender, EventArgs e)
    {
        if (updateFolding)
            foldingStrategy.UpdateFoldings(foldingManager, Editor.Document);

        if (completionStage == CompletionStage.Complete)
            CompleteKeywordInsertion();

        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var text = clipboard.TryGetTextAsync().GetAwaiter().GetResult();
        // We could have used Editor.CanPaste but it always returns true!
        ToolbarPasteButton.IsEnabled = PasteMenuItem.IsEnabled = !string.IsNullOrEmpty(text);
    }

    private void EditorDwell(object sender, EventArgs e)
    {
        dwellTimer.Stop();

        if (hoverPosition == null) return;

        int hoverOffset = Editor.Document.GetOffset(hoverPosition.Value.Location);
        if (InCommentOrString(hoverOffset)) return;

        string hoverWord = GetWordAtOffset(hoverOffset);
        if (!CallTipProvider.IsDefined(hoverWord)) return;

        CallTipInfo callTip = CallTipProvider.GetCallTip(hoverWord);
        ShowInfoPopup(SR.BuiltinFunction, callTip.ToString());
    }

    #endregion

    #region Editor menu events

    private void InsertSnippetMenuItemClick(object sender, RoutedEventArgs e)
    {
        ShowCompletionWindow(CodeSnippetCompletionData.All);
    }

    private void SurroundWithMenuItemClick(object sender, RoutedEventArgs e)
    {
        ShowCompletionWindow(SurroundCodeCompletionData.All);
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
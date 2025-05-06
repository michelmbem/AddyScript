using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.Search;
using AvaloniaEdit.TextMate;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using TextMateSharp.Grammars;
using StringRes = AddyScript.Gui.Properties.Resources;
using MBIcon = MsBox.Avalonia.Enums.Icon;

namespace AddyScript.Gui;

public partial class MainWindow : Window
{
    #region Fields
    
    private const string TitleBase = "AddyScript";
    private const string HelpLink = "https://github.com/michelmbem/AddyScript/blob/master/docs/README.md";
    
    private readonly BraceFoldingStrategy foldingStrategy = new();
    private FoldingManager foldingManager;
    private SearchPanel searchPanel;

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
        var registryOptions = new RegistryOptions(ThemeName.LightPlus); 
        var textMateInstallation = Editor.InstallTextMate(registryOptions); 
        textMateInstallation.SetGrammar(registryOptions.GetScopeByExtension(".cs"));
        
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
        
        searchPanel = SearchPanel.Install(Editor);
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
                Title = TitleBase;
            }
            else
            {
                FileNameStatusLabel.Content = value;
                Title = $"{Path.GetFileName(value)} - {TitleBase}";
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
    private static string EscapeCmdLineArg(string arg)
    {
        return $"\"{arg.Replace("\"", "\"\"")}\"";
    }

    /// <summary>
    /// Checks if a <see cref="KeyEventArgs"/> instance matches the given configuration.
    /// </summary>
    /// <param name="e">The <see cref="KeyEventArgs"/> to check</param>
    /// <param name="key">The expected <see cref="Key"/> member</param>
    /// <param name="modifiers">Tells whether one or any of the Control/Alt/System/Shift keys should be pressed or not</param>
    /// <returns><b>true</b> is <paramref name="e"/> matches the configuration. <b>false</b> otherwise</returns>
    private static bool IsHotKey(KeyEventArgs e, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        return e.Key == key && (e.KeyModifiers & modifiers) == modifiers;
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
            '(' or ')' or '[' or ']' or '{' or '}' => true,
            _ => false,
        };
    }

    /// <summary>
    /// Resets the environment.
    /// </summary>
    public void Reset()
    {
        Editor.Document = new TextDocument();
        Editor.Document.Changed += EditorDocumentChanged;
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
        Editor.Document = new TextDocument(File.ReadAllText(path));
        Editor.Document.Changed += EditorDocumentChanged;
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

    private async Task OpenAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = StringRes.OpenFileDialogTitle,
            AllowMultiple = true,
            FileTypeFilter = [
                new FilePickerFileType(StringRes.FileDialogFilter) { Patterns = ["*.add", "*.txt"] },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count > 0)
        {
            App.Load(files[0].Path.LocalPath);
            if (Saved && Editor.Document.TextLength <= 0)
                Close();
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
            Title = StringRes.SaveFileDialogTitle,
            DefaultExtension = ".add",
            FileTypeChoices = [
                new FilePickerFileType(StringRes.FileDialogFilter) { Patterns = ["*.add", "*.txt"] },
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
                Title = StringRes.ExportXmlTitle,
                DefaultExtension = ".xml",
                FileTypeChoices = [
                    new FilePickerFileType(StringRes.XmlFileFilter) { Patterns = ["*.xml"] },
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
                .GetMessageBoxStandard(StringRes.ErrorMessageTitle, ex.Message, ButtonEnum.Ok, MBIcon.Error)
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
            .GetMessageBoxStandard(Title!, StringRes.PromptToSave, ButtonEnum.YesNoCancel, MBIcon.Question)
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
        ToolbarUndoButton.IsEnabled = Editor.CanUndo;
        ToolbarRedoButton.IsEnabled = Editor.CanRedo;
        ToolbarRunButton.IsEnabled = Editor.Document.TextLength > 0;

        TextLengthStatusLabel.Content = string.Format(StringRes.TextLength, Editor.Document.TextLength);
        
        if (!Editor.CanUndo) Saved = true;
    }

    /// <summary>
    /// Updates the 'Cut', 'Copy' toolbar buttons as well as
    /// the part of the status bar where the caret info are shown.
    /// </summary>
    private void UpdateCutCopyCaretInfo()
    {
        ToolbarCutButton.IsEnabled = Editor.CanCut;
        ToolbarCopyButton.IsEnabled = Editor.CanCopy;
        
        CaretStatusLabel.Content = string.Format(StringRes.CaretStatus,
            Editor.TextArea.Caret.Line, Editor.TextArea.Caret.Column, Editor.TextArea.Selection.Length);
    }

    private void OpenSearchPanel(bool replaceMode)
    {
        searchPanel.IsReplaceMode = replaceMode;
        searchPanel.Open();

        var selection = Editor.TextArea.Selection;
        if (!(selection.IsEmpty || selection.IsMultiline))
            searchPanel.SearchPattern = selection.GetText();
        
        Dispatcher.UIThread.Post(searchPanel.Reactivate, DispatcherPriority.Input);
    }

    private void ReportError(string errorMessage, ScriptLocation start, ScriptLocation end)
    {
        Console.WriteLine($@"{errorMessage} @{start}:{end}");
    }
    
    #endregion

    #region Event handlers
    
    #region Window events

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
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
    
    #endregion

    #region Toolbar events

    public void ToolbarNewButtonClick(object sender, RoutedEventArgs e)
    {
        App.Load();
        if (Saved && Editor.Document.TextLength <= 0)
            Close();
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
            .GetMessageBoxStandard(Title!, StringRes.MissingFunctionality, ButtonEnum.Ok, MBIcon.Warning)
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

        foreach (var directory in App.Directories)
            argsBuilder.Append(" -d ").Append(EscapeCmdLineArg(directory));

        foreach (var assemblyName in App.Assemblies)
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
            ReportError(errorMessage, start, end);;
        }
        catch (Exception ex)
        {
            MessageBoxManager
                .GetMessageBoxStandard(StringRes.ErrorMessageTitle,ex.Message, ButtonEnum.Ok, MBIcon.Error)
                .ShowAsync();
        }
        finally
        {
            if (scriptPath != filePath) File.Delete(scriptPath);
            if (File.Exists(logPath)) File.Delete(logPath);
            Activate();
        }
    }

    public void ToolbarConfigButtonClick(object sender, RoutedEventArgs e)
    {
    }

    public void ToolbarHelpButtonClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(HelpLink) { UseShellExecute = true });
    }

    private void ToolbarHelpAboutMenuItemClick(object sender, RoutedEventArgs e)
    {
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
        Console.WriteLine($@"Text entered: {e.Text}");
    }
    
    #endregion
    
    #endregion
}
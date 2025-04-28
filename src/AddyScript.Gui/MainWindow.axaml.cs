using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit.Document;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using TextMateSharp.Grammars;

namespace AddyScript.Gui;

public partial class MainWindow : Window
{
    private const string TITLE_BASE = "AddyScript";
    private const string HELP_LINK = "https://github.com/michelmbem/AddyScript/blob/master/docs/README.md";

    private string filePath;
    private bool saved;
    
    private int caretPosition;
    private int scriptLength;
    private string errorMessage;
    private string dndFilePath;

    #region Initialization
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeStyling();
        Reset();
    }

    private void InitializeStyling()
    {
        var registryOptions = new RegistryOptions(ThemeName.SolarizedLight); 
        var textMateInstallation = Editor.InstallTextMate(registryOptions); 
        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".cs").Id));

        Editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(Editor.Options);
        Editor.TextArea.Caret.PositionChanged += EditorCaretPositionChanged;
        Editor.TextArea.SelectionChanged += EditorSelectionChanged;
        Editor.TextArea.DocumentChanged += EditorDocumentChanged;
        Editor.TextArea.TextEntering += EditorTextEntering;
        Editor.TextArea.TextEntered += EditorTextEntered;
        
        Editor.Options.AllowToggleOverstrikeMode = true;
        Editor.Options.EnableTextDragDrop = true;
        Editor.Options.ShowBoxForControlCharacters = true;
        Editor.Options.ColumnRulerPositions = [80, 120];
        Editor.Options.HighlightCurrentLine = true;
        //Editor.Options.CompletionAcceptAction = CompletionAcceptAction.DoubleTapped;
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
    /// <param name="key">The expected <see cref="Key"/> member</param>
    /// <param name="modifiers">Tells whether one or any of the Control/Alt/System/Shift keys should be pressed or not</param>
    /// <returns><b>true</b> is <paramref name="e"/> matches the configuration. <b>false</b> otherwise</returns>
    private static bool IsHotKey(KeyEventArgs e, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        return e.Key == key && (modifiers == KeyModifiers.None || (e.KeyModifiers & modifiers) != KeyModifiers.None);
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
        Editor.Document = new TextDocument(File.ReadAllText(path));
        FilePath = path;
        Saved = true;
        scriptLength = Editor.Document.TextLength;
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
    /// Prompts the user to save the script before leaving.
    /// </summary>
    /// <returns>true to continue; false to cancel the current action</returns>
    public async Task<bool> PromptToSave()
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Title!,
            "Should the current script be saved?", ButtonEnum.YesNoCancel, MsBox.Avalonia.Enums.Icon.Question);
        var answer = await box.ShowAsync();

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
        ToolbarRunButton.IsEnabled = scriptLength > 0;
        
        TextLengthStatusLabel.Content = $"{scriptLength} characters";
        
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
        
        CaretStatusLabel.Content = $"Ln: {Editor.TextArea.Caret.Line}  Col: {Editor.TextArea.Caret.Column}  Sel: {Editor.TextArea.Selection.Length}";
    }
    
    #endregion

    #region Event handlers

    #region Toolbar

    public void ToolbarNewButtonClick(object sender, RoutedEventArgs e)
    {
        Reset();
    }

    public async void ToolbarOpenButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load a script from file",
            Filters = [
                new FileDialogFilter { Name = "AddyScript files", Extensions = { "add" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };
        
        var fileNames = await dialog.ShowAsync(this);

        if (fileNames?.Length > 0)
        {
            Open(fileNames[0]);
        }
    }

    public void ToolbarSaveButtonClick(object sender, RoutedEventArgs e)
    {
        
        if (string.IsNullOrEmpty(filePath))
            ToolbarSaveAsButtonClick(null, null);
        else
            Save(FilePath);
    }

    public async void ToolbarSaveAsButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save the script as",
            Filters = [
                new FileDialogFilter { Name = "AddyScript files", Extensions = { "add" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };
        
        var fileName = await dialog.ShowAsync(this);

        if (fileName?.Length > 0)
        {
            Save(fileName);
        }
    }

    public void ToolbarPrintButtonClick(object sender, RoutedEventArgs e)
    {
        
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

        foreach (var directory in Program.Directories)
            argsBuilder.Append(" -d ").Append(EscapeCmdLineArg(directory));

        foreach (var assemblyName in Program.Assemblies)
            argsBuilder.Append(" -r ").Append(EscapeCmdLineArg(assemblyName));

        var logPath = Path.ChangeExtension(Path.GetTempFileName(), ".log");
        argsBuilder.Append(" -l ").Append(EscapeCmdLineArg(logPath));

        //ClearErrors();

        try
        {
            var asis = Process.Start("asis", argsBuilder.ToString());
            asis.WaitForExit();

            if (asis.ExitCode <= 0) return;

            using var logReader = File.OpenText(logPath);
            if (logReader.ReadLine() != scriptPath) return;

            var parts = logReader.ReadLine()!.Split(',');
            var start = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

            parts = logReader.ReadLine()!.Split(',');
            var end = new ScriptLocation(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

            errorMessage = logReader.ReadLine();
            //ReportError(new ScriptElement(start, end));
        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard("Something went wrong",ex.Message, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
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
        Process.Start(new ProcessStartInfo(HELP_LINK) { UseShellExecute = true });
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

    private void EditorDocumentChanged(object sender, DocumentChangedEventArgs e)
    {
        // Handle text length changes
        if (scriptLength == e.NewDocument.TextLength) return;
        
        scriptLength = e.NewDocument.TextLength;
        Saved = scriptLength <= 0 && string.IsNullOrEmpty(filePath);
        UpdateUndoRedoFileSize();
    }

    private void EditorTextEntering(object sender, TextInputEventArgs e)
    {
        Console.WriteLine($"Text enterring: {e.Text}");
    }

    private void EditorTextEntered(object sender, TextInputEventArgs e)
    {
        Console.WriteLine($"Text enterred: {e.Text}");
    }
    
    #endregion
    
    #endregion
}
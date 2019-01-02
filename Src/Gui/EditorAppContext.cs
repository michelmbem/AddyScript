using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using AddyScript.Gui.Properties;
using AddyScript.Gui.Utilities;


namespace AddyScript.Gui
{
    /// <summary>
    /// Manages the application's lifetime.
    /// </summary>
    public class EditorAppContext : ApplicationContext
    {
        private static readonly List<Form> Forms = new List<Form>();
        
        private static string[] Directories;
        private static Assembly[] Assemblies;

        /// <summary>
        /// Initializes a new instance of EditorAppContext
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public EditorAppContext(string[] args)
        {
            string[] files = ParseOptions(args);

            if (files.Length > 0)
                foreach (string file in files)
                    OpenForm(file);
            else
                OpenForm(null);
            
            MainForm = Forms[0];
        }

        private static string[] ParseOptions(string[] args)
        {
            var files = new List<string>();
            var directories = new List<string>();
            var assemblies = new List<Assembly>();

            if (Settings.Default.ScriptContextSettings != null)
            {
                directories.AddRange(Settings.Default.ScriptContextSettings.Directories);
                assemblies.AddRange(Settings.Default.ScriptContextSettings.Assemblies);
            }

            if (directories.Count <= 0)
            {
                directories.Add(@"..\..\Examples");
            }

            if (assemblies.Count <= 0)
            {
                assemblies.Add(ScriptContext.Mscorlib);
                assemblies.Add(typeof(System.Diagnostics.Process).Assembly);
                assemblies.Add(typeof(System.Xml.XmlDocument).Assembly);
                assemblies.Add(typeof(System.Data.DataSet).Assembly);
                assemblies.Add(typeof(System.Drawing.Graphics).Assembly);
                assemblies.Add(typeof(Control).Assembly);
            }

            int index = 0;
            while (index < args.Length && args[index][0] == '-')
            {
                switch (args[index])
                {
                    case "-d":
                        if (index == args.Length - 1 || args[index + 1][0] == '-')
                            throw new ApplicationException("A directory name is required after -d");
                        string dirname = args[index + 1];
                        if (!Directory.Exists(dirname))
                            throw new ApplicationException("Directory " + dirname + " does not exist");
                        if (!directories.Contains(dirname)) directories.Add(dirname);
                        break;
                    case "-r":
                        if (index == args.Length - 1 || args[index + 1][0] == '-')
                            throw new ApplicationException("An assembly name is required after -r");
                        Assembly asm = ScriptContext.LoadAssembly(args[index + 1]);
                        if (!assemblies.Contains(asm)) assemblies.Add(asm);
                        break;
                    default:
                        throw new ApplicationException("Invalid option: " + args[index]);
                }

                index += 2;
            }

            while (index < args.Length)
            {
                files.Add(args[index]);
                ++index;
            }

            Directories = directories.ToArray();
            Assemblies = assemblies.ToArray();

            return files.ToArray();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ToolStripManager.Renderer = new EditorToolStripRenderer();

            try
            {
                Application.Run(new EditorAppContext(args));
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(ex.Message, "Initialization failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Settings.Default.ScriptContextSettings = new ScriptContextSettings(Directories, Assemblies);
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Creates an editor's window for a file and displays it.
        /// </summary>
        /// <param name="file">The file for which to create a window</param>
        public static void OpenForm(string file)
        {
            var editorForm = new EditorForm(file);
            editorForm.FormClosed += FormClosed;
            Forms.Add(editorForm);
            editorForm.Show();
        }

        public static void UpdateScriptContext(string[] directories, Assembly[] assemblies)
        {
            Directories = directories;
            Assemblies = assemblies;
        }

        public static ScriptContext GetScriptContext()
        {
            return new ScriptContext
                       {
                           SearchPath = Directories,
                           References = Assemblies
                       };
        }

        private static void FormClosed(object sender, FormClosedEventArgs e)
        {
            Forms.Remove((Form) sender);
        }

        /// <summary>
        /// Select another main window when the previous is closed.
        /// </summary>
        /// <param name="sender">The closed form</param>
        /// <param name="e">An <see cref="EventArgs"/></param>
        protected override void OnMainFormClosed(object sender, EventArgs e)
        {
            if (Forms.Count > 0)
                MainForm = Forms[0];
            else
                base.OnMainFormClosed(sender, e);
        }
    }
}

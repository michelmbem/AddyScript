using System;
using System.Collections.Generic;
using System.Windows.Forms;

using AddyScript.Gui.Properties;


namespace AddyScript.Gui
{
    public partial class OptionBox : Form
    {
        public OptionBox()
        {
            InitializeComponent();
        }

        private void OptionBox_Load(object sender, EventArgs e)
        {
            directoryList.Items.AddRange(EditorAppContext.Directories);
            assemblyList.Items.AddRange(EditorAppContext.Assemblies);
        }

        private void addDirectoryButton_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.Cancel) return;
            if (directoryList.Items.Contains(fbd.SelectedPath)) return;
            directoryList.Items.Add(fbd.SelectedPath);
        }

        private void deleteDirectoryButton_Click(object sender, EventArgs e)
        {
            if (directoryList.SelectedIndex >= 0)
            {
                DialogResult result = MessageBox.Show(Resources.ConfirmationBoxMessage,
                                                      Resources.ConfirmationBoxTitle,
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                    directoryList.Items.RemoveAt(directoryList.SelectedIndex);
            }
        }

        private void addAssemblyButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(assemblyTextBox.Text.Trim())) return;

            if (ScriptContext.LoadAssembly(assemblyTextBox.Text) == null)
                MessageBox.Show(string.Format(Resources.AssemblyLoadFailureMessage, assemblyTextBox.Text),
                                Resources.AssemblyLoadFailureTitle,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            else
            {
                if (!assemblyList.Items.Contains(assemblyTextBox.Text))
                    assemblyList.Items.Add(assemblyTextBox.Text);

                assemblyTextBox.Clear();
            }
        }

        private void deleteAssemblyButton_Click(object sender, EventArgs e)
        {
            if (assemblyList.SelectedIndex >= 0)
            {
                DialogResult result = MessageBox.Show(Resources.ConfirmationBoxMessage,
                                                      Resources.ConfirmationBoxTitle,
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                    assemblyList.Items.RemoveAt(assemblyList.SelectedIndex);
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            var directories = new List<string>();
            foreach (string directory in directoryList.Items)
                directories.Add(directory);
            EditorAppContext.Directories = directories.ToArray();

            var assemblies = new List<string>();
            foreach (string assemblyName in assemblyList.Items)
                assemblies.Add(assemblyName);
            EditorAppContext.Assemblies = assemblies.ToArray();

            DialogResult = DialogResult.OK;
        }
    }
}

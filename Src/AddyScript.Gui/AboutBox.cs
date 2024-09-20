using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace AddyScript.Gui
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void AboutBox_Load(object sender, System.EventArgs e)
        {
            Text = string.Format(Text, AssemblyTitle);
            lblVersion.Text = string.Format(lblVersion.Text, AssemblyVersion);
            lblDescription.Text = AssemblyDescription;
            lblCopyright.Text = AssemblyCopyright;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://" + lnkWebsite.Text);
        }

        #region Assembly Attribute Accessors

        internal static Assembly ThisAssembly
        {
            get => Assembly.GetExecutingAssembly();
        }

        internal static string AssemblyVersion
        {
            get => ThisAssembly.GetName().Version.ToString();
        }

        internal static T GetAssemblyAttribute<T>()
        {
            object[] attributes = ThisAssembly.GetCustomAttributes(typeof(T), false);
            if (attributes.Length <= 0) return default;
            return (T) attributes[0];
        }

        internal static string AssemblyTitle
        {
            get
            {
                var titleAttribute = GetAssemblyAttribute<AssemblyTitleAttribute>();
                return titleAttribute != null
                     ? titleAttribute.Title
                     : Path.GetFileNameWithoutExtension(ThisAssembly.Location);
            }
        }

        internal static string AssemblyDescription
        {
            get
            {
                var descriptionAttribute = GetAssemblyAttribute<AssemblyDescriptionAttribute>();
                return descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
            }
        }

        internal static string AssemblyCopyright
        {
            get
            {
                var copyrightAttribute = GetAssemblyAttribute<AssemblyCopyrightAttribute>();
                return copyrightAttribute != null ? copyrightAttribute.Copyright : string.Empty;
            }
        }

        #endregion
    }
}
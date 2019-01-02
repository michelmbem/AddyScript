using System.Diagnostics;
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

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://" + lnkWebsite.Text);
        }

        private void AboutBox_Load(object sender, System.EventArgs e)
        {
            Text = string.Format(Text, AssemblyTitle);
            lblVersion.Text = string.Format(lblVersion.Text, AssemblyVersion);
            lblDescription.Text = AssemblyDescription;
            lblCopyright.Text = AssemblyCopyright;
        }

        #region Assembly Attribute Accessors

        internal static string AssemblyTitle
        {
            get
            {
                object[] attributes =
                    Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                
                if (attributes.Length > 0)
                {
                    var titleAttribute = (AssemblyTitleAttribute) attributes[0];
                    if (!string.IsNullOrEmpty(titleAttribute.Title))
                        return titleAttribute.Title;
                }
                
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        internal static string AssemblyVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        internal static string AssemblyDescription
        {
            get
            {
                object[] attributes =
                    Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                    return "";
                return ((AssemblyDescriptionAttribute) attributes[0]).Description;
            }
        }

        internal static string AssemblyCopyright
        {
            get
            {
                object[] attributes =
                    Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                    return "";
                return ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
            }
        }

        #endregion
    }
}
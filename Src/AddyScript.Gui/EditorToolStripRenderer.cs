using System.Windows.Forms;


namespace AddyScript.Gui
{
    public class EditorToolStripRenderer : ToolStripProfessionalRenderer
    {
        public EditorToolStripRenderer() : base(new EditorColorTable())
        {
            RoundedEdges = false;
        }
    }
}

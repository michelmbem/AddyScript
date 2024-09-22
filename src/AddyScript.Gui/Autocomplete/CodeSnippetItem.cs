using AutocompleteMenuNS;


namespace AddyScript.Gui.Autocomplete
{
    internal class CodeSnippetItem : SnippetAutocompleteItem
    {
        public CodeSnippetItem(string text, string menuText = null, int imageIndex = -1)
            : base(text)
        {
            MenuText = menuText ?? text;
            ImageIndex = imageIndex;
            ToolTipTitle = ToolTipText = string.Empty;
        }

        public override void OnSelected(SelectedEventArgs e)
        {
            base.OnSelected(e);
            /*
            if (!Text.Contains("^")) return;

            var tb = Parent.TargetControlWrapper;
            var text = tb.Text;

            for (int i = Parent.Fragment.Start; i < text.Length; ++i)
                if (text[i] == '^')
                {
                    tb.SelectionStart = i;
                    tb.SelectionLength = 1;
                    tb.SelectedText = "";
                    return;
                }
            */
        }

        public override CompareResult Compare(string fragmentText)
        {
            return CompareResult.Visible;
        }
    }
}

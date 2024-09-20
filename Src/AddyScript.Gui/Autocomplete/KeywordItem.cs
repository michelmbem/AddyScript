using System;
using System.Windows.Forms;

using AutocompleteMenuNS;


namespace AddyScript.Gui.Autocomplete
{
    internal class KeywordItem(string text, int imageIndex, bool isMethodItem)
        : AutocompleteItem(text, imageIndex)
    {
        public bool IsMethodItem { get; private set; } = isMethodItem;

        public override void OnSelected(SelectedEventArgs e)
        {
            base.OnSelected(e);

            if (IsMethodItem) SendKeys.Send("{(}");
        }

        public override CompareResult Compare(string fragmentText)
        {
            return Text.StartsWith(fragmentText, StringComparison.InvariantCultureIgnoreCase)
                 ? CompareResult.Visible
                 : CompareResult.Hidden;
        }
    }
}

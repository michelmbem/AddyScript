namespace AddyScript.Gui.CallTips
{
    public class ParameterInfo(int start, int end, bool infinite)
    {
        public int Start { get; set; } = start;

        public int End { get; set; } = end;

        public bool Infinite { get; set; } = infinite;
    }
}
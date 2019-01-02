namespace AddyScript.Gui.CallTips
{
    public class ParameterInfo
    {
        public ParameterInfo(int start, int end, bool infinite)
        {
            Start = start;
            End = end;
            Infinite = infinite;
        }

        public int Start { get; set; }

        public int End { get; set; }

        public bool Infinite { get; set; }
    }
}
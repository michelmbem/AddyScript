namespace AddyScript.Gui.Terminal;

public static class EscapeSequences
{
    public static readonly byte[] CmdNewline = [10];
    public static readonly byte[] CmdRet = [13];
    public static readonly byte[] CmdEsc = [0x1b];
    public static readonly byte[] CmdDel = [0x7f];
    public static readonly byte[] CmdDelKey = [0x1b, (byte)'[', (byte)'3', (byte)'~'];
    public static readonly byte[] MoveUpApp = [0x1b, (byte)'O', (byte)'A'];
    public static readonly byte[] MoveUpNormal = [0x1b, (byte)'[', (byte)'A'];
    public static readonly byte[] MoveDownApp = [0x1b, (byte)'O', (byte)'B'];
    public static readonly byte[] MoveDownNormal = [0x1b, (byte)'[', (byte)'B'];
    public static readonly byte[] MoveLeftApp = [0x1b, (byte)'O', (byte)'D'];
    public static readonly byte[] MoveLeftNormal = [0x1b, (byte)'[', (byte)'D'];
    public static readonly byte[] MoveRightApp = [0x1b, (byte)'O', (byte)'C'];
    public static readonly byte[] MoveRightNormal = [0x1b, (byte)'[', (byte)'C'];
    public static readonly byte[] MoveHomeApp = [0x1b, (byte)'O', (byte)'H'];
    public static readonly byte[] MoveHomeNormal = [0x1b, (byte)'[', (byte)'H'];
    public static readonly byte[] MoveEndApp = [0x1b, (byte)'O', (byte)'F'];
    public static readonly byte[] MoveEndNormal = [0x1b, (byte)'[', (byte)'F'];
    public static readonly byte[] CmdTab = [9];
    public static readonly byte[] CmdBackTab = [0x1b, (byte)'[', (byte)'Z'];
    public static readonly byte[] CmdPageUp = [0x1b, (byte)'[', (byte)'5', (byte)'~'];
    public static readonly byte[] CmdPageDown = [0x1b, (byte)'[', (byte)'6', (byte)'~'];

    public static readonly byte[][] CmdF =
    [
        [0x1b, (byte)'O', (byte)'P'], /* F1 */
        [0x1b, (byte)'O', (byte)'Q'], /* F2 */
        [0x1b, (byte)'O', (byte)'R'], /* F3 */
        [0x1b, (byte)'O', (byte)'S'], /* F4 */
        [0x1b, (byte)'[', (byte)'1', (byte)'5', (byte)'~'], /* F5 */
        [0x1b, (byte)'[', (byte)'1', (byte)'7', (byte)'~'], /* F6 */
        [0x1b, (byte)'[', (byte)'1', (byte)'8', (byte)'~'], /* F7 */
        [0x1b, (byte)'[', (byte)'1', (byte)'9', (byte)'~'], /* F8 */
        [0x1b, (byte)'[', (byte)'2', (byte)'0', (byte)'~'], /* F9 */
        [0x1b, (byte)'[', (byte)'2', (byte)'1', (byte)'~'], /* F10 */
        [0x1b, (byte)'[', (byte)'2', (byte)'3', (byte)'~'], /* F11 */
        [0x1b, (byte)'[', (byte)'2', (byte)'4', (byte)'~'], /* F12 */
    ];
}
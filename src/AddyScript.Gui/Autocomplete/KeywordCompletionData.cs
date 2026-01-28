using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace AddyScript.Gui.Autocomplete;

internal enum KeywordType
{
    Statement,
    DataType,
    Constant,
    Function,
    Operator,
    Contextual
}

internal partial class KeywordCompletionData(string keyword, KeywordType type) : ICompletionData
{
    private static readonly Dictionary<KeywordType, IImage> KeywordIcons = new()
    {
        [KeywordType.Statement] = ImageFactory.LoadFontIcon("mdi-key"),
        [KeywordType.DataType] = ImageFactory.LoadFontIcon("mdi-shape-outline"),
        [KeywordType.Constant] = ImageFactory.LoadFontIcon("mdi-application-variable-outline"),
        [KeywordType.Function] = ImageFactory.LoadFontIcon("mdi-function-variant"),
        [KeywordType.Operator] = ImageFactory.LoadFontIcon("mdi-plus-minus"),
        [KeywordType.Contextual] = ImageFactory.LoadFontIcon("mdi-label-outline"),
    };

    static KeywordCompletionData()
    {
        const string keywords =
            """
            abs:3 abstract:0 acos:3 and:4 as:4 asin:3 atan:3 atan2:3 blob:1 bool:1 break:0 case:0 catch:0 ceil:3 chr:3
            class:0 closure:1 complex:1 const:0 constructor:0 contains:4 continue:0 cos:3 cosh:3 date:1 days:3 decimal:1
            default:0 deg2rad:3 do:0 E:2 duration:1 else:0 endswith:4 eval:3 event:0 exit:3 exp:3 extern:0 false:2 final:0
            finally:0 float:1 floor:3 for:0 foreach:0 format:3 function:0 goto:0 hash:3 hours:3 I:2 if:0 import:0 in:4 int:1
            is:4 let:0 list:1 log:3 log10:3 log2:3 long:1 map:1 matches:4 max:3 MAXDATE:2 MAXFLOAT:2 MAXINT:2 milliseconds:3
            min:3 MINDATE:2 MINFLOAT:2 MININT:2 minutes:3 NAN:2 new:4 NEWLINE:2 NINFINITY:2 not:4 now:3 null:2 object:1
            operator:0 or:4 ord:3 pack:3 PI:2 PINFINITY:2 print:3 println:3 private:0 property:0 protected:0 public:0 queue:1
            rad2deg:3 rand:3 randint:3 rational:1 read:5 readln:3 resource:1 return:0 round:3 seconds:3 set:1 sign:3 sin:3
            sinh:3 sleep:3 sqrt:3 stack:1 startswith:4 static:0 string:1 super:5 switch:0 tan:3 tanh:3 this:5 throw:0 true:2
            trunc:3 try:0 tuple:1 typeof:4 unpack:3 var:0 void:1 when:0 while:0 with:4 write:5 yield:0
            """;

        All = [.. from keyword in KeywordSplitRegex().Split(keywords)
                  where keyword.Length > 0
                  select keyword.Split(':') into parts
                  let typeOrdinal = int.Parse(parts[1])
                  select new KeywordCompletionData(parts[0], (KeywordType)typeOrdinal)];
    }

    public static List<KeywordCompletionData> All { get; }

    public IImage Image => KeywordIcons[type];

    public string Text => keyword;

    public object Content => keyword;

    public object Description => null;

    public double Priority => 0;

    public static List<KeywordCompletionData> AllMatching(string prefix) =>
        All.FindAll(item => item.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"\s+")]
    private static partial Regex KeywordSplitRegex();

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        TextDocument document = textArea.Document;
        int offset = completionSegment.Offset;

        int wordStart = TextUtilities.GetNextCaretPosition(
            document,
            offset,
            LogicalDirection.Backward,
            CaretPositioningMode.WordStart);

        int wordEnd = TextUtilities.GetNextCaretPosition(
            document,
            wordStart,
            LogicalDirection.Forward,
            CaretPositioningMode.WordBorder);

        int wordLength = wordEnd > wordStart ? wordEnd - wordStart : 0;
        string textToInsert = type == KeywordType.Function ? Text + '(' : Text;
        document.Replace(wordStart, wordLength, textToInsert, OffsetChangeMappingType.RemoveAndInsert);
    }
}
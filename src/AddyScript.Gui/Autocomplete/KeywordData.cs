using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

internal partial class KeywordData(string word, KeywordType type) :
    AbstractCompletionData(KeywordTypeIcon(type), word, word, $"Insert {word}")
{
    static KeywordData()
    {
        const string keywords =
            @"abs?3 abstract?0 acos?3 as?0 asin?3 atan?3 atan2?3 blob?0 bool?1 break?0 case?0 catch?0 ceil?3 chr?3
            class?0 closure?1 complex?1 const?0 constructor?0 contains?4 continue?0 cos?3 cosh?3 date?1 decimal?1
            default?0 deg2rad?3 do?0 E?2 else?0 endswith?4 eval?3 event?0 exp?3 extern?0 false?2 final?0 finally?0
            float?1 floor?3 for?0 foreach?0 format?3 function?0 goto?0 I?2 if?0 import?0 in?4 int?1 is?4 list?1 log?3
            log10?3 log2?3 long?1 map?1 matches?4 max?3 MAXDATE?2 MAXFLOAT?2 MAXINT?2 min?3 MINDATE?2 MINFLOAT?2 MININT?2
            NAN?2 new?4 NEWLINE?2 NINFINITY?2 not?0 now?3 null?2 object?1 operator?0 ord?3 pack?3 PI?2 PINFINITY?2 print?3
            println?3 private?0 property?0 protected?0 public?0 queue?1 rad2deg?3 rand?3 randint?3 rational?1 read?5
            readln?3 resource?1 return?0 round?3 set?1 sign?3 sin?3 sinh?3 sqrt?3 stack?1 startswith?4 static?0 string?1
            super?0 switch?0 tan?3 tanh?3 this?5 throw?0 true?2 trunc?3 try?0 tuple?0 typeof?4 unpack?3 var?0 void?1 while?0
            with?0 write?5 yield?0";

        foreach (string keyword in KeywordRegex().Split(keywords))
        {
            if (keyword.Length == 0) continue;

            string[] parts = keyword.Split('?');
            int typeOrdinal = int.Parse(parts[1]);

            All.Add(new(parts[0], (KeywordType)typeOrdinal));
        }
    }

    public static List<KeywordData> All { get; } = [];

    public static List<KeywordData> AllMatching(string prefix) =>
        All.FindAll(k => k.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"\s+")]
    private static partial Regex KeywordRegex();

    private static IImage KeywordTypeIcon(KeywordType type) => type switch
    {
        KeywordType.Statement => ImageFactory.LoadFontIcon("mdi-label-outline"),
        KeywordType.DataType => ImageFactory.LoadFontIcon("mdi-shape-outline"),
        KeywordType.Constant => ImageFactory.LoadFontIcon("mdi-application-variable-outline"),
        KeywordType.Function => ImageFactory.LoadFontIcon("mdi-script-outline"),
        KeywordType.Operator => ImageFactory.LoadFontIcon("mdi-plus-minus"),
        KeywordType.Contextual => ImageFactory.LoadFontIcon("mdi-label"),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public override void Complete(TextArea textArea, ISegment segment, EventArgs args)
    {
        TextDocument document = textArea.Document;
        int offset = segment.Offset;

        int wordStart = TextUtilities.GetNextCaretPosition(
           document,
           offset,
           LogicalDirection.Backward,
           CaretPositioningMode.WordStart);

        int wordEnd = TextUtilities.GetNextCaretPosition(
            document,
            offset,
            LogicalDirection.Forward,
            CaretPositioningMode.WordBorder);

        int wordLength = wordEnd > wordStart ? wordEnd - wordStart : 0;
        string replacement = type == KeywordType.Function ? $"{Text}(" : Text;
        document.Replace(wordStart, wordLength, replacement, OffsetChangeMappingType.RemoveAndInsert);
    }
}

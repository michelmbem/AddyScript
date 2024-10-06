using System.Collections.Generic;

using AddyScript.Ast.Statements;
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.Frames;


namespace AddyScript.Translators.Utility;


public class InterpreterState(Stack<MethodFrame> frames, MethodFrame rootFrame,
                              string fileName, MissingReferenceAction misRefAct,
                              JumpCode jumpCode, List<DataItem> yieldedValues,
                              Goto lastGoto)
{
    public readonly Stack<MethodFrame> frames = frames;
    public readonly MethodFrame rootFrame = rootFrame;
    public readonly string fileName = fileName;
    public readonly MissingReferenceAction misRefAct = misRefAct;
    public readonly JumpCode jumpCode = jumpCode;
    public readonly List<DataItem> yieldedValues = yieldedValues;
    public readonly Goto lastGoto = lastGoto;
}

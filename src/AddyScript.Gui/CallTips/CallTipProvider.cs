using System.Collections.Generic;
using AddyScript.Runtime;

namespace AddyScript.Gui.CallTips;
public static class CallTipProvider
{
    private static readonly Dictionary<string, CallTipInfo> callTips = [];

    static CallTipProvider()
    {
        foreach (InnerFunction function in InnerFunction.Globals)
            callTips[function.Name] = new CallTipInfo(function);
    }

    public static bool IsDefined(string fName) => callTips.ContainsKey(fName);

    public static CallTipInfo GetCallTipInfo(string fName)
    {
        CallTipInfo callTip = callTips[fName];
        callTip.Reset();
        return callTip;
    }
}
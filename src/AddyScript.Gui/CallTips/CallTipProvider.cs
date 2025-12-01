using System.Collections.Generic;
using AddyScript.Runtime;

namespace AddyScript.Gui.CallTips;

internal static class CallTipProvider
{
    private static readonly Dictionary<string, CallTipInfo> Registry = [];

    static CallTipProvider()
    {
        foreach (var innerFunction in InnerFunction.Globals)
            Registry[innerFunction.Name] = new CallTipInfo(innerFunction);
    }

    public static bool IsDefined(string functionName) => Registry.ContainsKey(functionName);

    public static CallTipInfo GetCallTip(string functionName) => Registry[functionName];
}
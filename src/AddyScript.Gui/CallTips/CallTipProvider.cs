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

    public static bool IsDefined(string fName) => Registry.ContainsKey(fName);

    public static CallTipInfo GetCallTipInfo(string fName) => Registry[fName];
}
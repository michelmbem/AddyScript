namespace AddyScript.Runtime
{
    /// <summary>
    /// Class identifiers: uniquely identify each primitive type.<br/>
    /// Play the same role than TypeCodes in .Net.
    /// </summary>
    public enum ClassID
    {
        Void,
        Boolean,
        Integer,
        Long,
        Rational,
        Float,
        Decimal,
        Complex,
        Date,
        String,
        List,
        Map,
        Set,
        Queue,
        Stack,
        Object,
        Resource,
        Closure
    }
}
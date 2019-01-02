namespace AddyScript.Runtime
{
    /// <summary>
    /// Used to manage jumps.
    /// </summary>
    public enum JumpCode
    {
        /// <summary>
        /// Execution may resume normally
        /// </summary>
        None,

        /// <summary>
        /// The current block should be left here
        /// </summary>
        Continue,

        /// <summary>
        /// The current loop should stop here
        /// </summary>
        Break,

        /// <summary>
        /// Jump to a specific address
        /// </summary>
        Goto,

        /// <summary>
        /// The current function should return here
        /// </summary>
        Return
    }
}
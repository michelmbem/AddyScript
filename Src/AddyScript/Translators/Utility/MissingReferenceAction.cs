namespace AddyScript.Translators.Utility
{
    /// <summary>
    /// Used to handle references to undefined objects.
    /// </summary>
    public enum MissingReferenceAction
    {
        /// <summary>
        /// Indicates that nothing should be done.
        /// </summary>
        Ignore,

        /// <summary>
        /// Indicates that the missing object should be created.
        /// </summary>
        Create,

        /// <summary>
        /// Indicates that an exception should be thrown.
        /// </summary>
        Fail
    }
}
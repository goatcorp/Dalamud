namespace Dalamud.Interface.Internal.Scratchpad
{
    /// <summary>
    /// The load status of a <see cref="ScratchpadDocument"/> class.
    /// </summary>
    internal enum ScratchLoadStatus
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// Failure to compile.
        /// </summary>
        FailureCompile,

        /// <summary>
        /// Failure to initialize.
        /// </summary>
        FailureInit,

        /// <summary>
        /// Success.
        /// </summary>
        Success,
    }
}

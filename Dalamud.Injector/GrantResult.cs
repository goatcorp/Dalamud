namespace Dalamud.Injector
{
    /// <summary>
    /// Outcome of ensuring an AppContainer has access to a path.
    /// </summary>
    public enum GrantResult
    {
        /// <summary>
        /// The container already held the requested access. The DACL wasn't modified.
        /// </summary>
        AlreadyGranted,

        /// <summary>
        /// The access was granted by writing the DACL.
        /// </summary>
        Granted,

        /// <summary>
        /// The DACL could not be written.
        /// </summary>
        AccessDenied,
    }
}

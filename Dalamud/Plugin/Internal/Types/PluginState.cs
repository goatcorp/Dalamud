namespace Dalamud.Plugin.Internal.Types
{
    /// <summary>
    /// Values representing plugin load state.
    /// </summary>
    internal enum PluginState
    {
        /// <summary>
        /// Plugin is defined, but unloaded.
        /// </summary>
        Unloaded,

        /// <summary>
        /// Plugin has thrown an error during unload.
        /// </summary>
        UnloadError,

        /// <summary>
        /// Currently loading.
        /// </summary>
        InProgress,

        /// <summary>
        /// Load is successful.
        /// </summary>
        Loaded,

        /// <summary>
        /// Plugin has thrown an error during loading.
        /// </summary>
        LoadError,
    }
}

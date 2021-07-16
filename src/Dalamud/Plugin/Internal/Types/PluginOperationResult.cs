using System;

namespace Dalamud.Plugin.Internal.Types
{
    /// <summary>
    /// This represents the result of a an operation taken against a plugin.
    /// Loading, unloading, installation, etc.
    /// </summary>
    internal enum PluginOperationResult
    {
        /// <summary>
        /// The result is unknown. Should not be used.
        /// </summary>
        [Obsolete("Do not use this", error: true)]
        Unknown,

        /// <summary>
        /// The result is pending. Take a seat and wait.
        /// </summary>
        Pending,

        /// <summary>
        /// The operation was successful.
        /// </summary>
        Success,

        /// <summary>
        /// During the plugin operation, an unexpected error occurred.
        /// </summary>
        UnknownError,

        /// <summary>
        /// The plugin state was invalid for the attempted operation.
        /// </summary>
        InvalidState,

        /// <summary>
        /// The plugin applicable version is not compativle with the currently running game.
        /// </summary>
        InvalidGameVersion,

        /// <summary>
        /// The plugin API level is not compatible with the currently running Dalamud.
        /// </summary>
        InvalidApiLevel,

        /// <summary>
        /// During loading, the current plugin was marked as disabled.
        /// </summary>
        InvalidStateDisabled,

        /// <summary>
        /// During loading, another plugin was detected with the same internal name.
        /// </summary>
        InvalidStateDuplicate,
    }
}

using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Utility;

/// <summary>
/// Helper class for checking registered events and nagging if a plugin dev doesn't unregister them.
/// </summary>
internal static class PluginCleanupNag
{
    /// <summary>
    /// Check the passed in even handlers for any listeners still registered, if there are any nag that they should have been cleaned up.
    /// </summary>
    /// <param name="plugin">Source plugin.</param>
    /// <param name="log">Module Log to report to.</param>
    /// <param name="events">Event listeners.</param>
    /// <typeparam name="T">Event type.</typeparam>
    internal static void CheckEvent(LocalPlugin plugin, ModuleLog log, params Delegate?[] events)
    {
        foreach (var subscribableEvent in events)
        {
            if (subscribableEvent is null) continue;

            var subscribedEvents = subscribableEvent.GetInvocationList().Length;

            if (subscribedEvents > 0)
            {
                log.Warning($"{plugin.InternalName} is leaking {subscribedEvents} {subscribableEvent.Method.DeclaringType?.FullName ?? "Error Resolving Type"} listeners! Make sure that all of them are unregistered properly.");
            }
        }
    }
}

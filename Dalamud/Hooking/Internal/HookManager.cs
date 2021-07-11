using System;
using System.Collections.Generic;

namespace Dalamud.Hooking.Internal
{
    /// <summary>
    /// This class manages the final disposition of hooks, cleaning up any that have not reverted their changes.
    /// </summary>
    internal class HookManager : IDisposable
    {
        // private readonly Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="HookManager"/> class.
        /// </summary>
        /// <param name="dalamud">Dalamud instance.</param>
        public HookManager(Dalamud dalamud)
        {
            _ = dalamud;
            // this.dalamud = dalamud;
        }

        /// <summary>
        /// Gets a static list of tracked and registered hooks.
        /// </summary>
        internal static List<HookInfo> TrackedHooks { get; } = new();

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}

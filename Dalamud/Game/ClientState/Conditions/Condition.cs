using System;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.ClientState.Conditions
{
    /// <summary>
    /// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public class Condition
    {
        /// <summary>
        /// The current max number of conditions. You can get this just by looking at the condition sheet and how many rows it has.
        /// </summary>
        public const int MaxConditionEntries = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition"/> class.
        /// </summary>
        /// <param name="resolver">The ClientStateAddressResolver instance.</param>
        internal Condition(ClientStateAddressResolver resolver)
        {
            this.Address = resolver.ConditionFlags;
        }

        /// <summary>
        /// Gets the condition array base pointer.
        /// </summary>
        public IntPtr Address { get; private set; }

        /// <summary>
        /// Check the value of a specific condition/state flag.
        /// </summary>
        /// <param name="flag">The condition flag to check.</param>
        public unsafe bool this[ConditionFlag flag]
        {
            get
            {
                var idx = (int)flag;

                if (idx < 0 || idx >= MaxConditionEntries)
                    return false;

                return *(bool*)(this.Address + idx);
            }
        }

        /// <summary>
        /// Check if any condition flags are set.
        /// </summary>
        /// <returns>Whether any single flag is set.</returns>
        public bool Any()
        {
            for (var i = 0; i < MaxConditionEntries; i++)
            {
                var typedCondition = (ConditionFlag)i;
                var cond = this[typedCondition];

                if (cond)
                    return true;
            }

            return false;
        }
    }
}

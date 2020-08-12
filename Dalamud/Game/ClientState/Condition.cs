using System;
using System.Runtime.CompilerServices;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
    /// </summary>
    public class Condition
    {
        internal readonly IntPtr conditionArrayBase;

        /// <summary>
        /// The current max number of conditions. You can get this just by looking at the condition sheet and how many rows it has.
        /// </summary>
        public const int MaxConditionEntries = 100;

        internal Condition( ClientStateAddressResolver resolver )
        {
            this.conditionArrayBase = resolver.ConditionFlags;
        }

        /// <summary>
        /// Check the value of a specific condition/state flag.
        /// </summary>
        /// <param name="flag">The condition flag to check</param>
        public unsafe bool this[ ConditionFlag flag ]
        {
            get
            {
                var idx = ( int )flag;
                
                if( idx > MaxConditionEntries || idx < 0 )
                    return false;
                
                return *( bool* )( this.conditionArrayBase + idx );
            }
        }

        public bool Any() {
            var didAny = false;

            for (var i = 0; i < MaxConditionEntries; i++)
            {
                var typedCondition = (ConditionFlag)i;
                var cond = this[typedCondition];

                if (!cond)
                {
                    continue;
                }

                didAny = true;
            }

            return didAny;
        }
    }
}

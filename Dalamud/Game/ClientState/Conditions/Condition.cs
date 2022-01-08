using System;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.Conditions
{
    /// <summary>
    /// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed partial class Condition
    {
        /// <summary>
        /// The current max number of conditions. You can get this just by looking at the condition sheet and how many rows it has.
        /// </summary>
        public const int MaxConditionEntries = 100;

        private readonly bool[] cache = new bool[MaxConditionEntries];

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition"/> class.
        /// </summary>
        /// <param name="resolver">The ClientStateAddressResolver instance.</param>
        internal Condition(ClientStateAddressResolver resolver)
        {
            this.Address = resolver.ConditionFlags;
        }

        /// <summary>
        /// A delegate type used with the <see cref="ConditionChange"/> event.
        /// </summary>
        /// <param name="flag">The changed condition.</param>
        /// <param name="value">The value the condition is set to.</param>
        public delegate void ConditionChangeDelegate(ConditionFlag flag, bool value);

        /// <summary>
        /// Event that gets fired when a condition is set.
        /// Should only get fired for actual changes, so the previous value will always be !value.
        /// </summary>
        public event ConditionChangeDelegate? ConditionChange;

        /// <summary>
        /// Gets the condition array base pointer.
        /// </summary>
        public IntPtr Address { get; private set; }

        /// <summary>
        /// Check the value of a specific condition/state flag.
        /// </summary>
        /// <param name="flag">The condition flag to check.</param>
        public unsafe bool this[int flag]
        {
            get
            {
                if (flag < 0 || flag >= MaxConditionEntries)
                    return false;

                return *(bool*)(this.Address + flag);
            }
        }

        /// <inheritdoc cref="this[int]"/>
        public unsafe bool this[ConditionFlag flag]
            => this[(int)flag];

        /// <summary>
        /// Check if any condition flags are set.
        /// </summary>
        /// <returns>Whether any single flag is set.</returns>
        public bool Any()
        {
            for (var i = 0; i < MaxConditionEntries; i++)
            {
                var cond = this[i];

                if (cond)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Enables the hooks of the Condition class function.
        /// </summary>
        public void Enable()
        {
            // Initialization
            for (var i = 0; i < MaxConditionEntries; i++)
                this.cache[i] = this[i];

            Service<Framework>.Get().Update += this.FrameworkUpdate;
        }

        private void FrameworkUpdate(Framework framework)
        {
            for (var i = 0; i < MaxConditionEntries; i++)
            {
                var value = this[i];

                if (value != this.cache[i])
                {
                    this.cache[i] = value;

                    try
                    {
                        this.ConditionChange?.Invoke((ConditionFlag)i, value);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"While invoking {nameof(this.ConditionChange)}, an exception was thrown.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
    /// </summary>
    public sealed partial class Condition : IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// Finalizes an instance of the <see cref="Condition" /> class.
        /// </summary>
        ~Condition()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes this instance, alongside its hooks.
        /// </summary>
        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (this.isDisposed)
                return;

            if (disposing)
            {
                Service<Framework>.Get().Update -= this.FrameworkUpdate;
            }

            this.isDisposed = true;
        }
    }
}

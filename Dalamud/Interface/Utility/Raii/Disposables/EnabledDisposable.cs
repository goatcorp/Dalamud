// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around re-enabling the state. </summary>
    public sealed class EnabledDisposable : IDisposable
    {
        /// <summary> The stored number of ended disposables as a workaround. </summary>
        public int Count { get; private set; }

        /// <summary> Enforce an enabled state by popping the global number of disabled states. </summary>
        /// <param name="condition"> Whether to force the enabled state. </param>
        /// <returns> A disposable object that will push the prior number of enabled states after leaving scope. Use with using. </returns>
        /// <remarks> This is a workaround for the problem that you can not force the state to be enabled without knowing the disabled stack's size. </remarks>
        public EnabledDisposable(bool condition)
        {
            if (!condition)
                return;

            this.Count = DisabledDisposable.GlobalCount;
            var count = this.Count;
            while (count-- > 0)
                ImGui.EndDisabled();
        }

        /// <inheritdoc cref="EnabledDisposable(bool)"/>
        public EnabledDisposable()
        {
            this.Count = DisabledDisposable.GlobalCount;
            var count = this.Count;
            while (count-- > 0)
                ImGui.EndDisabled();
        }

        /// <summary> Return to the prior disabled state. </summary>
        public void Dispose()
        {
            while (this.Count-- > 0)
                ImGui.BeginDisabled(true);
        }
    }
}

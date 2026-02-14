// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tooltips. </summary>
    public ref struct TooltipDisposable : IDisposable
    {
        /// <summary> Whether the tooltip is still open. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="TooltipDisposable"/> struct. </summary>
        /// <returns> A disposable object. Use with using. </returns>
        /// <remarks> Anything drawn while a tooltip is active will be drawn in a little popup window on your cursor. </remarks>
        public TooltipDisposable()
        {
            this.Alive = true;
            ImGui.BeginTooltip();
        }

        /// <summary> End the tooltip on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            ImGui.EndTooltip();
            this.Alive = false;
        }

        /// <summary> End a Tooltip without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndTooltip();
    }
}

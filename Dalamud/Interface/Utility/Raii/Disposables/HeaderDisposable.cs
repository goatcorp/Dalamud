// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui collapsing headers that also push an ID. </summary>
    public ref struct HeaderDisposable : IDisposable
    {
        /// <summary> Whether the collapsing header is currently opened. </summary>
        public readonly bool Success;

        /// <summary> Whether the ID node is already popped. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="HeaderDisposable"/> struct. </summary>
        /// <param name="label"> The header label and ID as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="flags"> Additional flags to control the header's behaviour. </param>
        /// <returns> A disposable object that evaluates to true if the collapsing header is currently open. Use with using. </returns>
        internal HeaderDisposable(ImU8String label, ImGuiTreeNodeFlags flags)
        {
            this.Success = ImGui.CollapsingHeader(label, flags);
            if (this.Success)
                ImGui.PushID(label);
            this.Alive = true;
        }

        /// <summary>Initializes a new instance of the <see cref="HeaderDisposable"/> struct. </summary>
        /// <param name="label"> The header label and ID as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="visible"> If true, displays a small close button on the upper right of the header, which will set this to false when clicked. If false, do not display the header at all. </param>
        /// <param name="flags"> Additional flags to control the header's behaviour. </param>
        /// <returns> A disposable object that evaluates to true if the collapsing header is currently open. Use with using. </returns>
        internal HeaderDisposable(ImU8String label, ref bool visible, ImGuiTreeNodeFlags flags)
        {
            this.Success = ImGui.CollapsingHeader(label, ref visible, flags);
            if (this.Success)
                ImGui.PushID(label);
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(HeaderDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(HeaderDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(HeaderDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(HeaderDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(HeaderDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(HeaderDisposable i, bool value)
            => i.Success || value;

        /// <summary> Pop the tree node on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.PopID();
            this.Alive = false;
        }
    }
}

// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tab items. </summary>
    public unsafe ref struct TabItemDisposable : IDisposable
    {
        /// <summary> Whether creating the tab item succeeded, and it is currently selected. </summary>
        public readonly bool Success;

        /// <summary> Whether the tab item is already ended. </summary>
        public bool Alive { get; private set; }

        /// <inheritdoc cref="TabItemDisposable(ImU8String,ref bool,ImGuiTabItemFlags)"/>
        internal TabItemDisposable(ImU8String label)
        {
            this.Success = ImGui.BeginTabItem(label);
            this.Alive   = true;
        }

        /// <inheritdoc cref="TabItemDisposable(ImU8String,ref bool,ImGuiTabItemFlags)"/>
        internal TabItemDisposable(ImU8String label, ImGuiTabItemFlags flags)
        {
            this.Success = ImGui.BeginTabItem(label, flags);
            this.Alive   = true;
        }

        /// <inheritdoc cref="TabItemDisposable(ImU8String,ref bool,ImGuiTabItemFlags)"/>
        internal TabItemDisposable(ImU8String label, ref bool open)
        {
            this.Success = ImGui.BeginTabItem(label, ref open);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="TabItemDisposable"/> struct. </summary>
        /// <param name="label"> The tab item label as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="open"> Whether the tab item is currently open. If this is provided, the tab item will render a close button that controls this value. </param>
        /// <param name="flags"> Additional flags to control the tab item's behaviour. </param>
        /// <returns> A disposable object that evaluates to true if the tab item is currently opened. Use with using. </returns>
        internal TabItemDisposable(ImU8String label, scoped ref bool open, ImGuiTabItemFlags flags)
        {
            this.Success = ImGui.BeginTabItem(label, ref open, flags);
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(TabItemDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(TabItemDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(TabItemDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(TabItemDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(TabItemDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(TabItemDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the tab item on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndTabItem();
            this.Alive = false;
        }

        /// <summary> End a tab item without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndTabItem();
    }
}

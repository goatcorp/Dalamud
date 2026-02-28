// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui popups. </summary>
    public ref struct PopupDisposable : IDisposable
    {
        /// <summary> Whether creating the popup succeeded and it is open. </summary>
        public readonly bool Success;

        /// <summary> Whether the popup is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="PopupDisposable"/> struct. </summary>
        /// <param name="id"> The ID of the popup as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="flags"> Flags to forward to the popup window creation. </param>
        /// <returns> A disposable object that evaluates to true if the begun popup is currently open. Use with using. </returns>
        internal PopupDisposable(ImU8String id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            this.Success = ImGui.BeginPopup(id, flags);
            this.Alive   = true;
        }

        /// <summary> Open a popup when clicking on the last drawn item and begin it. </summary>
        /// <param name="id"> The ID of the popup. </param>
        /// <param name="flags"> Additional flags to control the popups behavior and the button to click to open it. </param>
        /// <returns> A disposable object that evaluates to true if the begun popup is currently open. Use with using. </returns>
        internal static PopupDisposable ContextItem(ImU8String id, ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight)
            => new(ImGui.BeginPopupContextItem(id, flags));

        /// <summary> Open a popup when clicking on empty space in the current window and begin it. </summary>
        /// <param name="id"> The ID of the popup. </param>
        /// <param name="flags"> Additional flags to control the popups behavior and the button to click to open it. </param>
        /// <returns> A disposable object that evaluates to true if the begun popup is currently open. Use with using. </returns>
        internal static PopupDisposable ContextWindow(ImU8String id, ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight)
            => new(ImGui.BeginPopupContextWindow(id, flags));

        /// <summary> Begin a modal popup and end it on leaving scope. </summary>
        /// <param name="title"> The title of the popup as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="open"> Whether the modal should be kept open or closed. </param>
        /// <param name="flags"> Flags to pass to the popup window creation. </param>
        /// <returns> A disposable object that evaluates to true if the begun popup is currently open. Use with using. </returns>
        /// <remarks> Modal popups block interactions behind the popup and can not be closed by the user, add a dimming background and have a title bar. </remarks>
        internal static PopupDisposable Modal(ImU8String title, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            => new(ImGui.BeginPopupModal(title, ref open, flags));

        /// <inheritdoc cref="Modal(ImU8String,ref bool,ImGuiWindowFlags)"/>
        internal static PopupDisposable Modal(ImU8String title, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            => new(ImGui.BeginPopupModal(title, flags));

        private PopupDisposable(bool success)
        {
            this.Success = success;
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(PopupDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(PopupDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(PopupDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(PopupDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(PopupDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(PopupDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the popup on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndPopup();

            this.Alive = false;
        }

        /// <summary> End a popup without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndPopup();
    }
}

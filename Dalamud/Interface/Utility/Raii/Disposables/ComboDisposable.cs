using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around creating pre-table style column separation. </summary>
    public ref struct ComboDisposable : IDisposable
    {
        /// <summary> Whether creating the combo box succeeded and it is expanded. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the combo box is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ComboDisposable"/> struct. </summary>
        /// <param name="label"> The combo label as text. </param>
        /// <param name="previewValue"> The currently displayed string in the combo field as text. </param>
        /// <returns> A disposable object that evaluates to true if the begun combo popup is currently open. Use with using. </returns>
        internal ComboDisposable(ImU8String label, ImU8String previewValue)
        {
            this.Success = ImGui.BeginCombo(label, previewValue);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="ComboDisposable"/> struct. </summary>
        /// <param name="label"> The combo label as text. </param>
        /// <param name="previewValue"> The currently displayed string in the combo field as text. </param>
        /// <param name="flags"> Additional flags to control the combo's behaviour. </param>
        /// <returns> A disposable object that evaluates to true if the begun combo popup is currently open. Use with using. </returns>
        internal ComboDisposable(ImU8String label, ImU8String previewValue, ImGuiComboFlags flags)
        {
            this.Success = ImGui.BeginCombo(label, previewValue, flags);
            this.Alive   = true;
        }

        public static implicit operator bool(ComboDisposable value)
            => value.Success;

        public static bool operator true(ComboDisposable i)
            => i.Success;

        public static bool operator false(ComboDisposable i)
            => !i.Success;

        public static bool operator !(ComboDisposable i)
            => !i.Success;

        public static bool operator &(ComboDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(ComboDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the combo box on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndCombo();

            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a combo box without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndCombo();
#pragma warning restore SA1204
    }
}

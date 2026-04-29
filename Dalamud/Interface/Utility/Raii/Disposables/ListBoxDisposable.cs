using System.Numerics;

using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui list boxes. </summary>
    public ref struct ListBoxDisposable : IDisposable
    {
        /// <summary> Whether creating the list box succeeded and it is expanded. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the list box is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ListBoxDisposable"/> struct. </summary>
        /// <param name="label"> The list box label as text. </param>
        /// <returns> A disposable object that evaluates to true if the begun list box is currently visible. Use with using. </returns>
        internal ListBoxDisposable(ImU8String label)
        {
            this.Success = ImGui.BeginListBox(label);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="ListBoxDisposable"/> struct. </summary>
        /// <param name="label"> The list box label as text. </param>
        /// <param name="size">
        /// The size of the box. If these values are greater than 0, use them as pixel counts.
        /// If .X == 0, use the current item width.
        /// If .y == 0, use an arbitrary default height of about 7 items.
        /// If they are less than 0, align to the right or bottom respectively.
        /// </param>
        /// <returns> A disposable object that evaluates to true if the begun list box is currently visible. Use with using. </returns>
        internal ListBoxDisposable(ImU8String label, Vector2 size)
        {
            this.Success = ImGui.BeginListBox(label, size);
            this.Alive   = true;
        }

        public static implicit operator bool(ListBoxDisposable value)
            => value.Success;

        public static bool operator true(ListBoxDisposable i)
            => i.Success;

        public static bool operator false(ListBoxDisposable i)
            => !i.Success;

        public static bool operator !(ListBoxDisposable i)
            => !i.Success;

        public static bool operator &(ListBoxDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(ListBoxDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the list box on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndListBox();

            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a list box without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndListBox();
#pragma warning restore SA1204
    }
}

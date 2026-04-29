using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui menus. </summary>
    public ref struct MenuDisposable : IDisposable
    {
        /// <summary> Whether creating the menu bar succeeded. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the menu is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="MenuDisposable"/> struct. </summary>
        /// <param name="label"> The label of the menu as text. </param>
        /// <returns> A disposable object that evaluates to true if the menu was created. Use with using. </returns>
        /// <remarks> Can create or append to a menu that already exists in a menu bar. </remarks>
        internal MenuDisposable(ImU8String label)
        {
            this.Success = ImGui.BeginMenu(label);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="MenuDisposable"/> struct. </summary>
        /// <param name="label"> The label of the menu as text. </param>
        /// <param name="enabled"> Whether the menu is enabled or not. </param>
        /// <returns> A disposable object that evaluates to true if the menu was created. Use with using. </returns>
        /// <remarks> Can create or append to a menu that already exists in a menu bar. </remarks>
        internal MenuDisposable(ImU8String label, bool enabled)
        {
            this.Success = ImGui.BeginMenu(label, enabled);
            this.Alive   = true;
        }

        public static implicit operator bool(MenuDisposable value)
            => value.Success;

        public static bool operator true(MenuDisposable i)
            => i.Success;

        public static bool operator false(MenuDisposable i)
            => !i.Success;

        public static bool operator !(MenuDisposable i)
            => !i.Success;

        public static bool operator &(MenuDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(MenuDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the menu on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndMenu();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a menu without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndMenu();
#pragma warning restore SA1204
    }
}

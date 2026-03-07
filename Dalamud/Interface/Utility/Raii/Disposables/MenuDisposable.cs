// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui menus. </summary>
    public ref struct MenuDisposable : IDisposable
    {
        /// <summary> Whether creating the menu bar succeeded. </summary>
        public readonly bool Success;

        /// <summary> Whether the menu is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="MenuDisposable"/> struct. </summary>
        /// <param name="label"> The label of the menu as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <returns> A disposable object that evaluates to true if the menu was created. Use with using. </returns>
        /// <remarks> Can create or append to a menu that already exists in a menu bar. </remarks>
        internal MenuDisposable(ImU8String label)
        {
            this.Success = ImGui.BeginMenu(label);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="MenuDisposable"/> struct. </summary>
        /// <param name="label"> The label of the menu as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="enabled"> Whether the menu is enabled or not. </param>
        /// <returns> A disposable object that evaluates to true if the menu was created. Use with using. </returns>
        /// <remarks> Can create or append to a menu that already exists in a menu bar. </remarks>
        internal MenuDisposable(ImU8String label, bool enabled)
        {
            this.Success = ImGui.BeginMenu(label, enabled);
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(MenuDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(MenuDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(MenuDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(MenuDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(MenuDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
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

        /// <summary> End a menu without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndMenu();
    }
}

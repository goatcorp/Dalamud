// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui menu bars. </summary>
    public ref struct MenuBarDisposable : IDisposable
    {
        /// <summary> Whether creating the menu bar succeeded. </summary>
        public readonly bool Success;

        /// <summary> Whether the menu bar is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="MenuBarDisposable"/> struct. </summary>
        /// <returns> A disposable object that evaluates to true if the main menu bar was created. Use with using. </returns>
        /// <remarks> Can create or append to a window that has <seealso cref="WindowFlags.MenuBar"/> set. </remarks>
        public MenuBarDisposable()
        {
            Success = ImGui.BeginMenuBar();
            Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(MenuBarDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(MenuBarDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(MenuBarDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(MenuBarDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(MenuBarDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(MenuBarDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the menu bar on leaving scope. </summary>
        public void Dispose()
        {
            if (!Alive)
                return;

            if (Success)
                ImGui.EndMenuBar();
            Alive = false;
        }

        /// <summary> End a menu bar without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndMenuBar();
    }
}

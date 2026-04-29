using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui menu bars. </summary>
    public ref struct MenuBarDisposable : IDisposable
    {
        /// <summary> Whether creating the menu bar succeeded. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the menu bar is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="MenuBarDisposable"/> struct. </summary>
        /// <returns> A disposable object that evaluates to true if the main menu bar was created. Use with using. </returns>
        /// <remarks> Can create or append to a window that has <seealso cref="ImGuiWindowFlags.MenuBar"/> set. </remarks>
        public MenuBarDisposable()
        {
            this.Success = ImGui.BeginMenuBar();
            this.Alive   = true;
        }

        public static implicit operator bool(MenuBarDisposable value)
            => value.Success;

        public static bool operator true(MenuBarDisposable i)
            => i.Success;

        public static bool operator false(MenuBarDisposable i)
            => !i.Success;

        public static bool operator !(MenuBarDisposable i)
            => !i.Success;

        public static bool operator &(MenuBarDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(MenuBarDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the menu bar on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndMenuBar();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a menu bar without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndMenuBar();
#pragma warning restore SA1204
    }
}

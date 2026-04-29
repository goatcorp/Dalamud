using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui main menu bars. </summary>
    public ref struct MainMenuBarDisposable : IDisposable
    {
        /// <summary> Whether creating the main menu bar succeeded. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the main menu bar is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="MainMenuBarDisposable"/> struct. </summary>
        /// <returns> A disposable object that evaluates to true if the main menu bar was created. Use with using. </returns>
        /// <remarks> Can create or append to a full-screen menu bar at the top of the screen. </remarks>
        public MainMenuBarDisposable()
        {
            this.Success = ImGui.BeginMainMenuBar();
            this.Alive   = true;
        }

        public static implicit operator bool(MainMenuBarDisposable value)
            => value.Success;

        public static bool operator true(MainMenuBarDisposable i)
            => i.Success;

        public static bool operator false(MainMenuBarDisposable i)
            => !i.Success;

        public static bool operator !(MainMenuBarDisposable i)
            => !i.Success;

        public static bool operator &(MainMenuBarDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(MainMenuBarDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the main menu bar on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndMainMenuBar();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a main menu bar without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndMainMenuBar();
#pragma warning restore SA1204
    }
}

using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tab bars. </summary>
    public ref struct TabBarDisposable : IDisposable
    {
        /// <summary> Whether creating the tab bar succeeded. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the tab bar is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="TabBarDisposable"/> struct. </summary>
        /// <param name="label"> The tab bar label as text. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun tab bar is currently visible. Use with using. </returns>
        internal TabBarDisposable(ImU8String label)
        {
            this.Success = ImGui.BeginTabBar(label);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="TabBarDisposable"/> struct. </summary>
        /// <param name="label"> The tab bar label as text. </param>
        /// <param name="flags"> Additional flags to control the tab bar's behavior. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun tab bar is currently visible. Use with using. </returns>
        internal TabBarDisposable(ImU8String label, ImGuiTabBarFlags flags)
        {
            this.Success = ImGui.BeginTabBar(label, flags);
            this.Alive   = true;
        }

        public static implicit operator bool(TabBarDisposable value)
            => value.Success;

        public static bool operator true(TabBarDisposable i)
            => i.Success;

        public static bool operator false(TabBarDisposable i)
            => !i.Success;

        public static bool operator !(TabBarDisposable i)
            => !i.Success;

        public static bool operator &(TabBarDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(TabBarDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the tab bar on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndTabBar();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a tab bar without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndTabBar();
#pragma warning restore SA1204
    }
}

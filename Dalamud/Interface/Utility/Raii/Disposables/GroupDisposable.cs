using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui groups. </summary>
    public ref struct GroupDisposable : IDisposable
    {
        /// <summary> Gets a value indicating whether the group is still open. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="GroupDisposable"/> struct. </summary>
        /// <returns> A disposable object. Use with using. </returns>
        /// <remarks> Groups can be used to group multiple items together and treat them as a single item. </remarks>
        public GroupDisposable()
        {
            this.Alive = true;
            ImGui.BeginGroup();
        }

        /// <summary> End the group on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            ImGui.EndGroup();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a Group without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndGroup();
#pragma warning restore SA1204
    }
}

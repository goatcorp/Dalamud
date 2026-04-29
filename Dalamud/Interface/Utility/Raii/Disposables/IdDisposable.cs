using System.Runtime.CompilerServices;

using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ID pushing. </summary>
    public sealed class IdDisposable : IDisposable
    {
        /// <summary> Gets the number of IDs currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push a numerical ID to the ID stack and pop it on leaving scope. </summary>
        /// <param name="id"> The ID. </param>
        /// <param name="enabled"> If this is false, the id is not pushed. </param>
        /// <returns> A disposable object that counts the number of pushes and can be used to push further IDs. Use with using. </returns>
        /// <remarks> If you need to keep IDs pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        [OverloadResolutionPriority(100)]
        public IdDisposable Push(ImU8String id, bool enabled = true)
        {
            if (!enabled)
                return this;

            ++this.Count;
            ImGui.PushID(id);
            return this;
        }

        /// <inheritdoc cref="Push(ImU8String, bool)"/>
        public IdDisposable Push(int id, bool enabled = true)
        {
            if (!enabled)
                return this;

            ++this.Count;
            ImGui.PushID(id);
            return this;
        }

        /// <inheritdoc cref="Push(ImU8String, bool)"/>
        public unsafe IdDisposable Push(nint id, bool enabled = true)
        {
            if (!enabled)
                return this;

            ++this.Count;
            ImGui.PushID(id.ToPointer());
            return this;
        }

        /// <summary> Pop a number of IDs from the ID stack. </summary>
        /// <param name="count"> The number of IDs to pop. This is clamped to the number of IDs pushed by this object. </param>
        public void Pop(int count = 1)
        {
            if (count > this.Count)
                count = this.Count;
            this.Count -= count;
            while (count-- > 0)
                ImGui.PopID();
        }

        /// <summary> Pop all pushed IDs. </summary>
        public void Dispose()
            => this.Pop(this.Count);

#pragma warning disable SA1204
        /// <summary> Pop a number of IDs from the ID stack without using an IDisposable. </summary>
        /// <param name="num"> The number of item widths to pop. The number is not checked against the item width stack. </param>
        /// <remarks> Avoid using this function, and IDs across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
        {
            while (num-- > 0)
                ImGui.PopID();
        }
#pragma warning restore SA1204
    }
}

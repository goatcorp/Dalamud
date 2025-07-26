using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

// Push an arbitrary amount of ids into an object that are all popped when it is disposed.
// If condition is false, no id is pushed.
public static partial class ImRaii
{
    public static Id PushId(ImU8String id, bool enabled = true)
        => enabled ? new Id().Push(id) : new Id();

    public static Id PushId(int id, bool enabled = true)
        => enabled ? new Id().Push(id) : new Id();

    public static Id PushId(IntPtr id, bool enabled = true)
        => enabled ? new Id().Push(id) : new Id();

    public sealed class Id : IDisposable
    {
        private int count;

        public Id Push(ImU8String id, bool condition = true)
        {
            if (condition)
            {
                ImGui.PushID(id);
                ++this.count;
            }

            return this;
        }

        public Id Push(int id, bool condition = true)
        {
            if (condition)
            {
                ImGui.PushID(id);
                ++this.count;
            }

            return this;
        }

        public unsafe Id Push(IntPtr id, bool condition = true)
        {
            if (condition)
            {
                ImGui.PushID(id.ToPointer());
                ++this.count;
            }

            return this;
        }

        public void Pop(int num = 1)
        {
            num    =  Math.Min(num, this.count);
            this.count -= num;
            while (num-- > 0)
                ImGui.PopID();
        }

        public void Dispose()
            => this.Pop(this.count);
    }
}

#nullable disable

namespace Dalamud.Bindings.ImGui
{
    using System;

    public unsafe struct STBTexteditStatePtr : IEquatable<STBTexteditStatePtr>
    {
        public STBTexteditState* Handle;

        public unsafe STBTexteditStatePtr(STBTexteditState* handle)
        {
            Handle = handle;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is STBTexteditStatePtr ptr && Equals(ptr);
        }

        public readonly bool Equals(STBTexteditStatePtr other)
        {
            return Handle == other.Handle;
        }

        public override readonly int GetHashCode()
        {
            return ((nint)Handle).GetHashCode();
        }

        public static bool operator ==(STBTexteditStatePtr left, STBTexteditStatePtr right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(STBTexteditStatePtr left, STBTexteditStatePtr right)
        {
            return !(left == right);
        }

        public static implicit operator STBTexteditState*(STBTexteditStatePtr handle) => handle.Handle;

        public static implicit operator STBTexteditStatePtr(STBTexteditState* handle) => new(handle);
    }
}

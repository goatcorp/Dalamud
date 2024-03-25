using Dalamud.Utility;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Utility interface for implement event object instance pools.</summary>
public static class ControlEventArgsPool
{
    /// <summary>Rents an instance of the object.</summary>
    /// <returns>The rented object.</returns>
    /// <typeparam name="T">The type of the event.</typeparam>
    public static T Rent<T>() where T : SpannableControlEventArgs, new()
    {
        ThreadSafety.DebugAssertMainThread();
        foreach (ref var x in Storage<T>.ObjectPool.AsSpan())
        {
            if (x is not null)
            {
                var t = x;
                x = null;
                return t;
            }
        }

        return new();
    }

    /// <summary>Returns the object to the pool.</summary>
    /// <param name="value">The object being returnd.</param>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <remarks>Returning a <c>null</c> is a no-op.</remarks>
    public static void Return<T>(T? value) where T : SpannableControlEventArgs, new()
    {
        ThreadSafety.DebugAssertMainThread();
        if (value is null)
            return;
        foreach (ref var x in Storage<T>.ObjectPool.AsSpan())
        {
            if (x is null)
            {
                value.Sender = null!;
                x = value;
                return;
            }
        }
    }

    private static class Storage<T> where T : SpannableControlEventArgs, new()
    {
        public static readonly T?[] ObjectPool = new T?[16];
    }
}

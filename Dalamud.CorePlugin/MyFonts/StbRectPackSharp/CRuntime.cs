#pragma warning disable

// ReSharper disable all

using System;
using System.Runtime.InteropServices;

namespace Dalamud.CorePlugin.MyFonts;

internal static unsafe class CRuntime
{
    public static void* malloc(ulong size)
    {
        return malloc((long)size);
    }

    public static void* malloc(long size)
    {
        var ptr = Marshal.AllocHGlobal((int)size);

        return ptr.ToPointer();
    }

    public static void free(void* a)
    {
        var ptr = new IntPtr(a);
        Marshal.FreeHGlobal(ptr);
    }

    public delegate int QSortComparer(void* a, void* b);

    private static void qsortSwap(byte* data, long size, long pos1, long pos2)
    {
        var a = data + size * pos1;
        var b = data + size * pos2;

        for (long k = 0; k < size; ++k)
        {
            var tmp = *a;
            *a = *b;
            *b = tmp;

            a++;
            b++;
        }
    }

    private static long qsortPartition(byte* data, long size, QSortComparer comparer, long left, long right)
    {
        void* pivot = data + size * left;
        var i = left - 1;
        var j = right + 1;
        for (; ; )
        {
            do
            {
                ++i;
            } while (comparer(data + size * i, pivot) < 0);

            do
            {
                --j;
            } while (comparer(data + size * j, pivot) > 0);

            if (i >= j)
            {
                return j;
            }

            qsortSwap(data, size, i, j);
        }
    }


    private static void qsortInternal(byte* data, long size, QSortComparer comparer, long left, long right)
    {
        if (left < right)
        {
            var p = qsortPartition(data, size, comparer, left, right);

            qsortInternal(data, size, comparer, left, p);
            qsortInternal(data, size, comparer, p + 1, right);
        }
    }

    public static void qsort(void* data, ulong count, ulong size, QSortComparer comparer)
    {
        qsortInternal((byte*)data, (long)size, comparer, 0, (long)count - 1);
    }
}

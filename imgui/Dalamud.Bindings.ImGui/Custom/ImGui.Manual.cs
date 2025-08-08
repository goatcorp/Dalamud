using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bindings.ImGui;

public unsafe partial class ImGui
{
    public static Span<byte> DataTypeFormatString<T>(
        Span<byte> buf, ImGuiDataType dataType, T data, ImU8String format = default)
        where T : unmanaged, IBinaryNumber<T>
    {
        if (format.IsEmpty)
            format = GetFormatSpecifierU8(dataType);

        if (sizeof(T) != GetImGuiDataTypeSize(dataType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataType),
                dataType,
                $"Type indicated by {nameof(dataType)} does not match the type of {nameof(data)}.");
        }

        fixed (byte* bufPtr = buf)
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        {
            var len = ImGuiNative.DataTypeFormatString(bufPtr, buf.Length, dataType, &data, formatPtr);
            format.Recycle();
            return buf[..len];
        }
    }

    public static Span<byte> DataTypeFormatString<T>(Span<byte> buf, T data, ImU8String format = default)
        where T : unmanaged, IBinaryNumber<T> => DataTypeFormatString(buf, GetImGuiDataType<T>(), data, format);

    public static Span<byte> ImParseFormatTrimDecorations(ImU8String format, Span<byte> buf)
    {
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (byte* bufPtr = buf)
            ImGuiNative.ImParseFormatTrimDecorations(formatPtr, bufPtr, (nuint)buf.Length);
        format.Recycle();
        var nul = buf.IndexOf((byte)0);
        return nul == -1 ? buf : buf[..nul];
    }

    public static int ImTextStrFromUtf8(
        Span<char> outBuf, ReadOnlySpan<byte> inText, out ReadOnlySpan<byte> inRemaining)
    {
        fixed (char* outBufPtr = outBuf)
        fixed (byte* inTextPtr = inText)
        {
            byte* inRemainingPtr;
            var r = ImGuiNative.ImTextStrFromUtf8(
                (ushort*)outBufPtr,
                outBuf.Length,
                inTextPtr,
                inTextPtr + inText.Length,
                &inRemainingPtr);
            inRemaining = inText[(int)(inRemainingPtr - inTextPtr)..];
            return r;
        }
    }

    public static Span<byte> ImTextStrToUtf8(Span<byte> outBuf, ReadOnlySpan<char> inText)
    {
        fixed (byte* outBufPtr = outBuf)
        fixed (char* inTextPtr = inText)
        {
            return outBuf[..ImGuiNative.ImTextStrToUtf8(
                              outBufPtr,
                              outBuf.Length,
                              (ushort*)inTextPtr,
                              (ushort*)inTextPtr + inText.Length)];
        }
    }

    public delegate int ImGuiInputTextCallbackDelegate(scoped ref ImGuiInputTextCallbackData data);

    public delegate int ImGuiInputTextCallbackPtrDelegate(ImGuiInputTextCallbackDataPtr data);

    public delegate int ImGuiInputTextCallbackRefContextDelegate<TContext>(
        scoped ref ImGuiInputTextCallbackData data, scoped ref TContext context);

    public delegate int ImGuiInputTextCallbackInContextDelegate<TContext>(
        scoped ref ImGuiInputTextCallbackData data, scoped in TContext context);

    public static bool InputText(
        ImU8String label, Span<byte> buf, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        ImGuiInputTextCallbackDelegate? callback = null)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, default, buf, default, flags, callback);
    }

    public static bool InputText(
        ImU8String label, Span<byte> buf, ImGuiInputTextFlags flags, ImGuiInputTextCallbackPtrDelegate? callback)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, default, buf, default, flags, callback);
    }

    public static bool InputText<TContext>(
        ImU8String label, Span<byte> buf, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, default, buf, default, flags, callback, ref context);
    }

    public static bool InputText<TContext>(
        ImU8String label, Span<byte> buf, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, default, buf, default, flags, callback, in context);
    }

    public static bool InputText(
        ImU8String label, scoped ref string buf, int maxLength = ImU8String.AllocFreeBufferSize,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallbackDelegate? callback = null)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputText(label, t.Buffer[..(maxLength + 1)], flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputText(
        ImU8String label, scoped ref string buf, int maxLength, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackPtrDelegate? callback)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputText(label, t.Buffer[..(maxLength + 1)], flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputText<TContext>(
        ImU8String label, scoped ref string buf, int maxLength, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputText(label, t.Buffer[..(maxLength + 1)], flags, callback, ref context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputText<TContext>(
        ImU8String label, scoped ref string buf, int maxLength, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputText(label, t.Buffer[..(maxLength + 1)], flags, callback, in context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextEx(
        ImU8String label, ImU8String hint, Span<byte> buf, Vector2 sizeArg = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallbackDelegate? callback = null)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* hintPtr = &hint.GetPinnableNullTerminatedReference())
        fixed (byte* bufPtr = buf)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            var dataBuffer = PointerTuple.Create(&callback);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.InputTextEx(
                        labelPtr,
                        hintPtr,
                        bufPtr,
                        buf.Length,
                        sizeArg,
                        flags,
                        callback == null ? null : &InputTextCallbackStatic,
                        callback == null ? null : &dataBuffer) != 0;
            label.Recycle();
            hint.Recycle();
            return r;
        }
    }

    public static bool InputTextEx(
        ImU8String label, ImU8String hint, Span<byte> buf, Vector2 sizeArg,
        ImGuiInputTextFlags flags, ImGuiInputTextCallbackPtrDelegate? callback)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* hintPtr = &hint.GetPinnableNullTerminatedReference())
        fixed (byte* bufPtr = buf)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            var dataBuffer = PointerTuple.Create(&callback);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.InputTextEx(
                        labelPtr,
                        hintPtr,
                        bufPtr,
                        buf.Length,
                        sizeArg,
                        flags,
                        callback == null ? null : &InputTextCallbackPtrStatic,
                        callback == null ? null : &dataBuffer) != 0;
            label.Recycle();
            hint.Recycle();
            return r;
        }
    }

    public static bool InputTextEx<TContext>(
        ImU8String label, ImU8String hint, Span<byte> buf, Vector2 sizeArg, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* hintPtr = &hint.GetPinnableNullTerminatedReference())
        fixed (byte* bufPtr = buf)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            var dataBuffer = PointerTuple.Create(&callback, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.InputTextEx(
                        labelPtr,
                        hintPtr,
                        bufPtr,
                        buf.Length,
                        sizeArg,
                        flags,
                        &InputTextCallbackRefContextStatic,
                        &dataBuffer) != 0;
            label.Recycle();
            hint.Recycle();
            return r;
        }
    }

    public static bool InputTextEx<TContext>(
        ImU8String label, ImU8String hint, Span<byte> buf, Vector2 sizeArg, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* hintPtr = &hint.GetPinnableNullTerminatedReference())
        fixed (byte* bufPtr = buf)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            var dataBuffer = PointerTuple.Create(&callback, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.InputTextEx(
                        labelPtr,
                        hintPtr,
                        bufPtr,
                        buf.Length,
                        sizeArg,
                        flags,
                        &InputTextCallbackInContextStatic,
                        &dataBuffer) != 0;
            label.Recycle();
            hint.Recycle();
            return r;
        }
    }

    public static bool InputTextEx(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength = ImU8String.AllocFreeBufferSize,
        Vector2 sizeArg = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        ImGuiInputTextCallbackDelegate? callback = null)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextEx(label, hint, t.Buffer[..(maxLength + 1)], sizeArg, flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextEx(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength, Vector2 sizeArg,
        ImGuiInputTextFlags flags, ImGuiInputTextCallbackPtrDelegate? callback)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextEx(label, hint, t.Buffer[..(maxLength + 1)], sizeArg, flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextEx<TContext>(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength, Vector2 sizeArg,
        ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextEx(label, hint, t.Buffer[..(maxLength + 1)], sizeArg, flags, callback, ref context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextEx<TContext>(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength, Vector2 sizeArg,
        ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextEx(label, hint, t.Buffer[..(maxLength + 1)], sizeArg, flags, callback, in context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextMultiline(
        ImU8String label, Span<byte> buf, Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        ImGuiInputTextCallbackDelegate? callback = null) =>
        InputTextEx(
            label,
            default,
            buf,
            size,
            flags | (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline,
            callback);

    public static bool InputTextMultiline(
        ImU8String label, Span<byte> buf, Vector2 size, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackPtrDelegate? callback) =>
        InputTextEx(
            label,
            default,
            buf,
            size,
            flags | (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline,
            callback);

    public static bool InputTextMultiline<TContext>(
        ImU8String label, Span<byte> buf, Vector2 size, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context) =>
        InputTextEx(
            label,
            default,
            buf,
            size,
            flags | (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline,
            callback,
            ref context);

    public static bool InputTextMultiline<TContext>(
        ImU8String label, Span<byte> buf, Vector2 size, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context) =>
        InputTextEx(
            label,
            default,
            buf,
            size,
            flags | (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline,
            callback,
            in context);

    public static bool InputTextMultiline(
        ImU8String label, scoped ref string buf, int maxLength = ImU8String.AllocFreeBufferSize, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        ImGuiInputTextCallbackDelegate? callback = null)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextMultiline(label, t.Buffer[..(maxLength + 1)], size, flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextMultiline(
        ImU8String label, scoped ref string buf, int maxLength, Vector2 size, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackPtrDelegate? callback)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextMultiline(label, t.Buffer[..(maxLength + 1)], size, flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextMultiline<TContext>(
        ImU8String label, scoped ref string buf, int maxLength, Vector2 size, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextMultiline(label, t.Buffer[..(maxLength + 1)], size, flags, callback, ref context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextMultiline<TContext>(
        ImU8String label, scoped ref string buf, int maxLength, Vector2 size, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextMultiline(label, t.Buffer[..(maxLength + 1)], size, flags, callback, in context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextWithHint(
        ImU8String label, ImU8String hint, Span<byte> buf, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        ImGuiInputTextCallbackDelegate? callback = null)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, hint, buf, default, flags, callback);
    }

    public static bool InputTextWithHint(
        ImU8String label, ImU8String hint, Span<byte> buf, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackPtrDelegate? callback)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, hint, buf, default, flags, callback);
    }

    public static bool InputTextWithHint<TContext>(
        ImU8String label, ImU8String hint, Span<byte> buf, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, hint, buf, default, flags, callback, ref context);
    }

    public static bool InputTextWithHint<TContext>(
        ImU8String label, ImU8String hint, Span<byte> buf, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context)
    {
        if ((flags & (ImGuiInputTextFlags)ImGuiInputTextFlagsPrivate.Multiline) != ImGuiInputTextFlags.None)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "Multiline must not be set");
        return InputTextEx(label, hint, buf, default, flags, callback, in context);
    }

    public static bool InputTextWithHint(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength = ImU8String.AllocFreeBufferSize,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        ImGuiInputTextCallbackDelegate? callback = null)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextWithHint(label, hint, t.Buffer[..(maxLength + 1)], flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextWithHint(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackPtrDelegate? callback)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextWithHint(label, hint, t.Buffer[..(maxLength + 1)], flags, callback);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextWithHint<TContext>(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackRefContextDelegate<TContext> callback, scoped ref TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextWithHint(label, hint, t.Buffer[..(maxLength + 1)], flags, callback, ref context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool InputTextWithHint<TContext>(
        ImU8String label, ImU8String hint, scoped ref string buf, int maxLength, ImGuiInputTextFlags flags,
        ImGuiInputTextCallbackInContextDelegate<TContext> callback, scoped in TContext context)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = InputTextWithHint(label, hint, t.Buffer[..(maxLength + 1)], flags, callback, in context);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    public static bool TempInputText(
        ImRect bb, uint id, ImU8String label, Span<byte> buf, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* bufPtr = buf)
        {
            var r = ImGuiNative.TempInputText(bb, id, labelPtr, bufPtr, buf.Length, flags) != 0;
            label.Recycle();
            return r;
        }
    }

    public static bool TempInputText(
        ImRect bb, uint id, ImU8String label, scoped ref string buf, int maxLength = ImU8String.AllocFreeBufferSize,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        var t = new ImU8String(buf);
        t.Reserve(maxLength + 1);
        var r = TempInputText(bb, id, label, t.Buffer[..(maxLength + 1)], flags);
        var i = t.Buffer.IndexOf((byte)0);
        buf = Encoding.UTF8.GetString(i == -1 ? t.Buffer : t.Buffer[..i]);
        t.Recycle();
        return r;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int InputTextCallbackStatic(ImGuiInputTextCallbackData* data)
    {
        ref var dvps = ref PointerTuple.From<ImGuiInputTextCallbackDelegate>(data->UserData);
        return dvps.Item1.Invoke(ref *data);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int InputTextCallbackPtrStatic(ImGuiInputTextCallbackData* data)
    {
        ref var dvps = ref PointerTuple.From<ImGuiInputTextCallbackPtrDelegate>(data->UserData);
        return dvps.Item1.Invoke(data);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int InputTextCallbackRefContextStatic(ImGuiInputTextCallbackData* data)
    {
        ref var dvps = ref PointerTuple.From<ImGuiInputTextCallbackRefContextDelegate<object>, object>(data->UserData);
        return dvps.Item1.Invoke(ref *data, ref dvps.Item2);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int InputTextCallbackInContextStatic(ImGuiInputTextCallbackData* data)
    {
        ref var dvps = ref PointerTuple.From<ImGuiInputTextCallbackInContextDelegate<object>, object>(data->UserData);
        return dvps.Item1.Invoke(ref *data, in dvps.Item2);
    }
}

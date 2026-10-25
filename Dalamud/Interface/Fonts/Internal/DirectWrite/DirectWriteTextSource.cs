using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>An <see cref="IDWriteTextAnalysisSource"/> over UTF-16 text, left to right, with one locale.</summary>
/// <remarks>Lives on the stack for the duration of one call.</remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DirectWriteTextSource
{
    private static readonly void** Vtbl = CreateVtbl();

    private void** vtbl;
    private char* text;
    private uint length;
    private char* localeName;

    /// <summary>Initializes a new instance of the <see cref="DirectWriteTextSource"/> struct.</summary>
    /// <param name="text">The text, which must stay valid while the source is used.</param>
    /// <param name="length">Length of the text in code units.</param>
    /// <param name="localeName">Locale name, zero-terminated.</param>
    public DirectWriteTextSource(char* text, uint length, char* localeName)
    {
        this.vtbl = Vtbl;
        this.text = text;
        this.length = length;
        this.localeName = localeName;
    }

    private static void** CreateVtbl()
    {
        var v = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DirectWriteTextSource), sizeof(void*) * 8);
        Debug.Assert(v is not null, "AllocateTypeAssociatedMemory throws rather than returning null.");
        v[0] = (delegate* unmanaged<DirectWriteTextSource*, Guid*, void**, int>)&QueryInterface;
        v[1] = (delegate* unmanaged<DirectWriteTextSource*, uint>)&AddRefOrRelease;
        v[2] = (delegate* unmanaged<DirectWriteTextSource*, uint>)&AddRefOrRelease;
        v[3] = (delegate* unmanaged<DirectWriteTextSource*, uint, char**, uint*, int>)&GetTextAtPosition;
        v[4] = (delegate* unmanaged<DirectWriteTextSource*, uint, char**, uint*, int>)&GetTextBeforePosition;
        v[5] = (delegate* unmanaged<DirectWriteTextSource*, DWRITE_READING_DIRECTION>)&GetParagraphReadingDirection;
        v[6] = (delegate* unmanaged<DirectWriteTextSource*, uint, uint*, char**, int>)&GetLocaleName;
        v[7] = (delegate* unmanaged<DirectWriteTextSource*, uint, uint*, void**, int>)&GetNumberSubstitution;
        return v;
    }

    [UnmanagedCallersOnly]
    private static int QueryInterface(DirectWriteTextSource* self, Guid* iid, void** obj)
    {
        if (*iid == IID.IID_IUnknown || *iid == IID.IID_IDWriteTextAnalysisSource)
        {
            *obj = self;
            return 0;
        }

        *obj = null;
        return E.E_NOINTERFACE;
    }

    // Not reference counted, as it lives on the stack.
    [UnmanagedCallersOnly]
    private static uint AddRefOrRelease(DirectWriteTextSource* self) => 1;

    [UnmanagedCallersOnly]
    private static int GetTextAtPosition(DirectWriteTextSource* self, uint position, char** text, uint* length)
    {
        *text = position < self->length ? self->text + position : null;
        *length = position < self->length ? self->length - position : 0;
        return 0;
    }

    [UnmanagedCallersOnly]
    private static int GetTextBeforePosition(DirectWriteTextSource* self, uint position, char** text, uint* length)
    {
        *text = position > 0 && position <= self->length ? self->text : null;
        *length = position > 0 && position <= self->length ? position : 0;
        return 0;
    }

    [UnmanagedCallersOnly]
    private static DWRITE_READING_DIRECTION GetParagraphReadingDirection(DirectWriteTextSource* self) =>
        DWRITE_READING_DIRECTION.DWRITE_READING_DIRECTION_LEFT_TO_RIGHT;

    [UnmanagedCallersOnly]
    private static int GetLocaleName(DirectWriteTextSource* self, uint position, uint* length, char** localeName)
    {
        *length = position < self->length ? self->length - position : 0;
        *localeName = self->localeName;
        return 0;
    }

    [UnmanagedCallersOnly]
    private static int GetNumberSubstitution(
        DirectWriteTextSource* self, uint position, uint* length, void** substitution)
    {
        *length = position < self->length ? self->length - position : 0;
        *substitution = null;
        return 0;
    }
}

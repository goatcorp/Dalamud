using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility;

/// <summary>Wrapper aroundx <see cref="ImDrawData"/> containing one <see cref="ImDrawList"/>.</summary>
public unsafe struct BufferBackedImDrawData : IDisposable
{
    private nint buffer;

    /// <summary>Initializes a new instance of the <see cref="BufferBackedImDrawData"/> struct.</summary>
    /// <param name="buffer">Address of buffer to use.</param>
    private BufferBackedImDrawData(nint buffer) => this.buffer = buffer;

    /// <summary>Gets the <see cref="ImDrawData"/> stored in this buffer.</summary>
    public readonly ref ImDrawData Data => ref ((DataStruct*)this.buffer)->Data;

    /// <summary>Gets the <see cref="ImDrawDataPtr"/> stored in this buffer.</summary>
    public readonly ImDrawDataPtr DataPtr => new((ImDrawData*)Unsafe.AsPointer(ref this.Data));

    /// <summary>Gets the <see cref="ImDrawList"/> stored in this buffer.</summary>
    public readonly ref ImDrawList List => ref ((DataStruct*)this.buffer)->List;

    /// <summary>Gets the <see cref="ImDrawListPtr"/> stored in this buffer.</summary>
    public readonly ImDrawListPtr ListPtr => new((ImDrawList*)Unsafe.AsPointer(ref this.List));

    /// <summary>Creates a new instance of <see cref="BufferBackedImDrawData"/>.</summary>
    /// <returns>A new instance of <see cref="BufferBackedImDrawData"/>.</returns>
    public static BufferBackedImDrawData Create()
    {
        if (ImGui.GetCurrentContext().IsNull || ImGui.GetIO().FontDefault.Handle is null)
            throw new("ImGui is not ready");

        var res = new BufferBackedImDrawData(Marshal.AllocHGlobal(sizeof(DataStruct)));
        var ds = (DataStruct*)res.buffer;
        *ds = default;

        var atlas = ImGui.GetIO().Fonts;
        ds->SharedData = *ImGui.GetDrawListSharedData().Handle;
        ds->SharedData.TexIdCommon = atlas.Textures[atlas.TextureIndexCommon].TexID;
        ds->SharedData.TexUvWhitePixel = atlas.TexUvWhitePixel;
        ds->SharedData.TexUvLines = (Vector4*)Unsafe.AsPointer(ref atlas.TexUvLines[0]);
        ds->SharedData.Font = ImGui.GetIO().FontDefault;
        ds->SharedData.FontSize = ds->SharedData.Font->FontSize;
        ds->SharedData.ClipRectFullscreen = new(
            float.NegativeInfinity,
            float.NegativeInfinity,
            float.PositiveInfinity,
            float.PositiveInfinity);

        ds->List.Data = &ds->SharedData;
        ds->ListPtr = &ds->List;
        ds->Data.CmdLists = &ds->ListPtr;
        ds->Data.CmdListsCount = 1;
        ds->Data.FramebufferScale = Vector2.One;

        res.ListPtr._ResetForNewFrame();
        res.ListPtr.PushClipRectFullScreen();
        res.ListPtr.PushTextureID(new(atlas.TextureIndexCommon));
        return res;
    }

    /// <summary>Updates the statistics information stored in <see cref="DataPtr"/> from <see cref="ListPtr"/>.</summary>
    public readonly void UpdateDrawDataStatistics()
    {
        this.Data.TotalIdxCount = this.List.IdxBuffer.Size;
        this.Data.TotalVtxCount = this.List.VtxBuffer.Size;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.buffer != 0)
        {
            this.ListPtr._ClearFreeMemory();
            Marshal.FreeHGlobal(this.buffer);
            this.buffer = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataStruct
    {
        public ImDrawData Data;
        public ImDrawList* ListPtr;
        public ImDrawList List;
        public ImDrawListSharedData SharedData;
    }
}

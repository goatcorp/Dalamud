using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

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
        if (ImGui.GetCurrentContext() == 0 || ImGui.GetIO().FontDefault.NativePtr is null)
            throw new("ImGui is not ready");

        var res = new BufferBackedImDrawData(Marshal.AllocHGlobal(sizeof(DataStruct)));
        var ds = (DataStruct*)res.buffer;
        *ds = default;

        var atlas = ImGui.GetIO().Fonts;
        ref var atlasTail = ref ImFontAtlasTailReal.From(atlas);
        ds->SharedData = *(ImDrawListSharedData*)ImGui.GetDrawListSharedData();
        ds->SharedData.TexIdCommon = atlas.Textures[atlasTail.TextureIndexCommon].TexID;
        ds->SharedData.TexUvWhitePixel = atlas.TexUvWhitePixel;
        ds->SharedData.TexUvLines = (Vector4*)atlas.TexUvLines.Data;
        ds->SharedData.Font = ImGui.GetIO().FontDefault;
        ds->SharedData.FontSize = ds->SharedData.Font->FontSize;
        ds->SharedData.ClipRectFullscreen = new(
            float.NegativeInfinity,
            float.NegativeInfinity,
            float.PositiveInfinity,
            float.PositiveInfinity);

        ds->List._Data = (nint)(&ds->SharedData);
        ds->ListPtr = &ds->List;
        ds->Data.CmdLists = &ds->ListPtr;
        ds->Data.CmdListsCount = 1;
        ds->Data.FramebufferScale = Vector2.One;

        res.ListPtr._ResetForNewFrame();
        res.ListPtr.PushClipRectFullScreen();
        res.ListPtr.PushTextureID((nint)atlasTail.TextureIndexCommon);
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

    [StructLayout(LayoutKind.Sequential)]
    private struct ImFontAtlasTailReal
    {
        /// <summary>Index of texture containing the below.</summary>
        public int TextureIndexCommon;

        /// <summary>Custom texture rectangle ID for both of the below.</summary>
        public int PackIdCommon;

        /// <summary>Custom texture rectangle for white pixel and mouse cursors.</summary>
        public ImFontAtlasCustomRect RectMouseCursors;

        /// <summary>Custom texture rectangle for baked anti-aliased lines.</summary>
        public ImFontAtlasCustomRect RectLines;

        public static ref ImFontAtlasTailReal From(ImFontAtlasPtr fontAtlasPtr) =>
            ref *(ImFontAtlasTailReal*)(&fontAtlasPtr.NativePtr->FontBuilderFlags + sizeof(uint));
    }

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1306:Field names should begin with lower-case letter",
        Justification = "ImGui Internals")]
    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1202:Elements should be ordered by access",
        Justification = "ImGui Internals")]
    private struct ImDrawListSharedData
    {
        private const int ImDrawListArcFastTableSize = 48;
        private const int ImDrawListCircleSegmentCountsSize = 64;

        /// <summary>Texture ID for white pixel and anti-aliased lines.</summary>
        public nint TexIdCommon;

        /// <summary>UV of white pixel in the atlas.</summary>
        public Vector2 TexUvWhitePixel;

        /// <summary>Current/default font (optional, for simplified AddText overload).</summary>
        public ImFont* Font;

        /// <summary>Current/default font size (optional, for simplified AddText overload).</summary>
        public float FontSize;

        /// <summary>Tessellation tolerance when using PathBezierCurveTo().</summary>
        public float CurveTessellationTol;

        /// <summary>Number of circle segments to use per pixel of radius for AddCircle() etc.</summary>
        public float CircleSegmentMaxError;

        /// <summary>Value for PushClipRectFullscreen().</summary>
        public Vector4 ClipRectFullscreen;

        /// <summary>Initial flags at the beginning of the frame (it is possible to alter flags on a per-drawlist basis afterward).</summary>
        public ImDrawListFlags InitialFlags;

        /// <summary>Sample points on the quarter of the circle.</summary>
        private fixed float ArcFastVtxBuffer[2 * ImDrawListArcFastTableSize];

        /// <summary>Cutoff radius after which arc drawing will fall back to slower PathArcTo().</summary>
        public float ArcFastRadiusCutoff;

        /// <summary>Precomputed segment count for given radius before we calculate it dynamically (to avoid calculation overhead).</summary>
        public fixed byte CircleSegmentCounts[ImDrawListCircleSegmentCountsSize];

        /// <summary>UV of anti-aliased lines in the atlas.</summary>
        public Vector4* TexUvLines;

        /// <inheritdoc cref="ArcFastVtxBuffer"/>
        public Span<Vector2> ArcFastVtx =>
            MemoryMarshal.Cast<float, Vector2>(
                MemoryMarshal.CreateSpan(ref this.ArcFastVtxBuffer[0], 2 * ImDrawListArcFastTableSize));
    }
}

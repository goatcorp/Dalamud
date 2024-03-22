using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Rendering;

/// <summary>
/// An initial render context.
/// </summary>
public readonly struct RenderContext
{
    /// <summary>The resolved ImGui global ID, or 0 if no ImGui state management is used.</summary>
    public readonly uint ImGuiGlobalId;

    /// <summary>Gets or sets the scale.</summary>
    /// <remarks>
    /// <para>Defaults to <see cref="ImGuiHelpers.GlobalScale"/>.</para>
    /// <para>Scale specified here will be referred to when a font is loaded. The scale specified from
    /// <see cref="Transformation"/> only affects drawing.</para>
    /// </remarks>
    public readonly float Scale;

    /// <summary>The target draw list. Can be null.</summary>
    public readonly ImDrawListPtr DrawListPtr;

    /// <summary>The location of the mouse in screen coordinates.</summary>
    public readonly Vector2 MouseScreenLocation;

    /// <summary>Gets or sets the maximum size at which point line break or ellipsis should happen.</summary>
    /// <remarks>Default value is <c>new Vector2(ImGui.GetColumnWidth(), float.MaxValue)</c>.</remarks>
    public readonly Vector2 MaxSize;
    
    /// <inheritdoc cref="ISpannableState.ScreenOffset"/>
    public readonly Vector2 ScreenOffset;
    
    /// <inheritdoc cref="ISpannableState.TransformationOrigin"/>
    /// <remarks>Default value is <c>(0.5, 0.5)</c>.</remarks>
    public readonly Vector2 TransformationOrigin;

    /// <inheritdoc cref="ISpannableState.Transformation"/>
    /// <remarks>Default value is <see cref="Matrix4x4.Identity"/>.</remarks>
    public readonly Matrix4x4 Transformation;

    /// <summary>Whether to put a dummy after rendering.</summary>
    public readonly bool PutDummyAfterRender;

    /// <summary>Gets or sets a value indicating whether to handle links.</summary>
    /// <remarks>Default value is to enable link handling. Will never be set to <c>true</c> if <see cref="DrawListPtr"/>
    /// is empty.</remarks>
    public readonly bool UseLinks;

    /// <summary>Initializes a new instance of the <see cref="RenderContext"/> struct.</summary>
    /// <param name="imGuiLabel">The ImGui label in UTF-16 for tracking interaction state.</param>
    /// <param name="options">The options.</param>
    public unsafe RenderContext(ReadOnlySpan<char> imGuiLabel, in Options options = default)
        : this(ImGui.GetWindowDrawList(), true, options)
    {
        if (imGuiLabel.IsEmpty)
            return;

        Span<byte> buf = stackalloc byte[Encoding.UTF8.GetByteCount(imGuiLabel)];
        Encoding.UTF8.GetBytes(imGuiLabel, buf);
        fixed (byte* p = buf)
            this.ImGuiGlobalId = ImGuiNative.igGetID_StrStr(p, p + buf.Length);
    }

    /// <summary>Initializes a new instance of the <see cref="RenderContext"/> struct.</summary>
    /// <param name="imGuiLabel">The ImGui label in UTF-8 for tracking interaction state.</param>
    /// <param name="options">The options.</param>
    public unsafe RenderContext(ReadOnlySpan<byte> imGuiLabel, in Options options = default)
        : this(ImGui.GetWindowDrawList(), true, options)
    {
        if (imGuiLabel.IsEmpty)
            return;

        fixed (byte* p = imGuiLabel)
            this.ImGuiGlobalId = ImGuiNative.igGetID_StrStr(p, p + imGuiLabel.Length);
    }

    /// <summary>Initializes a new instance of the <see cref="RenderContext"/> struct.</summary>
    /// <param name="imGuiId">The numeric ImGui ID.</param>
    /// <param name="options">The options.</param>
    public unsafe RenderContext(nint imGuiId, in Options options = default)
        : this(ImGui.GetWindowDrawList(), true, options)
    {
        if (imGuiId == nint.Zero)
            return;

        this.ImGuiGlobalId = ImGuiNative.igGetID_Ptr((void*)imGuiId);
    }

    /// <summary>Initializes a new instance of the <see cref="RenderContext"/> struct.</summary>
    /// <param name="measureOnly">Whether to only do a measure pass, without drawing.</param>
    /// <param name="options">The options.</param>
    public RenderContext(bool measureOnly, in Options options = default)
        : this(ImGui.GetWindowDrawList(), !measureOnly, options)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RenderContext"/> struct.</summary>
    /// <param name="drawListPtr">The pointer to the draw list.</param>
    /// <param name="options">The options.</param>
    public RenderContext(ImDrawListPtr drawListPtr, in Options options = default)
        : this(drawListPtr, false, options)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RenderContext"/> struct.</summary>
    /// <param name="drawListPtr">The target draw list.</param>
    /// <param name="putDummyAfterRender">Whether to put a dummy after render.</param>
    /// <param name="options">The options.</param>
    /// <returns>A reference of this instance after the initialize operation is completed.</returns>
    /// <exception cref="InvalidOperationException">Called outside the main thread. If called from the main thread,
    /// but not during the drawing context, the behavior is undefined and may crash.</exception>
    private RenderContext(ImDrawListPtr drawListPtr, bool putDummyAfterRender, in Options options)
    {
        ThreadSafety.DebugAssertMainThread();

        this.UseLinks = options.UseLinks ?? true;
        this.MaxSize = options.MaxSize ?? new(ImGui.GetColumnWidth(), float.MaxValue);
        this.Scale = options.Scale ?? ImGuiHelpers.GlobalScale;
        this.DrawListPtr = drawListPtr;
        this.PutDummyAfterRender = putDummyAfterRender;
        this.MouseScreenLocation = ImGui.GetMousePos();
        this.ScreenOffset = options.ScreenOffset ?? ImGui.GetCursorScreenPos();
        this.TransformationOrigin = options.TransformationOrigin ?? new(0.5f);
        this.Transformation = options.Transformation ?? Matrix4x4.Identity;

        this.UseLinks &= this.UseDrawing;
    }

    /// <summary>Gets a value indicating whether to actually draw.</summary>
    public unsafe bool UseDrawing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.DrawListPtr.NativePtr is not null;
    }

    /// <summary>The initial options that may be set to <c>null</c> to use the default values.</summary>
    public struct Options
    {
        /// <inheritdoc cref="RenderContext.UseLinks"/>
        public bool? UseLinks { get; set; }

        /// <inheritdoc cref="RenderContext.Scale"/>
        public float? Scale { get; set; }

        /// <inheritdoc cref="RenderContext.MaxSize"/>
        public Vector2? MaxSize { get; set; }

        /// <inheritdoc cref="RenderContext.ScreenOffset"/>
        public Vector2? ScreenOffset { get; set; }

        /// <inheritdoc cref="RenderContext.TransformationOrigin"/>
        public Vector2? TransformationOrigin { get; set; }

        /// <inheritdoc cref="RenderContext.Transformation"/>
        public Matrix4x4? Transformation { get; set; }
    }
}

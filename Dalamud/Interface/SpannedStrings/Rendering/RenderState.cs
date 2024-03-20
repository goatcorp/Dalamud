using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Game.Config;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Rendering.Internal;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Rendering;

/// <summary>Represents a render state.</summary>
public struct RenderState
{
    /// <inheritdoc cref="RenderOptions.UseLinks"/>
    public readonly bool UseLinks;

    /// <summary>Whether to show representations of control characters.</summary>
    public readonly bool UseControlCharacter;

    /// <summary>Whether to show wrap markers.</summary>
    public readonly bool UseWrapMarker;

    /// <summary>Whether <see cref="WrapMarkerStyle"/> is valid.</summary>
    public readonly bool UseWrapMarkerParams;

    /// <summary>Whether to put a dummy after rendering.</summary>
    public readonly bool PutDummyAfterRender;

    /// <inheritdoc cref="RenderOptions.WordBreak"/>
    public readonly WordBreakType WordBreak;

    /// <inheritdoc cref="RenderOptions.AcceptedNewLines"/>
    public readonly NewLineType AcceptedNewLines;

    /// <summary>The resolved ImGui global ID, or 0 if no ImGui state management is used.</summary>
    public readonly uint ImGuiGlobalId;

    /// <inheritdoc cref="RenderOptions.TabWidth"/>
    public readonly float TabWidth;

    /// <inheritdoc cref="RenderOptions.Scale"/>
    public readonly float Scale;

    /// <inheritdoc cref="RenderOptions.LineWrapWidth"/>
    public readonly float LineWrapWidth;

    /// <summary>The first drawing screen offset.</summary>
    /// <remarks>This is an offset obtained from <see cref="ImGui.GetCursorScreenPos"/>.</remarks>
    public readonly Vector2 StartScreenOffset;

    /// <inheritdoc cref="RenderOptions.WrapMarker"/>
    public readonly string WrapMarker;

    /// <summary>The target draw list. Can be null.</summary>
    public readonly ImDrawListPtr DrawListPtr;

    /// <inheritdoc cref="RenderOptions.GraphicFontIconMode"/>
    public readonly int GfdIndex;

    /// <inheritdoc cref="RenderOptions.Transformation"/>
    public readonly Matrix4x4 Transformation;

    /// <summary>Inverse of <see cref="Transformation"/>.</summary>
    public readonly Matrix4x4 TransformationInverse;

    /// <inheritdoc cref="RenderOptions.ControlCharactersSpanParams"/>
    public readonly SpanStyle ControlCharactersSpanStyle;

    /// <inheritdoc cref="RenderOptions.InitialStyle"/>
    public readonly SpanStyle InitialSpanStyle;

    /// <inheritdoc cref="RenderOptions.WrapMarkerStyle"/>
    public readonly SpanStyle WrapMarkerStyle;

    /// <summary>The latest style.</summary>
    public SpanStyle LastStyle;

    /// <summary>The index of the last line, including new lines from word wrapping.</summary>
    public int LastLineIndex;

    /// <summary>The final drawing relative offset, pre-transformed value.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public Vector2 Offset;

    /// <summary>The left top relative offset of the text rendered so far, pre-transformed value.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public Vector2 BoundsLeftTop;

    /// <summary>The right bottom relative offset of the text rendered so far, pre-transformed value.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public Vector2 BoundsRightBottom;

    /// <summary>The mouse button that has been clicked.</summary>
    /// <remarks>As <c>0</c> is <see cref="ImGuiMouseButton.Left"/>, if no mouse button is detected clicked, then it
    /// will be set to an invalid enum value.</remarks>
    public ImGuiMouseButton ClickedMouseButton;

    /// <summary>The latest measurement.</summary>
    internal MeasuredLine LastMeasurement;

    /// <summary>Initializes a new instance of the <see cref="RenderState"/> struct.</summary>
    /// <param name="imGuiLabel">The ImGui label in UTF-16 for tracking interaction state.</param>
    /// <param name="options">The options.</param>
    public unsafe RenderState(ReadOnlySpan<char> imGuiLabel, in RenderOptions options = default)
        : this(ImGui.GetWindowDrawList(), true, options)
    {
        if (imGuiLabel.IsEmpty)
        {
            this.UseLinks = false;
            return;
        }

        Span<byte> buf = stackalloc byte[Encoding.UTF8.GetByteCount(imGuiLabel)];
        Encoding.UTF8.GetBytes(imGuiLabel, buf);
        fixed (byte* p = buf)
            this.ImGuiGlobalId = ImGuiNative.igGetID_StrStr(p, p + buf.Length);
    }

    /// <summary>Initializes a new instance of the <see cref="RenderState"/> struct.</summary>
    /// <param name="imGuiLabel">The ImGui label in UTF-8 for tracking interaction state.</param>
    /// <param name="options">The options.</param>
    public unsafe RenderState(ReadOnlySpan<byte> imGuiLabel, in RenderOptions options = default)
        : this(ImGui.GetWindowDrawList(), true, options)
    {
        if (imGuiLabel.IsEmpty)
        {
            this.UseLinks = false;
            return;
        }

        fixed (byte* p = imGuiLabel)
            this.ImGuiGlobalId = ImGuiNative.igGetID_StrStr(p, p + imGuiLabel.Length);
    }

    /// <summary>Initializes a new instance of the <see cref="RenderState"/> struct.</summary>
    /// <param name="imGuiId">The numeric ImGui ID.</param>
    /// <param name="options">The options.</param>
    public unsafe RenderState(nint imGuiId, in RenderOptions options = default)
        : this(ImGui.GetWindowDrawList(), true, options)
    {
        if (imGuiId == nint.Zero)
        {
            this.UseLinks = false;
            return;
        }

        this.ImGuiGlobalId = ImGuiNative.igGetID_Ptr((void*)imGuiId);
    }

    /// <summary>Initializes a new instance of the <see cref="RenderState"/> struct.</summary>
    /// <param name="measureOnly">Whether to only do a measure pass, without drawing.</param>
    /// <param name="options">The options.</param>
    public RenderState(bool measureOnly, in RenderOptions options = default)
        : this(ImGui.GetWindowDrawList(), !measureOnly, options)
    {
        this.UseLinks = false;
    }

    /// <summary>Initializes a new instance of the <see cref="RenderState"/> struct.</summary>
    /// <param name="drawListPtr">The pointer to the draw list.</param>
    /// <param name="options">The options.</param>
    public RenderState(ImDrawListPtr drawListPtr, in RenderOptions options = default)
        : this(drawListPtr, false, options)
    {
        this.UseLinks = false;
    }

    /// <summary>Initializes a new instance of the <see cref="RenderState"/> struct.</summary>
    /// <param name="drawListPtr">The target draw list.</param>
    /// <param name="putDummyAfterRender">Whether to put a dummy after render.</param>
    /// <param name="rendererOptions">the initial parameters.</param>
    /// <returns>A reference of this instance after the initialize operation is completed.</returns>
    private RenderState(ImDrawListPtr drawListPtr, bool putDummyAfterRender, in RenderOptions rendererOptions)
    {
        ThreadSafety.DebugAssertMainThread();

        this.UseLinks = rendererOptions.UseLinks ?? true;
        this.UseControlCharacter = rendererOptions.ControlCharactersSpanParams.HasValue;
        this.UseWrapMarker = !string.IsNullOrEmpty(rendererOptions.WrapMarker);
        this.UseWrapMarkerParams = rendererOptions.WrapMarkerStyle is not null;
        this.PutDummyAfterRender = putDummyAfterRender;
        this.WordBreak = rendererOptions.WordBreak ?? WordBreakType.Normal;
        this.AcceptedNewLines = rendererOptions.AcceptedNewLines ?? NewLineType.All;
        this.TabWidth = rendererOptions.TabWidth ?? -4;
        this.Scale = rendererOptions.Scale ?? ImGuiHelpers.GlobalScale;
        this.LineWrapWidth = rendererOptions.LineWrapWidth ?? ImGui.GetColumnWidth();
        this.Transformation = rendererOptions.Transformation ?? Matrix4x4.Identity;
        this.ControlCharactersSpanStyle = rendererOptions.ControlCharactersSpanParams ?? default;
        this.InitialSpanStyle = rendererOptions.InitialStyle ?? SpanStyle.FromContext;
        this.WrapMarker = rendererOptions.WrapMarker ?? string.Empty;
        this.WrapMarkerStyle = rendererOptions.WrapMarkerStyle ?? default;
        this.DrawListPtr = drawListPtr;

        if (!Matrix4x4.Invert(this.Transformation, out this.TransformationInverse))
            this.TransformationInverse = Matrix4x4.Identity;

        var gfdIndex = rendererOptions.GraphicFontIconMode ?? -1;
        if (gfdIndex < 0 || gfdIndex >= SpannableRenderer.GfdTexturePaths.Length)
        {
            gfdIndex =
                Service<GameConfig>.Get().TryGet(SystemConfigOption.PadSelectButtonIcon, out uint iconTmp)
                    ? (int)iconTmp
                    : 0;
        }

        this.GfdIndex = gfdIndex;
        this.StartScreenOffset = ImGui.GetCursorScreenPos();
        this.Offset = Vector2.Zero;
        this.BoundsLeftTop = new(float.MaxValue);
        this.BoundsRightBottom = new(float.MinValue);
        this.LastLineIndex = 0;
        this.ClickedMouseButton = unchecked((ImGuiMouseButton)(-1));
        this.LastStyle = this.InitialSpanStyle;
        this.LastMeasurement = default;
    }

    /// <summary>Gets a value indicating whether to actually draw.</summary>
    public unsafe bool UseDrawing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.DrawListPtr.NativePtr is not null;
    }

    /// <summary>Transforms the given coordinates according to the specified transformation.</summary>
    /// <param name="coord">The coordinates.</param>
    /// <returns>The transformed coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 Transform(Vector2 coord) => Vector2.Transform(coord, this.Transformation);

    /// <summary>Reverse transforms the given coordinates according to the specified transformation.</summary>
    /// <param name="coord">The coordinates.</param>
    /// <returns>The reverse transformed coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 TransformInverse(Vector2 coord) => Vector2.Transform(coord, this.TransformationInverse);
}

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Game.Config;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Rendering.Internal;
using Dalamud.Interface.SpannedStrings.Spannables;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Rendering;

/// <summary>Represents a render state.</summary>
public struct RenderState
{
    /// <inheritdoc cref="RenderOptions.UseLinks"/>
    public bool UseLinks;

    /// <summary>Whether to show representations of control characters.</summary>
    public bool UseControlCharacter;

    /// <inheritdoc cref="RenderOptions.WrapMarker"/>
    public ISpannable? WrapMarker;

    /// <summary>Whether to put a dummy after rendering.</summary>
    public bool PutDummyAfterRender;

    /// <inheritdoc cref="RenderOptions.WordBreak"/>
    public WordBreakType WordBreak;

    /// <inheritdoc cref="RenderOptions.AcceptedNewLines"/>
    public NewLineType AcceptedNewLines;

    /// <summary>The resolved ImGui global ID, or 0 if no ImGui state management is used.</summary>
    public uint ImGuiGlobalId;

    /// <inheritdoc cref="RenderOptions.TabWidth"/>
    public float TabWidth;

    /// <inheritdoc cref="RenderOptions.Scale"/>
    public float Scale;

    /// <inheritdoc cref="RenderOptions.MaxSize"/>
    public Vector2 MaxSize;

    /// <summary>The first drawing screen offset.</summary>
    /// <remarks>This is an offset obtained from <see cref="ImGui.GetCursorScreenPos"/>.</remarks>
    public Vector2 StartScreenOffset;

    /// <summary>The target draw list. Can be null.</summary>
    public ImDrawListPtr DrawListPtr;

    /// <inheritdoc cref="RenderOptions.GraphicFontIconMode"/>
    public int GfdIndex;

    /// <inheritdoc cref="RenderOptions.Transformation"/>
    public Matrix4x4 Transformation;

    /// <summary>Inverse of <see cref="Transformation"/>.</summary>
    public Matrix4x4 TransformationInverse;

    /// <inheritdoc cref="RenderOptions.ControlCharactersStyle"/>
    public SpanStyle ControlCharactersStyle;

    /// <inheritdoc cref="RenderOptions.InitialStyle"/>
    public SpanStyle InitialStyle;

    /// <summary>The latest style.</summary>
    public SpanStyle LastStyle;

    /// <summary>The index of the last line, including new lines from word wrapping.</summary>
    public int LineCount;

    /// <summary>The final drawing relative offset, pre-transformed value.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public Vector2 Offset;

    /// <summary>The boundary containing the relative offset of the text rendered, pre-transformed value.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public RectVector4 Boundary;

    /// <summary>The mouse button that has been clicked.</summary>
    /// <remarks>As <c>0</c> is <see cref="ImGuiMouseButton.Left"/>, if no mouse button is detected clicked, then it
    /// will be set to an invalid enum value.</remarks>
    public ImGuiMouseButton ClickedMouseButton;

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
        this.UseControlCharacter = rendererOptions.ControlCharactersStyle.HasValue;
        this.WrapMarker = rendererOptions.WrapMarker;
        this.PutDummyAfterRender = putDummyAfterRender;
        this.WordBreak = rendererOptions.WordBreak ?? WordBreakType.Normal;
        this.AcceptedNewLines = rendererOptions.AcceptedNewLines ?? NewLineType.All;
        this.TabWidth = rendererOptions.TabWidth ?? -4;
        this.Scale = rendererOptions.Scale ?? ImGuiHelpers.GlobalScale;
        this.MaxSize = rendererOptions.MaxSize ?? new(ImGui.GetColumnWidth(), float.MaxValue);
        this.Transformation = rendererOptions.Transformation ?? Matrix4x4.Identity;
        this.ControlCharactersStyle = rendererOptions.ControlCharactersStyle ?? default;
        this.InitialStyle = rendererOptions.InitialStyle ?? SpanStyle.FromContext;
        this.StartScreenOffset = rendererOptions.ScreenOffset ?? ImGui.GetCursorScreenPos();
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
        this.Offset = Vector2.Zero;
        this.Boundary = RectVector4.InvertedExtrema;
        this.LineCount = 0;
        this.ClickedMouseButton = unchecked((ImGuiMouseButton)(-1));
        this.LastStyle = this.InitialStyle;
    }

    /// <summary>Gets a value indicating whether to actually draw.</summary>
    public unsafe bool UseDrawing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.DrawListPtr.NativePtr is not null;
    }

    /// <summary>Determines whether the given new line type is accepted.</summary>
    /// <param name="newLineType">A new line type.</param>
    /// <returns><c>true</c> if accepted.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TestNewLine(NewLineType newLineType) =>
        (this.AcceptedNewLines & newLineType) != 0;

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

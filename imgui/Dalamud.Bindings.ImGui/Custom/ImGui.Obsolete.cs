using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

// Functions that were removed from Dear ImGui, or are only available when IMGUI_DISABLE_OBSOLETE_FUNCTIONS is not
// defined. Implemented the same way as Dear ImGui's obsolete implementations.
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    private const string ImageTintObsoleteMessage =
        "Use ImageWithBg for a tint. Image draws a border with ImGuiCol.Border and ImGuiStyleVar.ImageBorderSize.";

    private const string ImageButtonObsoleteMessage = "Use ImageButton with a string ID instead.";

    /// <summary>Obsoleted in 1.90.0.</summary>
    /// <returns>Current content boundaries in window coordinates.</returns>
    [Obsolete("Use GetContentRegionAvail() + GetCursorScreenPos() - GetWindowPos() instead.")]
    public static Vector2 GetContentRegionMax()
    {
        var g = GetCurrentContext();
        var window = ImGuiP.GetCurrentWindow();
        var mx = window.DC.CurrentColumns is not null || !g.CurrentTable.IsNull
                     ? window.WorkRect.Max
                     : window.ContentRegionRect.Max;
        return mx - window.Pos;
    }

    /// <summary>Obsoleted in 1.90.0.</summary>
    /// <returns>Content boundaries min for the window, in window coordinates.</returns>
    [Obsolete("Use GetCursorStartPos() instead.")]
    public static Vector2 GetWindowContentRegionMin()
    {
        var window = ImGuiP.GetCurrentWindow();
        return window.ContentRegionRect.Min - window.Pos;
    }

    /// <summary>Obsoleted in 1.90.0.</summary>
    /// <returns>Content boundaries max for the window, in window coordinates.</returns>
    [Obsolete("Use GetContentRegionAvail() + GetCursorScreenPos() - GetWindowPos() instead.")]
    public static Vector2 GetWindowContentRegionMax()
    {
        var window = ImGuiP.GetCurrentWindow();
        return window.ContentRegionRect.Max - window.Pos;
    }

    /// <summary>Sets the font scale of the current window. Obsoleted in 1.92.0.</summary>
    /// <param name="scale">The scale.</param>
    [Obsolete("Push a font size instead, such as with Dalamud's ImGui.PushFontScale.")]
    public static void SetWindowFontScale(float scale)
    {
        var window = ImGuiP.GetCurrentWindow();
        window.FontWindowScale = scale;
        ImGuiP.UpdateCurrentFontSize(0.0f);
    }

    /// <summary>Obsoleted in 1.90.0.</summary>
    /// <param name="strId">The ID.</param>
    /// <param name="size">The size.</param>
    /// <param name="border">Whether to draw a border.</param>
    /// <param name="flags">The window flags.</param>
    /// <returns>Whether the child window is visible.</returns>
    [Obsolete("Use the overload taking ImGuiChildFlags.", false)]
    public static bool BeginChild(
        ImU8String strId, Vector2 size, bool border, ImGuiWindowFlags flags = ImGuiWindowFlags.None) =>
        BeginChild(strId, size, border ? ImGuiChildFlags.Borders : ImGuiChildFlags.None, flags);

    /// <summary>Obsoleted in 1.90.0.</summary>
    /// <param name="id">The ID.</param>
    /// <param name="size">The size.</param>
    /// <param name="border">Whether to draw a border.</param>
    /// <param name="flags">The window flags.</param>
    /// <returns>Whether the child window is visible.</returns>
    [Obsolete("Use the overload taking ImGuiChildFlags.", false)]
    public static bool BeginChild(uint id, Vector2 size, bool border, ImGuiWindowFlags flags = ImGuiWindowFlags.None) =>
        BeginChild(id, size, border ? ImGuiChildFlags.Borders : ImGuiChildFlags.None, flags);

    /// <summary>Obsoleted in 1.90.0.</summary>
    /// <param name="id">The ID.</param>
    /// <param name="size">The size.</param>
    /// <param name="flags">The window flags.</param>
    /// <returns>Whether the child frame is visible.</returns>
    [Obsolete("Use BeginChild with ImGuiChildFlags.FrameStyle instead.")]
    public static bool BeginChildFrame(uint id, Vector2 size, ImGuiWindowFlags flags = ImGuiWindowFlags.None) =>
        ImGuiNative.BeginChild(id, size, ImGuiChildFlags.FrameStyle, flags) != 0;

    /// <summary>Obsoleted in 1.90.0.</summary>
    [Obsolete("Use EndChild instead.")]
    public static void EndChildFrame() => EndChild();

    /// <summary>Obsoleted in 1.89.0.</summary>
    /// <param name="userTextureId">The texture.</param>
    /// <param name="size">The image size.</param>
    /// <param name="uv0">The UV of the top left corner.</param>
    /// <param name="uv1">The UV of the bottom right corner.</param>
    /// <param name="framePadding">The frame padding, or a negative value to use the style.</param>
    /// <param name="bgCol">The background color.</param>
    /// <param name="tintCol">The tint color.</param>
    /// <returns>Whether the button has been pressed.</returns>
    [Obsolete(ImageButtonObsoleteMessage)]
    public static bool ImageButton(
        ImTextureID userTextureId, Vector2 size, Vector2 uv0, Vector2 uv1, int framePadding, Vector4 bgCol,
        Vector4 tintCol)
    {
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems)
            return false;

        PushID((void*)userTextureId.Handle);
        var id = ImGuiP.GetID(window, "#image"u8);
        PopID();

        if (framePadding >= 0)
            PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(framePadding, framePadding));
        var ret = ImGuiP.ImageButtonEx(id, userTextureId, size, uv0, uv1, bgCol, tintCol);
        if (framePadding >= 0)
            PopStyleVar();
        return ret;
    }

    /// <inheritdoc cref="ImageButton(ImTextureID, Vector2, Vector2, Vector2, int, Vector4, Vector4)"/>
    [Obsolete(ImageButtonObsoleteMessage)]
    public static bool ImageButton(ImTextureID userTextureId, Vector2 size) =>
        ImageButton(userTextureId, size, Vector2.Zero, Vector2.One, -1, Vector4.Zero, Vector4.One);

    /// <summary>Draws an image with a tint and a border. Obsoleted in 1.91.9.</summary>
    /// <param name="texRef">The texture.</param>
    /// <param name="imageSize">The image size.</param>
    /// <param name="uv0">The UV of the top left corner.</param>
    /// <param name="uv1">The UV of the bottom right corner.</param>
    /// <param name="tintCol">The tint color.</param>
    /// <param name="borderCol">The border color. A border is drawn if its alpha is above 0.</param>
    [Obsolete(ImageTintObsoleteMessage)]
    public static void Image(
        ImTextureRef texRef, Vector2 imageSize, Vector2 uv0, Vector2 uv1, Vector4 tintCol, Vector4 borderCol)
    {
        PushStyleVar(
            ImGuiStyleVar.ImageBorderSize,
            borderCol.W > 0.0f ? Math.Max(1.0f, GetStyle().ImageBorderSize) : 0.0f);
        PushStyleColor(ImGuiCol.Border, borderCol);
        ImageWithBg(texRef, imageSize, uv0, uv1, Vector4.Zero, tintCol);
        PopStyleColor();
        PopStyleVar();
    }

    /// <inheritdoc cref="Image(ImTextureRef, Vector2, Vector2, Vector2, Vector4, Vector4)"/>
    [Obsolete(ImageTintObsoleteMessage)]
    public static void Image(ImTextureRef texRef, Vector2 imageSize, Vector2 uv0, Vector2 uv1, Vector4 tintCol) =>
        Image(texRef, imageSize, uv0, uv1, tintCol, Vector4.Zero);

    /// <inheritdoc cref="Image(ImTextureRef, Vector2, Vector2, Vector2, Vector4, Vector4)"/>
    [Obsolete(ImageTintObsoleteMessage)]
    public static void Image(ImTextureRef texRef, Vector2 imageSize, Vector2 uv0, Vector4 tintCol) =>
        Image(texRef, imageSize, uv0, Vector2.One, tintCol, Vector4.Zero);

    /// <inheritdoc cref="Image(ImTextureRef, Vector2, Vector2, Vector2, Vector4, Vector4)"/>
    [Obsolete(ImageTintObsoleteMessage)]
    public static void Image(ImTextureRef texRef, Vector2 imageSize, Vector4 tintCol) =>
        Image(texRef, imageSize, Vector2.Zero, Vector2.One, tintCol, Vector4.Zero);

    /// <inheritdoc cref="Image(ImTextureRef, Vector2, Vector2, Vector2, Vector4, Vector4)"/>
    [Obsolete(ImageTintObsoleteMessage)]
    public static void Image(
        ImTextureRef texRef, Vector2 imageSize, Vector2 uv0, Vector4 tintCol, Vector4 borderCol) =>
        Image(texRef, imageSize, uv0, Vector2.One, tintCol, borderCol);

    /// <inheritdoc cref="Image(ImTextureRef, Vector2, Vector2, Vector2, Vector4, Vector4)"/>
    [Obsolete(ImageTintObsoleteMessage)]
    public static void Image(ImTextureRef texRef, Vector2 imageSize, Vector4 tintCol, Vector4 borderCol) =>
        Image(texRef, imageSize, Vector2.Zero, Vector2.One, tintCol, borderCol);

    /// <summary>Sets the options of color edit and picker widgets. Obsoleted in 1.92.9.</summary>
    /// <param name="flags">The options. A group without a flag set takes its default.</param>
    [Obsolete("Set ImGuiIO.ConfigColorEditFlags instead.")]
    public static void SetColorEditOptions(ImGuiColorEditFlags flags)
    {
        if ((flags & ImGuiColorEditFlags.DisplayMask) == 0)
            flags |= ImGuiColorEditFlags.DefaultOptions & ImGuiColorEditFlags.DisplayMask;
        if ((flags & ImGuiColorEditFlags.DataTypeMask) == 0)
            flags |= ImGuiColorEditFlags.DefaultOptions & ImGuiColorEditFlags.DataTypeMask;
        if ((flags & ImGuiColorEditFlags.PickerMask) == 0)
            flags |= ImGuiColorEditFlags.DefaultOptions & ImGuiColorEditFlags.PickerMask;
        if ((flags & ImGuiColorEditFlags.InputMask) == 0)
            flags |= ImGuiColorEditFlags.DefaultOptions & ImGuiColorEditFlags.InputMask;
        GetIO().ConfigColorEditFlags = flags;
    }
}

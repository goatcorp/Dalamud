using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Fonts.Internal.Markup;
using Dalamud.Interface.ImGuiBackend.Renderers;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>What the ImGui backend and its renderers call of the font engine.</summary>
/// <remarks>It is called on the main thread, a few times per frame or draw data, never per glyph.</remarks>
internal static unsafe class GlyphAtlasTextResources
{
    /// <summary>Sets up text in the ImGui context just made.</summary>
    public static void OnContextCreated() => TextLayouter.Install();

    /// <summary>Starts the text of a frame, after <see cref="ImGui.NewFrame"/>.</summary>
    /// <remarks>Drops markup callbacks pushed and never popped.</remarks>
    public static void OnNewFrame() => TextCallbackStack.Shrink(0);

    /// <summary>Ends the frame of the font engine, once the frame has been rendered. Called once per frame, also
    /// after a frame that failed.</summary>
    /// <remarks><ul>
    /// <li>The glyph budget starts again, and synchronous scopes left open end.</li>
    /// <li>The atlas retires and frees slices not used lately.</li>
    /// <li><see cref="FontClock.Frame"/> counts the frame.</li>
    /// <li>Fonts nobody holds or used lately are released.</li>
    /// </ul></remarks>
    /// <param name="resolver">The resolver whose fonts to release, or null if there is none yet.</param>
    public static void OnFrameRendered(FontResolver? resolver)
    {
        // The steps of the atlas and the clock never throw. They go first, so that a throw after them does not stop
        // the atlas from retiring slices.
        GlyphRasterBudget.EndFrame();
        GlyphAtlas.Shared.EndFrame();
        FontClock.Advance();
        resolver?.OnFrameRendered();
        FontProfiler.EndFrame();
    }

    /// <summary>Makes the queued glyph changes, and gets the glyph texture arrays to bind.</summary>
    /// <param name="context">The immediate context. Its device must be the same until
    /// <see cref="ReleaseDeviceResources"/>.</param>
    /// <returns>The views of the coverage and color arrays, either null if it has nothing yet.</returns>
    /// <remarks><ul>
    /// <li>They are bound at <c>t1</c> and <c>t2</c> of <c>imgui-text.hlsli</c>.</li>
    /// <li>The views are valid until the next call.</li>
    /// </ul></remarks>
    public static (nint Coverage, nint Color) Upload(ID3D11DeviceContext* context) =>
        GlyphAtlas.Shared.Upload(context);

    /// <summary>Makes the draws after this in a draw list read glyphs from other texture arrays.</summary>
    /// <param name="drawList">The draw list.</param>
    /// <param name="textures">The views to bind at <c>t1</c> and <c>t2</c>, from <see cref="GlyphImage.Textures"/>,
    /// or null for the arrays of <see cref="Upload"/>.</param>
    /// <remarks>The renderers handle <see cref="CustomImDrawCallbackEnum.BindGlyphTextures"/>.</remarks>
    public static void BindTextures(ImDrawList* drawList, nint textures) =>
        ImGuiNative.AddCallback(
            drawList,
            (delegate*<ImDrawList*, ImDrawCmd*, void>)(nint)CustomImDrawCallbackEnum.BindGlyphTextures,
            (void*)textures,
            0);

    /// <summary>Gets how the text shader draws on the monitor a window is on.</summary>
    /// <param name="window">The window, or default for the primary monitor.</param>
    /// <returns>The settings.</returns>
    public static SubpixelSettings.MonitorSettings GetShaderSettings(HWND window) =>
        SubpixelSettings.ForWindow(window);

    /// <summary>Releases what was made on the device, before the ImGui context is destroyed.</summary>
    /// <remarks>Main thread only. A finalizer must not call it.</remarks>
    public static void ReleaseDeviceResources() => GlyphAtlas.DisposeShared();
}

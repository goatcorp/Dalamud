using System.Linq;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Spannables;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using ImGuiNET;

using Lumina.Data.Files;

namespace Dalamud.Interface.SpannedStrings.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
[ServiceManager.EarlyLoadedService]
[PluginInterface]
[InterfaceVersion("1.0")]
#pragma warning disable SA1015
[ResolveVia<ISpannableRenderer>]
#pragma warning restore SA1015
internal sealed unsafe partial class SpannableRenderer : ISpannableRenderer, IInternalDisposableService
{
    /// <summary>The display character in place of a soft hyphen character.</summary>
    public const char SoftHyphenReplacementChar = '-';

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly TextureManager textureManager = Service<TextureManager>.Get();

    [ServiceManager.ServiceConstructor]
    private SpannableRenderer(InterfaceManager.InterfaceManagerWithScene imws)
    {
        var t = this.dataManager.GetFile("common/font/gfdata.gfd")!.Data;
        t.CopyTo((this.gfdFile = GC.AllocateUninitializedArray<byte>(t.Length, true)).AsSpan());
        this.gfdTextures =
            GfdTexturePaths
                .Select(x => this.textureManager.GetTexture(this.dataManager.GetFile<TexFile>(x)!))
                .ToArray();
    }

    /// <summary>Finalizes an instance of the <see cref="SpannableRenderer"/> class.</summary>
    ~SpannableRenderer() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public void DisposeService() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public void Render(ReadOnlySpan<char> sequence, RenderState renderState)
    {
        var ssb = this.RentBuilder();
        ssb.Append(sequence);
        this.Render(ssb, ref renderState);
        this.ReturnBuilder(ssb);
    }

    /// <inheritdoc/>
    public void Render(ReadOnlySpan<char> sequence, ref RenderState renderState)
    {
        var ssb = this.RentBuilder();
        ssb.Append(sequence);
        this.Render(ssb, ref renderState);
        this.ReturnBuilder(ssb);
    }

    /// <inheritdoc/>
    public void Render(ISpannable spannable, RenderState renderState) =>
        this.Render(spannable, ref renderState, out _);

    /// <inheritdoc/>
#pragma warning disable CS9087 // This returns a parameter by reference but it is not a ref parameter
    public bool Render(ISpannable spannable, RenderState renderState, out ReadOnlySpan<byte> hoveredLink) =>
        this.Render(spannable, ref renderState, out hoveredLink);
#pragma warning restore CS9087 // This returns a parameter by reference but it is not a ref parameter

    /// <inheritdoc/>
    public void Render(ISpannable spannable, ref RenderState renderState) =>
        this.Render(spannable, ref renderState, out _);

    /// <inheritdoc/>
    public bool Render(ISpannable spannable, ref RenderState renderState, out ReadOnlySpan<byte> hoveredLink)
    {
        ThreadSafety.AssertMainThread();

        using var splitter = renderState.UseDrawing ? this.RentSplitter(renderState.DrawListPtr) : default;

        var state = spannable.RentState(this, renderState, null);

        spannable.Measure(new(state));
        if (renderState.UseLinks)
            spannable.InteractWith(new(state), out hoveredLink);
        else
            hoveredLink = default;
        spannable.Draw(new(state, splitter));

        renderState = state.RenderState;
        spannable.ReturnState(state);

        if (renderState.PutDummyAfterRender)
        {
            var lt = renderState.Transform(renderState.Boundary.LeftTop);
            var rt = renderState.Transform(renderState.Boundary.RightTop);
            var rb = renderState.Transform(renderState.Boundary.RightBottom);
            var lb = renderState.Transform(renderState.Boundary.LeftBottom);
            var minPos = Vector2.Min(Vector2.Min(lt, rt), Vector2.Min(lb, rb));
            var maxPos = Vector2.Max(Vector2.Max(lt, rt), Vector2.Max(lb, rb));
            if (minPos.X <= maxPos.X && minPos.Y <= maxPos.Y)
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + minPos);
                ImGui.Dummy(maxPos - minPos);
            }
        }

        return !hoveredLink.IsEmpty;
    }

    /// <summary>Clear the resources used by this instance.</summary>
    private void ReleaseUnmanagedResources()
    {
        this.DisposePooledObjects();
    }
}

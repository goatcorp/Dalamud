using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    public bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat) =>
        this.scene.TextureManager.IsDxgiFormatSupportedForCreateFromExistingTextureAsync(dxgiFormat);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            async _ =>
            {
                var outWrap = await this.scene.TextureManager.CreateFromExistingTextureAsync(
                                  wrap,
                                  args,
                                  debugName,
                                  cancellationToken);
                this.BlameSetName(
                    outWrap,
                    debugName ??
                    $"{nameof(this.CreateFromExistingTextureAsync)}({wrap}, {args})");
                return outWrap;
            },
            cancellationToken,
            leaveWrapOpen ? null : wrap);

    /// <inheritdoc/>
    Task<IDalamudTextureWrap> ITextureProvider.CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        string? debugName,
        CancellationToken cancellationToken) =>
        this.CreateFromImGuiViewportAsync(args, null, debugName, cancellationToken);

    /// <inheritdoc cref="ITextureProvider.CreateFromImGuiViewportAsync"/>
    public Task<IDalamudTextureWrap> CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        LocalPlugin? ownerPlugin,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.scene.TextureManager.CreateFromImGuiViewportAsync(args, ownerPlugin, debugName, cancellationToken);

    /// <inheritdoc/>
    public async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var wrapDispose = leaveWrapOpen ? null : wrap;
        return await this.scene.TextureManager.GetRawImageAsync(wrap, args, cancellationToken);
    }
}

using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    [ServiceManager.ServiceDependency]
    private readonly SeStringRenderer seStringRenderer = Service<SeStringRenderer>.Get();

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTextureFromSeString(
        ReadOnlySpan<byte> text,
        scoped in SeStringDrawParams drawParams = default,
        string? debugName = null)
    {
        ThreadSafety.AssertMainThread();
        using var dd = this.seStringRenderer.CreateDrawData(text, drawParams);
        var texture = this.CreateDrawListTexture(debugName ?? nameof(this.CreateTextureFromSeString));
        try
        {
            texture.Size = dd.Data.DisplaySize;
            texture.Draw(dd.DataPtr);
            return texture;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }
}

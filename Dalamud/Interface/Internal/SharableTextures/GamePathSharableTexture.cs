using System.IO;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Utility;

using Lumina.Data.Files;

namespace Dalamud.Interface.Internal.SharableTextures;

/// <summary>
/// Represents a sharable texture, based on a file in game resources.
/// </summary>
internal sealed class GamePathSharableTexture : SharableTexture
{
    private readonly string path;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamePathSharableTexture"/> class.
    /// </summary>
    /// <param name="path">The path.</param>
    public GamePathSharableTexture(string path)
    {
        this.path = path;
        this.UnderlyingWrap = this.CreateTextureAsync();
    }

    /// <inheritdoc/>
    public override string SourcePathForDebug => this.path;

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(GamePathSharableTexture)}#{this.InstanceIdForDebug}({this.path})";

    /// <inheritdoc/>
    protected override void ReleaseResources()
    {
        this.DisposeSuppressingWrap = null;
        _ = this.UnderlyingWrap?.ToContentDisposedTask(true);
        this.UnderlyingWrap = null;
    }

    /// <inheritdoc/>
    protected override void ReviveResources() =>
        this.UnderlyingWrap = this.CreateTextureAsync();

    private Task<IDalamudTextureWrap> CreateTextureAsync() =>
        Task.Run(
            async () =>
            {
                var dm = await Service<DataManager>.GetAsync();
                var im = await Service<InterfaceManager>.GetAsync();
                var file = dm.GetFile<TexFile>(this.path);
                var t = (IDalamudTextureWrap)im.LoadImageFromTexFile(file ?? throw new FileNotFoundException());
                this.DisposeSuppressingWrap = new(t);
                return t;
            });
}

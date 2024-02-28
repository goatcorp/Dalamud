using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Utility;

using Lumina.Data.Files;

namespace Dalamud.Interface.Internal.SharedImmediateTextures;

/// <summary>Represents a sharable texture, based on a file in game resources.</summary>
internal sealed class GamePathSharedImmediateTexture : SharedImmediateTexture
{
    private readonly string path;

    /// <summary>Initializes a new instance of the <see cref="GamePathSharedImmediateTexture"/> class.</summary>
    /// <param name="path">The path.</param>
    private GamePathSharedImmediateTexture(string path) => this.path = path;

    /// <inheritdoc/>
    public override string SourcePathForDebug => this.path;

    /// <summary>Creates a new placeholder instance of <see cref="GamePathSharedImmediateTexture"/>.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    public static SharedImmediateTexture CreatePlaceholder(string path) => new GamePathSharedImmediateTexture(path);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(GamePathSharedImmediateTexture)}#{this.InstanceIdForDebug}({this.path})";

    /// <inheritdoc/>
    protected override void ReleaseResources()
    {
        _ = this.UnderlyingWrap?.ToContentDisposedTask(true);
        this.UnderlyingWrap = null;
    }

    /// <inheritdoc/>
    protected override void ReviveResources() =>
        this.UnderlyingWrap = Service<TextureLoadThrottler>.Get().CreateLoader(
            this,
            this.CreateTextureAsync,
            this.LoadCancellationToken);

    private async Task<IDalamudTextureWrap> CreateTextureAsync(CancellationToken cancellationToken)
    {
        var dm = await Service<DataManager>.GetAsync();
        var tm = await Service<TextureManager>.GetAsync();
        if (dm.GetFile<TexFile>(this.path) is not { } file)
            throw new FileNotFoundException();
        cancellationToken.ThrowIfCancellationRequested();
        return tm.NoThrottleGetFromTexFile(file);
    }
}

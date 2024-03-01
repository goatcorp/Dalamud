using System.Threading;
using System.Threading.Tasks;

using Dalamud.Utility;

namespace Dalamud.Interface.Internal.SharedImmediateTextures;

/// <summary>Represents a sharable texture, based on a file on the system filesystem.</summary>
internal sealed class FileSystemSharedImmediateTexture : SharedImmediateTexture
{
    private readonly string path;

    /// <summary>Initializes a new instance of the <see cref="FileSystemSharedImmediateTexture"/> class.</summary>
    /// <param name="path">The path.</param>
    private FileSystemSharedImmediateTexture(string path) => this.path = path;

    /// <inheritdoc/>
    public override string SourcePathForDebug => this.path;

    /// <summary>Creates a new placeholder instance of <see cref="GamePathSharedImmediateTexture"/>.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    public static SharedImmediateTexture CreatePlaceholder(string path) => new FileSystemSharedImmediateTexture(path);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(FileSystemSharedImmediateTexture)}#{this.InstanceIdForDebug}({this.path})";

    /// <inheritdoc/>
    protected override void ReleaseResources()
    {
        _ = this.UnderlyingWrap?.ToContentDisposedTask(true);
        this.UnderlyingWrap = null;
    }

    /// <inheritdoc/>
    protected override void ReviveResources() =>
        this.UnderlyingWrap = Service<TextureLoadThrottler>.Get().LoadTextureAsync(
            this,
            this.CreateTextureAsync,
            this.LoadCancellationToken);

    private async Task<IDalamudTextureWrap> CreateTextureAsync(CancellationToken cancellationToken)
    {
        var tm = await Service<TextureManager>.GetAsync();
        return await tm.NoThrottleCreateFromFileAsync(this.path, cancellationToken);
    }
}

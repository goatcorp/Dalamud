using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Utility;

namespace Dalamud.Interface.Internal.SharableTextures;

/// <summary>
/// Represents a sharable texture, based on a file on the system filesystem.
/// </summary>
internal sealed class FileSystemSharableTexture : SharableTexture
{
    private readonly string path;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemSharableTexture"/> class.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="holdSelfReference">If set to <c>true</c>, this class will hold a reference to self.
    /// Otherwise, it is expected that the caller to hold the reference.</param>
    private FileSystemSharableTexture(string path, bool holdSelfReference)
        : base(holdSelfReference)
    {
        this.path = path;
        if (holdSelfReference)
            this.ReviveResources();
    }

    /// <inheritdoc/>
    public override string SourcePathForDebug => this.path;

    /// <summary>
    /// Creates a new instance of <see cref="GamePathSharableTexture"/>.
    /// The new instance will hold a reference to itself.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    public static SharableTexture CreateImmediate(string path) => new FileSystemSharableTexture(path, true);

    /// <summary>
    /// Creates a new instance of <see cref="GamePathSharableTexture"/>.
    /// The caller is expected to manage ownership of the new instance.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    public static SharableTexture CreateAsync(string path) => new FileSystemSharableTexture(path, false);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(FileSystemSharableTexture)}#{this.InstanceIdForDebug}({this.path})";

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
        var tm = await Service<TextureManager>.GetAsync();
        return tm.NoThrottleGetFromImage(await File.ReadAllBytesAsync(this.path, cancellationToken));
    }
}

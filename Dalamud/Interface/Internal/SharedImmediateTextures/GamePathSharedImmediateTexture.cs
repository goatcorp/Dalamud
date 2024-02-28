using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Utility;

using Lumina.Data.Files;

namespace Dalamud.Interface.Internal.SharedImmediateTextures;

/// <summary>
/// Represents a sharable texture, based on a file in game resources.
/// </summary>
internal sealed class GamePathSharedImmediateTexture : SharedImmediateTexture
{
    private readonly string path;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamePathSharedImmediateTexture"/> class.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="holdSelfReference">If set to <c>true</c>, this class will hold a reference to self.
    /// Otherwise, it is expected that the caller to hold the reference.</param>
    private GamePathSharedImmediateTexture(string path, bool holdSelfReference)
        : base(holdSelfReference)
    {
        this.path = path;
        if (holdSelfReference)
            this.ReviveResources();
    }

    /// <inheritdoc/>
    public override string SourcePathForDebug => this.path;

    /// <summary>
    /// Creates a new instance of <see cref="GamePathSharedImmediateTexture"/>.
    /// The new instance will hold a reference to itself.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    public static SharedImmediateTexture CreateImmediate(string path) => new GamePathSharedImmediateTexture(path, true);

    /// <summary>
    /// Creates a new instance of <see cref="GamePathSharedImmediateTexture"/>.
    /// The caller is expected to manage ownership of the new instance.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    public static SharedImmediateTexture CreateAsync(string path) => new GamePathSharedImmediateTexture(path, false);

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

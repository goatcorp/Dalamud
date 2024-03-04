using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;

namespace Dalamud.Interface.Textures.Internal.SharedImmediateTextures;

/// <summary>Represents a sharable texture, based on a file on the system filesystem.</summary>
internal sealed class FileSystemSharedImmediateTexture : SharedImmediateTexture
{
    private readonly string path;

    /// <summary>Initializes a new instance of the <see cref="FileSystemSharedImmediateTexture"/> class.</summary>
    /// <param name="path">The path.</param>
    private FileSystemSharedImmediateTexture(string path)
        : base(path) => this.path = path;

    /// <summary>Creates a new placeholder instance of <see cref="GamePathSharedImmediateTexture"/>.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    public static SharedImmediateTexture CreatePlaceholder(string path) => new FileSystemSharedImmediateTexture(path);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(FileSystemSharedImmediateTexture)}#{this.InstanceIdForDebug}({this.path})";

    /// <inheritdoc/>
    protected override async Task<IDalamudTextureWrap> CreateTextureAsync(CancellationToken cancellationToken)
    {
        var tm = await Service<TextureManager>.GetAsync();
        var wrap = await tm.NoThrottleCreateFromFileAsync(this.path, cancellationToken);
        tm.BlameSetName(wrap, this.ToString());
        return wrap;
    }
}

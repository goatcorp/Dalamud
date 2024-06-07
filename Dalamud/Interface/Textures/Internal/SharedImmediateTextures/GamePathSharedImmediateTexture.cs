using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

using Lumina.Data.Files;

namespace Dalamud.Interface.Textures.Internal.SharedImmediateTextures;

/// <summary>Represents a sharable texture, based on a file in game resources.</summary>
internal sealed class GamePathSharedImmediateTexture : SharedImmediateTexture
{
    private readonly string path;

    /// <summary>Initializes a new instance of the <see cref="GamePathSharedImmediateTexture"/> class.</summary>
    /// <param name="path">The path.</param>
    private GamePathSharedImmediateTexture(string path)
        : base(path) => this.path = path;

    /// <summary>Creates a new placeholder instance of <see cref="GamePathSharedImmediateTexture"/>.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The new instance.</returns>
    /// <remarks>Only to be used from <see cref="TextureManager.SharedTextureManager.GetFromGame"/>.</remarks>
    public static SharedImmediateTexture CreatePlaceholder(string path) => new GamePathSharedImmediateTexture(path);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(GamePathSharedImmediateTexture)}#{this.InstanceIdForDebug}({this.path})";

    /// <inheritdoc/>
    protected override async Task<IDalamudTextureWrap> CreateTextureAsync(CancellationToken cancellationToken)
    {
        var dm = await Service<DataManager>.GetAsync();
        var tm = await Service<TextureManager>.GetAsync();
        var substPath = tm.GetSubstitutedPath(this.path);
        if (dm.GetFile<TexFile>(substPath) is not { } file)
            throw new FileNotFoundException();
        cancellationToken.ThrowIfCancellationRequested();
        var wrap = tm.NoThrottleCreateFromTexFile(file);
        tm.BlameSetName(wrap, this.ToString());
        return wrap;
    }
}

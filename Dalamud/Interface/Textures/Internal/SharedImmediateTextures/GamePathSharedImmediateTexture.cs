using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Interface.Textures.TextureWraps;

using Lumina.Data.Files;

using Serilog;

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

        TexFile? file;

        var substPath = tm.GetSubstitutedPath(this.path);
        if (!string.IsNullOrWhiteSpace(substPath) && substPath != this.path)
        {
            try
            {
                file =
                    Path.IsPathRooted(substPath)
                        ? dm.GameData.GetFileFromDisk<TexFile>(substPath, this.path)
                        : dm.GetFile<TexFile>(substPath) ??
                          throw new FileNotFoundException("Game file not found.", substPath);
            }
            catch (Exception e)
            {
                file = dm.GetFile<TexFile>(this.path);
                if (file is null)
                    throw;

                Log.Warning(
                    e,
                    "{who}: substitute path {subst} for {orig} failed to load. Using original path instead.",
                    nameof(GamePathSharedImmediateTexture),
                    substPath,
                    this.path);
            }
        }
        else
        {
            file = dm.GetFile<TexFile>(this.path) ?? throw new FileNotFoundException("Game file not found.", this.path);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var wrap = tm.NoThrottleCreateFromTexFile(file.Header, file.TextureBuffer);
        tm.BlameSetName(wrap, this.ToString());
        return wrap;
    }
}

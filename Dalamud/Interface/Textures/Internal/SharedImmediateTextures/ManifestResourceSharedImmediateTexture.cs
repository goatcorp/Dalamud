using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Textures.Internal.SharedImmediateTextures;

/// <summary>Represents a sharable texture, based on a manifest texture obtained from
/// <see cref="Assembly.GetManifestResourceStream(string)"/>.</summary>
internal sealed class ManifestResourceSharedImmediateTexture : SharedImmediateTexture
{
    private readonly Assembly assembly;
    private readonly string name;

    /// <summary>Initializes a new instance of the <see cref="ManifestResourceSharedImmediateTexture"/> class.</summary>
    /// <param name="assembly">The assembly containing manifest resources.</param>
    /// <param name="name">The case-sensitive name of the manifest resource being requested.</param>
    private ManifestResourceSharedImmediateTexture(Assembly assembly, string name)
    {
        this.assembly = assembly;
        this.name = name;
    }

    /// <inheritdoc/>
    public override string SourcePathForDebug => $"{this.assembly.GetName().FullName}:{this.name}";

    /// <summary>Creates a new placeholder instance of <see cref="ManifestResourceSharedImmediateTexture"/>.</summary>
    /// <param name="args">The arguments to pass to the constructor.</param>
    /// <returns>The new instance.</returns>
    public static SharedImmediateTexture CreatePlaceholder((Assembly Assembly, string Name) args) =>
        new ManifestResourceSharedImmediateTexture(args.Assembly, args.Name);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(ManifestResourceSharedImmediateTexture)}#{this.InstanceIdForDebug}({this.SourcePathForDebug})";

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
        await using var stream = this.assembly.GetManifestResourceStream(this.name);
        if (stream is null)
            throw new FileNotFoundException("The resource file could not be found.");

        var tm = await Service<TextureManager>.GetAsync();
        var ms = new MemoryStream(stream.CanSeek ? (int)stream.Length : 0);
        await stream.CopyToAsync(ms, cancellationToken);
        return tm.NoThrottleCreateFromImage(ms.GetBuffer().AsMemory(0, (int)ms.Length), cancellationToken);
    }
}

using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;

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
        : base($"{assembly.GetName().FullName}:{name}")
    {
        this.assembly = assembly;
        this.name = name;
    }

    /// <summary>Creates a new placeholder instance of <see cref="ManifestResourceSharedImmediateTexture"/>.</summary>
    /// <param name="args">The arguments to pass to the constructor.</param>
    /// <returns>The new instance.</returns>
    /// <remarks>Only to be used from <see cref="TextureManager.SharedTextureManager.GetFromManifestResource"/>.
    /// </remarks>
    public static SharedImmediateTexture CreatePlaceholder((Assembly Assembly, string Name) args) =>
        new ManifestResourceSharedImmediateTexture(args.Assembly, args.Name);

    /// <inheritdoc/>
    protected override async Task<IDalamudTextureWrap> CreateTextureAsync(CancellationToken cancellationToken)
    {
        await using var stream = this.assembly.GetManifestResourceStream(this.name);
        if (stream is null)
            throw new FileNotFoundException("The resource file could not be found.");

        var tm = await Service<TextureManager>.GetAsync();
        var ms = new MemoryStream(stream.CanSeek ? checked((int)stream.Length) : 0);
        await stream.CopyToAsync(ms, cancellationToken);
        var wrap = tm.NoThrottleCreateFromImage(ms.GetBuffer().AsMemory(0, checked((int)ms.Length)), cancellationToken);
        tm.BlameSetName(wrap, this.ToString());
        return wrap;
    }
}

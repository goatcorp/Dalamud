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
    public FileSystemSharableTexture(string path)
    {
        this.path = path;
        this.UnderlyingWrap = this.CreateTextureAsync();
    }

    /// <inheritdoc/>
    public override string SourcePathForDebug => this.path;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(FileSystemSharableTexture)}#{this.InstanceIdForDebug}({this.path})";

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
            () =>
            {
                var w = (IDalamudTextureWrap)Service<InterfaceManager>.Get().LoadImage(this.path)
                        ?? throw new("Failed to load image because of an unknown reason.");
                this.DisposeSuppressingWrap = new(w);
                return w;
            });
}

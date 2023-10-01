using System.IO;

using Dalamud.IoC.Internal;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Class containing various textures used by Dalamud windows for branding purposes.
/// </summary>
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[InherentDependency<InterfaceManager.InterfaceManagerWithScene>] // Can't load textures before this
#pragma warning restore SA1015
internal class Branding : IServiceType, IDisposable
{
    private readonly Dalamud dalamud;
    private readonly TextureManager tm;

    /// <summary>
    /// Initializes a new instance of the <see cref="Branding"/> class.
    /// </summary>
    /// <param name="dalamud">Dalamud instance.</param>
    /// <param name="tm">TextureManager instance.</param>
    [ServiceManager.ServiceConstructor]
    public Branding(Dalamud dalamud, TextureManager tm)
    {
        this.dalamud = dalamud;
        this.tm = tm;
        
        this.LoadTextures();
    }
    
    /// <summary>
    /// Gets a full-size Dalamud logo texture.
    /// </summary>
    public IDalamudTextureWrap Logo { get; private set; } = null!;

    /// <summary>
    /// Gets a small Dalamud logo texture.
    /// </summary>
    public IDalamudTextureWrap LogoSmall { get; private set; } = null!;
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.Logo.Dispose();
        this.LogoSmall.Dispose();
    }

    private void LoadTextures()
    {
        this.Logo = this.tm.GetTextureFromFile(new FileInfo(Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "logo.png"))) 
                    ?? throw new Exception("Could not load logo.");

        this.LogoSmall = this.tm.GetTextureFromFile(new FileInfo(Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "tsmLogo.png"))) 
                         ?? throw new Exception("Could not load TSM logo.");
    }
}

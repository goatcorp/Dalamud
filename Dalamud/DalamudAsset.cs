using Dalamud.Storage.Assets;

using TerraFX.Interop.DirectX;

namespace Dalamud;

/// <summary>
/// Specifies an asset that has been shipped as Dalamud Asset.<br />
/// <strong>Any asset can cease to exist at any point, even if the enum value exists.</strong><br />
/// Either ship your own assets, or be prepared for errors.
/// </summary>
public enum DalamudAsset
{
    /// <summary>
    /// Nothing.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.Empty, data: [])]
    Unspecified = 0,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromRaw"/>: A texture that is completely transparent.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromRaw, data: [0, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF])]
    [DalamudAssetRawTexture(4, 4, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, 8)]
    Empty4X4 = 1000,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromRaw"/>: A texture that is completely white.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromRaw, data: [0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0])]
    [DalamudAssetRawTexture(4, 4, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, 8)]
    White4X4 = 1014,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The Dalamud logo.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "logo.png")]
    Logo = 1001,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The Dalamud logo, but smaller.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "tsmLogo.png")]
    LogoSmall = 1002,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The default plugin icon.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "defaultIcon.png")]
    DefaultIcon = 1003,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The disabled plugin icon.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "disabledIcon.png")]
    DisabledIcon = 1004,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The outdated installable plugin icon.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "outdatedInstallableIcon.png")]
    OutdatedInstallableIcon = 1005,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The plugin trouble icon overlay.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "troubleIcon.png")]
    TroubleIcon = 1006,
    
    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The plugin trouble icon overlay.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "devPluginIcon.png")]
    DevPluginIcon = 1007,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The plugin update icon overlay.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "updateIcon.png")]
    UpdateIcon = 1008,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The plugin installed icon overlay.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "installedIcon.png")]
    InstalledIcon = 1009,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The third party plugin icon overlay.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "thirdIcon.png")]
    ThirdIcon = 1010,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The installed third party plugin icon overlay.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "thirdInstalledIcon.png")]
    ThirdInstalledIcon = 1011,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The API bump explainer icon.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "changelogApiBump.png")]
    ChangelogApiBumpIcon = 1012,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.TextureFromPng"/>: The background shade for
    /// <see cref="Interface.Internal.Windows.TitleScreenMenuWindow"/>.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.TextureFromPng)]
    [DalamudAssetPath("UIRes", "tsmShade.png")]
    TitleScreenMenuShade = 1013,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.Font"/>: Noto Sans CJK JP Medium.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.Font)]
    [DalamudAssetPath("UIRes", "NotoSansCJKjp-Regular.otf")]
    [DalamudAssetPath("UIRes", "NotoSansCJKjp-Medium.otf")]
    NotoSansJpMedium = 2000,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.Font"/>: Noto Sans CJK KR Regular.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.Font)]
    [DalamudAssetPath("UIRes", "NotoSansCJKkr-Regular.otf")]
    [DalamudAssetPath("UIRes", "NotoSansKR-Regular.otf")]
    NotoSansKrRegular = 2001,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.Font"/>: Inconsolata Regular.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.Font)]
    [DalamudAssetPath("UIRes", "Inconsolata-Regular.ttf")]
    InconsolataRegular = 2002,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.Font"/>: FontAwesome Free Solid.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.Font)]
    [DalamudAssetPath("UIRes", "FontAwesomeFreeSolid.otf")]
    FontAwesomeFreeSolid = 2003,

    /// <summary>
    /// <see cref="DalamudAssetPurpose.Font"/>: Game symbol fonts being used as webfonts at Lodestone.
    /// </summary>
    [DalamudAsset(DalamudAssetPurpose.Font, required: false)]
    // [DalamudAssetOnlineSource("https://img.finalfantasyxiv.com/lds/pc/global/fonts/FFXIV_Lodestone_SSF.ttf")]
    LodestoneGameSymbol = 2004,
}

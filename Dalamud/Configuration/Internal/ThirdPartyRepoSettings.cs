namespace Dalamud.Configuration;

/// <summary>
/// Third party repository for dalamud plugins.
/// </summary>
internal sealed class ThirdPartyRepoSettings
{
    /// <summary>
    /// Gets or sets the third party repo url.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the third party repo is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a short name for the repo url.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Clone this object.
    /// </summary>
    /// <returns>A shallow copy of this object.</returns>
    public ThirdPartyRepoSettings Clone() => this.MemberwiseClone() as ThirdPartyRepoSettings;
}

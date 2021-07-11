namespace Dalamud.Configuration
{
    /// <summary>
    /// Third party repository for dalamud plugins.
    /// </summary>
    public class ThirdRepoSetting
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
        /// Create new instance of third party repo object.
        /// </summary>
        /// <returns>New instance of third party repo.</returns>
        public ThirdRepoSetting Clone()
        {
            return new ThirdRepoSetting
            {
                Url = this.Url,
                IsEnabled = this.IsEnabled,
            };
        }
    }
}

namespace Dalamud.Configuration.Internal
{
    /// <summary>
    /// Settings for DevPlugins.
    /// </summary>
    internal sealed class DevPluginSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DevPluginSettings"/> class.
        /// </summary>
        /// <param name="dllFile">Filename of the DLL representing this plugin.</param>
        public DevPluginSettings(string dllFile)
        {
            this.DllFile = dllFile;
        }

        /// <summary>
        /// Gets or sets the path to a plugin DLL. This is automatically generated for any plugins in the devPlugins folder. However by
        /// specifiying this value manually, you can add arbitrary files outside the normal file paths.
        /// </summary>
        public string DllFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this plugin should automatically start when Dalamud boots up.
        /// </summary>
        public bool StartOnBoot { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this plugin should automatically reload on file change.
        /// </summary>
        public bool AutomaticReloading { get; set; } = false;
    }
}

using System;

namespace Dalamud.Configuration.Internal
{
    /// <summary>
    /// Environmental configuration settings.
    /// </summary>
    internal class EnvironmentConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether the DALAMUD_NOT_HAVE_INTERFACE setting has been enabled.
        /// </summary>
        public static bool DalamudNoInterface { get; } = GetEnvironmentVariable("DALAMUD_NOT_HAVE_INTERFACE");

        /// <summary>
        /// Gets a value indicating whether the XL_WINEONLINUX setting has been enabled.
        /// </summary>
        public static bool XlWineOnLinux { get; } = GetEnvironmentVariable("XL_WINEONLINUX");

        /// <summary>
        /// Gets a value indicating whether the DALAMUD_NOT_HAVE_PLUGINS setting has been enabled.
        /// </summary>
        public static bool DalamudNoPlugins { get; } = GetEnvironmentVariable("DALAMUD_NOT_HAVE_PLUGINS");

        /// <summary>
        /// Gets a value indicating whether the DalamudForceReloaded setting has been enabled.
        /// </summary>
        public static bool DalamudForceReloaded { get; } = GetEnvironmentVariable("DALAMUD_FORCE_RELOADED");

        /// <summary>
        /// Gets a value indicating whether the DalamudForceMinHook setting has been enabled.
        /// </summary>
        public static bool DalamudForceMinHook { get; } = GetEnvironmentVariable("DALAMUD_FORCE_MINHOOK");

        private static bool GetEnvironmentVariable(string name)
            => bool.Parse(Environment.GetEnvironmentVariable(name) ?? "false");
    }
}

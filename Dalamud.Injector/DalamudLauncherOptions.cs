using System;
using System.IO;

namespace Dalamud.Injector
{
    public sealed class DalamudLauncherOptions
    {
        public static DalamudLauncherOptions Default => new DalamudLauncherOptions
        {
            RootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dalamud"),
            BinaryDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dalamud", "bin"),
        };

        /// <summary>
        /// A directory to where Dalamud data is located.
        /// </summary>
        public string RootDirectory { get; set; } = "";

        /// <summary>
        /// A directory to where `Dalamud.dll` and its dependencies are located.
        /// </summary>
        /// <remarks>
        /// This path doesn't need to be the same directory as where the launcher is located.
        /// </remarks>
        public string BinaryDirectory { get; set; } = "";
    }
}

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
        /// 
        /// </summary>
        public string RootDirectory { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        public string BinaryDirectory { get; set; } = "";
    }
}

using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Injector
{
    /// <summary>
    /// User configuration for the AppContainer sandbox, read by the injector before launching the game.
    /// </summary>
    internal sealed class SandboxConfiguration
    {
        /// <summary>
        /// Gets or sets the AppContainer profile name.
        /// </summary>
        [JsonProperty("containerName")]
        public string ContainerName { get; set; } = "dalamud.game";

        /// <summary>
        /// Gets or sets the capabilities granted to the container token.
        /// </summary>
        [JsonProperty("capabilities")]
        public List<string> Capabilities { get; set; } = ["internetClient", "privateNetworkClientServer"];

        /// <summary>
        /// Gets or sets a value indicating whether the injector should try to allow localhost connections
        /// for the container.
        /// </summary>
        [JsonProperty("loopbackExempt")]
        public bool LoopbackExempt { get; set; }

        /// <summary>
        /// Gets or sets additional paths the sandboxed process may access.
        /// </summary>
        [JsonProperty("allowedPaths")]
        public List<SandboxAllowedPath> AllowedPaths { get; set; } = new();

        /// <summary>
        /// Load a configuration from the given path, falling back to defaults if it doesn't exist or can't be parsed.
        /// </summary>
        /// <param name="path">Path to the JSON configuration file.</param>
        /// <returns>The configuration.</returns>
        public static SandboxConfiguration Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var loaded = JsonConvert.DeserializeObject<SandboxConfiguration>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        Log.Information("[SANDBOX] Loaded sandbox configuration from {Path}", path);
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[SANDBOX] Could not read sandbox configuration at {Path}, using defaults", path);
            }

            return new SandboxConfiguration();
        }

        /// <summary>
        /// An additional path grant for the sandboxed process.
        /// </summary>
        internal sealed class SandboxAllowedPath
        {
            /// <summary>
            /// Gets or sets the path. Environment variables (%VAR%) are expanded.
            /// </summary>
            [JsonProperty("path")]
            public string Path { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets a value indicating whether write (modify) access is granted.
            /// </summary>
            [JsonProperty("write")]
            public bool Write { get; set; }
        }
    }
}

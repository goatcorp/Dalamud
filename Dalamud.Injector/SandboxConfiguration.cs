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
        /// Gets the default location of the sandbox configuration, read even when no sandbox arguments were
        /// passed on the command line.
        /// </summary>
        public static string DefaultPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher",
            "dalamudSandbox.json");

        /// <summary>
        /// Gets or sets a value indicating whether the game should be launched in the sandbox without command line
        /// arguments.
        /// </summary>
        [JsonProperty("enabledGlobally")]
        public bool EnabledGlobally { get; set; }

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
        /// Load a configuration from the given path, or defaults if mustExist is false.
        /// </summary>
        /// <param name="path">Path to the JSON configuration file.</param>
        /// <param name="mustExist">Whether a missing file is an error.</param>
        /// <returns>The configuration.</returns>
        public static SandboxConfiguration Load(string path, bool mustExist)
        {
            if (!File.Exists(path))
            {
                if (mustExist)
                    throw new FileNotFoundException($"No sandbox configuration at {path}.", path);

                Log.Verbose("[SANDBOX] No sandbox configuration at {Path}, using defaults", path);
                return new SandboxConfiguration();
            }

            SandboxConfiguration? loaded;
            try
            {
                loaded = JsonConvert.DeserializeObject<SandboxConfiguration>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    $"Could not read the sandbox configuration at {path}. " +
                    "Fix the file or pass --no-sandbox to launch without a sandbox.",
                    ex);
            }

            if (loaded == null)
            {
                throw new InvalidDataException(
                    $"The sandbox configuration at {path} is empty. " +
                    "Fix the file or pass --no-sandbox to launch without a sandbox.");
            }

            Log.Information("[SANDBOX] Loaded sandbox configuration from {Path}", path);
            return loaded;
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Dalamud.Data.LuminaExtensions;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiScene;
using JetBrains.Annotations;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Data
{
    /// <summary>
    /// This class provides data for Dalamud-internal features, but can also be used by plugins if needed.
    /// </summary>
    public sealed class DataManager : IDisposable
    {
        private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
        private readonly InterfaceManager interfaceManager;

        /// <summary>
        /// A <see cref="Lumina"/> object which gives access to any excel/game data.
        /// </summary>
        private GameData gameData;

        private Thread luminaResourceThread;
        private CancellationTokenSource luminaCancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataManager"/> class.
        /// </summary>
        /// <param name="language">The language to load data with by default.</param>
        /// <param name="interfaceManager">An <see cref="InterfaceManager"/> instance to parse the data with.</param>
        internal DataManager(ClientLanguage language, InterfaceManager interfaceManager)
        {
            this.interfaceManager = interfaceManager;

            // Set up default values so plugins do not null-reference when data is being loaded.
            this.ClientOpCodes = this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(new Dictionary<string, ushort>());

            this.Language = language;
        }

        /// <summary>
        /// Gets the current game client language.
        /// </summary>
        public ClientLanguage Language { get; private set; }

        /// <summary>
        /// Gets the OpCodes sent by the server to the client.
        /// </summary>
        public ReadOnlyDictionary<string, ushort> ServerOpCodes { get; private set; }

        /// <summary>
        /// Gets the OpCodes sent by the client to the server.
        /// </summary>
        [UsedImplicitly]
        public ReadOnlyDictionary<string, ushort> ClientOpCodes { get; private set; }

        /// <summary>
        /// Gets an <see cref="ExcelModule"/> object which gives access to any of the game's sheet data.
        /// </summary>
        public ExcelModule Excel => this.gameData?.Excel;

        /// <summary>
        /// Gets a value indicating whether Game Data is ready to be read.
        /// </summary>
        public bool IsDataReady { get; private set; }

        #region Lumina Wrappers

        /// <summary>
        /// Get an <see cref="ExcelSheet{T}"/> with the given Excel sheet row type.
        /// </summary>
        /// <typeparam name="T">The excel sheet type to get.</typeparam>
        /// <returns>The <see cref="ExcelSheet{T}"/>, giving access to game rows.</returns>
        public ExcelSheet<T> GetExcelSheet<T>() where T : ExcelRow
        {
            return this.Excel.GetSheet<T>();
        }

        /// <summary>
        /// Get an <see cref="ExcelSheet{T}"/> with the given Excel sheet row type with a specified language.
        /// </summary>
        /// <param name="language">Language of the sheet to get.</param>
        /// <typeparam name="T">The excel sheet type to get.</typeparam>
        /// <returns>The <see cref="ExcelSheet{T}"/>, giving access to game rows.</returns>
        public ExcelSheet<T> GetExcelSheet<T>(ClientLanguage language) where T : ExcelRow
        {
            var lang = language switch
            {
                ClientLanguage.Japanese => Lumina.Data.Language.Japanese,
                ClientLanguage.English => Lumina.Data.Language.English,
                ClientLanguage.German => Lumina.Data.Language.German,
                ClientLanguage.French => Lumina.Data.Language.French,
                _ => throw new ArgumentOutOfRangeException(nameof(language), $"Unknown Language: {language}"),
            };
            return this.Excel.GetSheet<T>(lang);
        }

        /// <summary>
        /// Get a <see cref="FileResource"/> with the given path.
        /// </summary>
        /// <param name="path">The path inside of the game files.</param>
        /// <returns>The <see cref="FileResource"/> of the file.</returns>
        public FileResource GetFile(string path)
        {
            return this.GetFile<FileResource>(path);
        }

        /// <summary>
        /// Get a <see cref="FileResource"/> with the given path, of the given type.
        /// </summary>
        /// <typeparam name="T">The type of resource.</typeparam>
        /// <param name="path">The path inside of the game files.</param>
        /// <returns>The <see cref="FileResource"/> of the file.</returns>
        public T GetFile<T>(string path) where T : FileResource
        {
            var filePath = GameData.ParseFilePath(path);
            if (filePath == null)
                return default;
            return this.gameData.Repositories.TryGetValue(filePath.Repository, out var repository) ? repository.GetFile<T>(filePath.Category, filePath) : default;
        }

        /// <summary>
        /// Check if the file with the given path exists within the game's index files.
        /// </summary>
        /// <param name="path">The path inside of the game files.</param>
        /// <returns>True if the file exists.</returns>
        public bool FileExists(string path)
        {
            return this.gameData.FileExists(path);
        }

        /// <summary>
        /// Get a <see cref="TexFile"/> containing the icon with the given ID.
        /// </summary>
        /// <param name="iconId">The icon ID.</param>
        /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
        public TexFile GetIcon(int iconId)
        {
            return this.GetIcon(this.Language, iconId);
        }

        /// <summary>
        /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given language.
        /// </summary>
        /// <param name="iconLanguage">The requested language.</param>
        /// <param name="iconId">The icon ID.</param>
        /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
        public TexFile GetIcon(ClientLanguage iconLanguage, int iconId)
        {
            var type = iconLanguage switch
            {
                ClientLanguage.Japanese => "ja/",
                ClientLanguage.English => "en/",
                ClientLanguage.German => "de/",
                ClientLanguage.French => "fr/",
                _ => throw new ArgumentOutOfRangeException(nameof(iconLanguage), $"Unknown Language: {iconLanguage}"),
            };

            return this.GetIcon(type, iconId);
        }

        /// <summary>
        /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given type.
        /// </summary>
        /// <param name="type">The type of the icon (e.g. 'hq' to get the HQ variant of an item icon).</param>
        /// <param name="iconId">The icon ID.</param>
        /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
        public TexFile GetIcon(string type, int iconId)
        {
            type ??= string.Empty;
            if (type.Length > 0 && !type.EndsWith("/"))
                type += "/";

            var filePath = string.Format(IconFileFormat, iconId / 1000, type, iconId);
            var file = this.GetFile<TexFile>(filePath);

            if (file != default(TexFile) || type.Length <= 0) return file;

            // Couldn't get specific type, try for generic version.
            filePath = string.Format(IconFileFormat, iconId / 1000, string.Empty, iconId);
            file = this.GetFile<TexFile>(filePath);
            return file;
        }

        /// <summary>
        /// Get the passed <see cref="TexFile"/> as a drawable ImGui TextureWrap.
        /// </summary>
        /// <param name="tex">The Lumina <see cref="TexFile"/>.</param>
        /// <returns>A <see cref="TextureWrap"/> that can be used to draw the texture.</returns>
        public TextureWrap GetImGuiTexture(TexFile tex)
            => this.interfaceManager.LoadImageRaw(tex.GetRgbaImageData(), tex.Header.Width, tex.Header.Height, 4);

        /// <summary>
        /// Get the passed texture path as a drawable ImGui TextureWrap.
        /// </summary>
        /// <param name="path">The internal path to the texture.</param>
        /// <returns>A <see cref="TextureWrap"/> that can be used to draw the texture.</returns>
        public TextureWrap GetImGuiTexture(string path)
            => this.GetImGuiTexture(this.GetFile<TexFile>(path));

        /// <summary>
        /// Get a <see cref="TextureWrap"/> containing the icon with the given ID, of the given language.
        /// </summary>
        /// <param name="iconLanguage">The requested language.</param>
        /// <param name="iconId">The icon ID.</param>
        /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
        public TextureWrap GetImGuiTextureIcon(ClientLanguage iconLanguage, int iconId)
            => this.GetImGuiTexture(this.GetIcon(iconLanguage, iconId));

        /// <summary>
        /// Get a <see cref="TextureWrap"/> containing the icon with the given ID, of the given type.
        /// </summary>
        /// <param name="type">The type of the icon (e.g. 'hq' to get the HQ variant of an item icon).</param>
        /// <param name="iconId">The icon ID.</param>
        /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
        public TextureWrap GetImGuiTextureIcon(string type, int iconId)
            => this.GetImGuiTexture(this.GetIcon(type, iconId));

        #endregion

        /// <summary>
        /// Dispose this DataManager.
        /// </summary>
        public void Dispose()
        {
            this.luminaCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Initialize this data manager.
        /// </summary>
        /// <param name="baseDir">The directory to load data from.</param>
        internal void Initialize(string baseDir)
        {
            try
            {
                Log.Verbose("Starting data load...");

                var zoneOpCodeDict = JsonConvert.DeserializeObject<Dictionary<string, ushort>>(
                    File.ReadAllText(Path.Combine(baseDir, "UIRes", "serveropcode.json")));
                this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(zoneOpCodeDict);

                Log.Verbose("Loaded {0} ServerOpCodes.", zoneOpCodeDict.Count);

                var clientOpCodeDict = JsonConvert.DeserializeObject<Dictionary<string, ushort>>(
                    File.ReadAllText(Path.Combine(baseDir, "UIRes", "clientopcode.json")));
                this.ClientOpCodes = new ReadOnlyDictionary<string, ushort>(clientOpCodeDict);

                Log.Verbose("Loaded {0} ClientOpCodes.", clientOpCodeDict.Count);

                var luminaOptions = new LuminaOptions
                {
                    CacheFileResources = true,

#if DEBUG
                    PanicOnSheetChecksumMismatch = true,
#else
                    PanicOnSheetChecksumMismatch = false,
#endif

                    DefaultExcelLanguage = this.Language switch
                    {
                        ClientLanguage.Japanese => Lumina.Data.Language.Japanese,
                        ClientLanguage.English => Lumina.Data.Language.English,
                        ClientLanguage.German => Lumina.Data.Language.German,
                        ClientLanguage.French => Lumina.Data.Language.French,
                        _ => throw new ArgumentOutOfRangeException(nameof(this.Language), $"Unknown Language: {this.Language}"),
                    },
                };

                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    this.gameData = new GameData(Path.Combine(Path.GetDirectoryName(processModule.FileName), "sqpack"), luminaOptions);
                }

                Log.Information("Lumina is ready: {0}", this.gameData.DataPath);

                this.IsDataReady = true;

                this.luminaCancellationTokenSource = new();

                var luminaCancellationToken = this.luminaCancellationTokenSource.Token;
                this.luminaResourceThread = new(() =>
                {
                    while (!luminaCancellationToken.IsCancellationRequested)
                    {
                        if (this.gameData.FileHandleManager.HasPendingFileLoads)
                        {
                            this.gameData.ProcessFileHandleQueue();
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
                    }
                });
                this.luminaResourceThread.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not download data.");
            }
        }
    }
}

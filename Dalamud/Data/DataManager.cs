using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

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
    public class DataManager : IDisposable
    {
        private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";

        /// <summary>
        /// A <see cref="Lumina"/> object which gives access to any excel/game data.
        /// </summary>
        private Lumina.GameData gameData;

        private ClientLanguage language;

        private Thread luminaResourceThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataManager"/> class.
        /// </summary>
        /// <param name="language">The language to load data with by default.</param>
        public DataManager(ClientLanguage language)
        {
            // Set up default values so plugins do not null-reference when data is being loaded.
            this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(new Dictionary<string, ushort>());

            this.language = language;
        }

        /// <summary>
        /// Gets the OpCodes sent by the server to the client.
        /// </summary>
        public ReadOnlyDictionary<string, ushort> ServerOpCodes { get; private set; }

        /// <summary>
        /// Gets the OpCodes sent by the client to the server.
        /// </summary>
        public ReadOnlyDictionary<string, ushort> ClientOpCodes { get; private set; }

        /// <summary>
        /// Gets an <see cref="ExcelModule"/> object which gives access to any of the game's sheet data.
        /// </summary>
        public ExcelModule Excel => this.gameData?.Excel;

        /// <summary>
        /// Gets a value indicating whether Game Data is ready to be read.
        /// </summary>
        public bool IsDataReady { get; private set; }

        /// <summary>
        /// Initialize this data manager.
        /// </summary>
        /// <param name="baseDir">The directory to load data from.</param>
        public void Initialize(string baseDir)
        {
            try
            {
                Log.Verbose("Starting data load...");

                var zoneOpCodeDict =
                    JsonConvert.DeserializeObject<Dictionary<string, ushort>>(File.ReadAllText(Path.Combine(baseDir, "UIRes", "serveropcode.json")));
                this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(zoneOpCodeDict);

                Log.Verbose("Loaded {0} ServerOpCodes.", zoneOpCodeDict.Count);

                var clientOpCodeDict =
                    JsonConvert.DeserializeObject<Dictionary<string, ushort>>(File.ReadAllText(Path.Combine(baseDir, "UIRes", "clientopcode.json")));
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

                    DefaultExcelLanguage = this.language switch {
                        ClientLanguage.Japanese => Language.Japanese,
                        ClientLanguage.English => Language.English,
                        ClientLanguage.German => Language.German,
                        ClientLanguage.French => Language.French,
                        _ => throw new ArgumentOutOfRangeException(
                                 nameof(this.language),
                                 "Unknown Language: " + this.language),
                    },
                };

                this.gameData = new GameData(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "sqpack"), luminaOptions);

                Log.Information("Lumina is ready: {0}", this.gameData.DataPath);

                this.IsDataReady = true;

                this.luminaResourceThread = new Thread(() =>
                {
                    while (true)
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

                    // ReSharper disable once FunctionNeverReturns
                });
                this.luminaResourceThread.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not download data.");
            }
        }

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
            var lang = language switch {
                ClientLanguage.Japanese => Language.Japanese,
                ClientLanguage.English => Language.English,
                ClientLanguage.German => Language.German,
                ClientLanguage.French => Language.French,
                _ => throw new ArgumentOutOfRangeException(nameof(this.language), "Unknown Language: " + this.language)
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
            ParsedFilePath filePath = GameData.ParseFilePath(path);
            if (filePath == null)
                return default(T);
            Repository repository;
            return this.gameData.Repositories.TryGetValue(filePath.Repository, out repository) ? repository.GetFile<T>(filePath.Category, filePath) : default(T);
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
            return this.GetIcon(this.language, iconId);
        }

        /// <summary>
        /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given language.
        /// </summary>
        /// <param name="iconLanguage">The requested language.</param>
        /// <param name="iconId">The icon ID.</param>
        /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
        public TexFile GetIcon(ClientLanguage iconLanguage, int iconId)
        {
            var type = iconLanguage switch {
                ClientLanguage.Japanese => "ja/",
                ClientLanguage.English => "en/",
                ClientLanguage.German => "de/",
                ClientLanguage.French => "fr/",
                _ => throw new ArgumentOutOfRangeException(nameof(this.language), "Unknown Language: " + this.language)
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

        #endregion

        /// <summary>
        /// Dispose this DataManager.
        /// </summary>
        public void Dispose()
        {
            this.luminaResourceThread.Abort();
        }
    }
}

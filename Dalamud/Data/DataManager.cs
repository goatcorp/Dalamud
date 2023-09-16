using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Timing;
using JetBrains.Annotations;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Data;

/// <summary>
/// This class provides data for Dalamud-internal features, but can also be used by plugins if needed.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IDataManager>]
#pragma warning restore SA1015
internal sealed class DataManager : IDisposable, IServiceType, IDataManager
{
    private readonly Thread luminaResourceThread;
    private readonly CancellationTokenSource luminaCancellationTokenSource;

    [ServiceManager.ServiceConstructor]
    private DataManager(DalamudStartInfo dalamudStartInfo, Dalamud dalamud)
    {
        this.Language = dalamudStartInfo.Language;

        // Set up default values so plugins do not null-reference when data is being loaded.
        this.ClientOpCodes =
            this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(new Dictionary<string, ushort>());

        var baseDir = dalamud.AssetDirectory.FullName;
        try
        {
            Log.Verbose("Starting data load...");

            var zoneOpCodeDict = JsonConvert.DeserializeObject<Dictionary<string, ushort>>(
                File.ReadAllText(Path.Combine(baseDir, "UIRes", "serveropcode.json")))!;
            this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(zoneOpCodeDict);

            Log.Verbose("Loaded {0} ServerOpCodes.", zoneOpCodeDict.Count);

            var clientOpCodeDict = JsonConvert.DeserializeObject<Dictionary<string, ushort>>(
                File.ReadAllText(Path.Combine(baseDir, "UIRes", "clientopcode.json")))!;
            this.ClientOpCodes = new ReadOnlyDictionary<string, ushort>(clientOpCodeDict);

            Log.Verbose("Loaded {0} ClientOpCodes.", clientOpCodeDict.Count);

            using (Timings.Start("Lumina Init"))
            {
                var luminaOptions = new LuminaOptions
                {
                    LoadMultithreaded = true,
                    CacheFileResources = true,
#if NEVER // Lumina bug
                    PanicOnSheetChecksumMismatch = true,
#else
                    PanicOnSheetChecksumMismatch = false,
#endif
                    DefaultExcelLanguage = this.Language.ToLumina(),
                };

                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    this.GameData = new GameData(Path.Combine(Path.GetDirectoryName(processModule.FileName)!, "sqpack"),
                                                 luminaOptions);
                }
                else
                {
                    throw new Exception("Could not main module.");
                }

                Log.Information("Lumina is ready: {0}", this.GameData.DataPath);

                try
                {
                    var tsInfo =
                        JsonConvert.DeserializeObject<LauncherTroubleshootingInfo>(
                            dalamudStartInfo.TroubleshootingPackData);
                    this.HasModifiedGameDataFiles =
                        tsInfo?.IndexIntegrity is LauncherTroubleshootingInfo.IndexIntegrityResult.Failed
                            or LauncherTroubleshootingInfo.IndexIntegrityResult.Exception;
                }
                catch
                {
                    // ignored
                }
            }

            this.IsDataReady = true;

            this.luminaCancellationTokenSource = new();

            var luminaCancellationToken = this.luminaCancellationTokenSource.Token;
            this.luminaResourceThread = new(() =>
            {
                while (!luminaCancellationToken.IsCancellationRequested)
                {
                    if (this.GameData.FileHandleManager.HasPendingFileLoads)
                    {
                        this.GameData.ProcessFileHandleQueue();
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

    /// <inheritdoc/>
    public ClientLanguage Language { get; private set; }

    /// <inheritdoc/>
    public GameData GameData { get; private set; }

    /// <inheritdoc/>
    public ExcelModule Excel => this.GameData.Excel;

    /// <inheritdoc/>
    public bool HasModifiedGameDataFiles { get; private set; }

    /// <summary>
    /// Gets a value indicating whether Game Data is ready to be read.
    /// </summary>
    internal bool IsDataReady { get; private set; }

    /// <summary>
    /// Gets a list of server opcodes from DalamudAssets. NOT FOR PLUGIN USE - USE HOOKS INSTEAD!.
    /// </summary>
    [Obsolete("Opcodes should no longer be used.")]
    internal ReadOnlyDictionary<string, ushort> ServerOpCodes { get; private set; }

    /// <summary>
    /// Gets a list of client opcodes from DalamudAssets. NOT FOR PLUGIN USE - USE HOOKS INSTEAD!.
    /// </summary>
    [UsedImplicitly]
    [Obsolete("Opcodes should no longer be used.")]
    internal ReadOnlyDictionary<string, ushort> ClientOpCodes { get; private set; }


    #region Lumina Wrappers

    /// <inheritdoc/>
    public ExcelSheet<T>? GetExcelSheet<T>() where T : ExcelRow
        => this.Excel.GetSheet<T>();

    /// <inheritdoc/>
    public ExcelSheet<T>? GetExcelSheet<T>(ClientLanguage language) where T : ExcelRow
        => this.Excel.GetSheet<T>(language.ToLumina());

    /// <inheritdoc/>
    public FileResource? GetFile(string path)
        => this.GetFile<FileResource>(path);

    /// <inheritdoc/>
    public T? GetFile<T>(string path) where T : FileResource
    {
        var filePath = GameData.ParseFilePath(path);
        if (filePath == null)
            return default;
        return this.GameData.Repositories.TryGetValue(filePath.Repository, out var repository)
                   ? repository.GetFile<T>(filePath.Category, filePath)
                   : default;
    }

    /// <inheritdoc/>
    public bool FileExists(string path)
        => this.GameData.FileExists(path);

    #endregion

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        this.luminaCancellationTokenSource.Cancel();
    }

    private class LauncherTroubleshootingInfo
    {
        public enum IndexIntegrityResult
        {
            Failed,
            Exception,
            NoGame,
            ReferenceNotFound,
            ReferenceFetchFailure,
            Success,
        }

        public IndexIntegrityResult IndexIntegrity { get; set; }
    }
}

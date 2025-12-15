using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
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
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IDataManager>]
#pragma warning restore SA1015
internal sealed class DataManager : IInternalDisposableService, IDataManager
{
    private readonly Thread luminaResourceThread;
    private readonly CancellationTokenSource luminaCancellationTokenSource;
    private readonly RsvResolver rsvResolver;

    [ServiceManager.ServiceConstructor]
    private DataManager(Dalamud dalamud)
    {
        this.Language = (ClientLanguage)dalamud.StartInfo.Language;

        this.rsvResolver = new();

        try
        {
            Log.Verbose("Starting data load...");

            using (Timings.Start("Lumina Init"))
            {
                var luminaOptions = new LuminaOptions
                {
                    LoadMultithreaded = true,
                    CacheFileResources = true,
                    PanicOnSheetChecksumMismatch = true,
                    RsvResolver = this.rsvResolver.TryResolve,
                    DefaultExcelLanguage = this.Language.ToLumina(),
                };

                try
                {
                    this.GameData = new(
                        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "sqpack"),
                        luminaOptions)
                    {
                        StreamPool = new(),
                    };
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lumina GameData init failed");
                    Util.Fatal(
                        "Dalamud could not read required game data files. This likely means your game installation is corrupted or incomplete.\n\n" +
                        "Please repair your installation by right-clicking the login button in XIVLauncher and choosing \"Repair game files\".",
                        "Dalamud");

                    return;
                }

                Log.Information("Lumina is ready: {0}", this.GameData.DataPath);

                if (!dalamud.StartInfo.TroubleshootingPackData.IsNullOrEmpty())
                {
                    try
                    {
                        var tsInfo =
                            JsonConvert.DeserializeObject<LauncherTroubleshootingInfo>(
                                dalamud.StartInfo.TroubleshootingPackData);

                        // Don't fail for IndexIntegrityResult.Exception, since the check during launch has a very small timeout
                        // this.HasModifiedGameDataFiles =
                        //     tsInfo?.IndexIntegrity is LauncherTroubleshootingInfo.IndexIntegrityResult.Failed;

                        // TODO: Put above back when check in XL is fixed
                        this.HasModifiedGameDataFiles = false;

                        if (this.HasModifiedGameDataFiles)
                            Log.Verbose("Game data integrity check failed!\n{TsData}", dalamud.StartInfo.TroubleshootingPackData);
                    }
                    catch
                    {
                        // ignored
                    }
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
            Log.Error(ex, "Could not initialize Lumina");
            throw;
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

    #region Lumina Wrappers

    /// <inheritdoc/>
    public ExcelSheet<T> GetExcelSheet<T>(ClientLanguage? language = null, string? name = null) where T : struct, IExcelRow<T>
        => this.Excel.GetSheet<T>(language?.ToLumina(), name);

    /// <inheritdoc/>
    public SubrowExcelSheet<T> GetSubrowExcelSheet<T>(ClientLanguage? language = null, string? name = null) where T : struct, IExcelSubrow<T>
        => this.Excel.GetSubrowSheet<T>(language?.ToLumina(), name);

    /// <inheritdoc/>
    public FileResource? GetFile(string path)
        => this.GetFile<FileResource>(path);

    /// <inheritdoc/>
    public T? GetFile<T>(string path) where T : FileResource
    {
        var filePath = GameData.ParseFilePath(path);
        if (filePath == null)
            return default;
        return this.GameData.Repositories.TryGetValue(filePath.Repository, out var repository) ? repository.GetFile<T>(filePath.Category, filePath) : default;
    }

    /// <inheritdoc/>
    public Task<T> GetFileAsync<T>(string path, CancellationToken cancellationToken) where T : FileResource =>
        GameData.ParseFilePath(path) is { } filePath &&
        this.GameData.Repositories.TryGetValue(filePath.Repository, out var repository)
            ? Task.Run(
                () => repository.GetFile<T>(filePath.Category, filePath) ?? throw new FileNotFoundException(
                          "Failed to load file, most likely because the file could not be found."),
                cancellationToken)
            : Task.FromException<T>(new FileNotFoundException("The file could not be found."));

    /// <inheritdoc/>
    public bool FileExists(string path)
        => this.GameData.FileExists(path);

    #endregion

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.luminaCancellationTokenSource.Cancel();
        this.GameData.Dispose();
        this.rsvResolver.Dispose();
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

        public IndexIntegrityResult? IndexIntegrity { get; set; }
    }
}

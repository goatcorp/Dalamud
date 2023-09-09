using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using Dalamud.Interface.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
using ImGuiScene;
using JetBrains.Annotations;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Tex.Buffers;
using Lumina.Excel;
using Newtonsoft.Json;
using Serilog;
using SharpDX.DXGI;

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
public sealed class DataManager : IDisposable, IServiceType, IDataManager
{
    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string HighResolutionIconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";

    private readonly Thread luminaResourceThread;
    private readonly CancellationTokenSource luminaCancellationTokenSource;

    [ServiceManager.ServiceConstructor]
    private DataManager(DalamudStartInfo dalamudStartInfo, Dalamud dalamud)
    {
        this.Language = dalamudStartInfo.Language;

        // Set up default values so plugins do not null-reference when data is being loaded.
        this.ClientOpCodes = this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(new Dictionary<string, ushort>());

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
                    this.GameData = new GameData(Path.Combine(Path.GetDirectoryName(processModule.FileName)!, "sqpack"), luminaOptions);
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
                        tsInfo?.IndexIntegrity is LauncherTroubleshootingInfo.IndexIntegrityResult.Failed or LauncherTroubleshootingInfo.IndexIntegrityResult.Exception;
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
    public ReadOnlyDictionary<string, ushort> ServerOpCodes { get; private set; }

    /// <inheritdoc/>
    [UsedImplicitly]
    public ReadOnlyDictionary<string, ushort> ClientOpCodes { get; private set; }

    /// <inheritdoc/>
    public GameData GameData { get; private set; }

    /// <inheritdoc/>
    public ExcelModule Excel => this.GameData.Excel;

    /// <inheritdoc/>
    public bool IsDataReady { get; private set; }

    /// <inheritdoc/>
    public bool HasModifiedGameDataFiles { get; private set; }

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
        return this.GameData.Repositories.TryGetValue(filePath.Repository, out var repository) ? repository.GetFile<T>(filePath.Category, filePath) : default;
    }

    /// <inheritdoc/>
    public bool FileExists(string path) 
        => this.GameData.FileExists(path);

    /// <summary>
    /// Get a <see cref="TexFile"/> containing the icon with the given ID.
    /// </summary>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(uint iconId) 
        => this.GetIcon(this.Language, iconId, false);

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(uint iconId, bool highResolution)
        => this.GetIcon(this.Language, iconId, highResolution);

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(bool isHq, uint iconId)
    {
        var type = isHq ? "hq/" : string.Empty;
        return this.GetIcon(type, iconId);
    }

    /// <summary>
    /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given language.
    /// </summary>
    /// <param name="iconLanguage">The requested language.</param>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(ClientLanguage iconLanguage, uint iconId)
        => this.GetIcon(iconLanguage, iconId, false);

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(ClientLanguage iconLanguage, uint iconId, bool highResolution)
    {
        var type = iconLanguage switch
        {
            ClientLanguage.Japanese => "ja/",
            ClientLanguage.English => "en/",
            ClientLanguage.German => "de/",
            ClientLanguage.French => "fr/",
            ClientLanguage.Korean => "ko/",
            _ => throw new ArgumentOutOfRangeException(nameof(iconLanguage), $"Unknown Language: {iconLanguage}"),
        };

        return this.GetIcon(type, iconId, highResolution);
    }

    /// <summary>
    /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given type.
    /// </summary>
    /// <param name="type">The type of the icon (e.g. 'hq' to get the HQ variant of an item icon).</param>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(string? type, uint iconId)
        => this.GetIcon(type, iconId, false);

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(string? type, uint iconId, bool highResolution)
    {
        var format = highResolution ? HighResolutionIconFileFormat : IconFileFormat;
        
        type ??= string.Empty;
        if (type.Length > 0 && !type.EndsWith("/"))
            type += "/";

        var filePath = string.Format(format, iconId / 1000, type, iconId);
        var file = this.GetFile<TexFile>(filePath);

        if (type == string.Empty || file != default)
            return file;

        // Couldn't get specific type, try for generic version.
        filePath = string.Format(format, iconId / 1000, string.Empty, iconId);
        file = this.GetFile<TexFile>(filePath);
        return file;
    }

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetHqIcon(uint iconId)
        => this.GetIcon(true, iconId);

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    [return: NotNullIfNotNull(nameof(tex))]
    public TextureWrap? GetImGuiTexture(TexFile? tex)
    {
        if (tex is null)
            return null;

        var im = Service<InterfaceManager>.Get();
        var buffer = tex.TextureBuffer;
        var bpp = 1 << (((int)tex.Header.Format & (int)TexFile.TextureFormat.BppMask) >>
                        (int)TexFile.TextureFormat.BppShift);

        var (dxgiFormat, conversion) = TexFile.GetDxgiFormatFromTextureFormat(tex.Header.Format, false);
        if (conversion != TexFile.DxgiFormatConversion.NoConversion || !im.SupportsDxgiFormat((Format)dxgiFormat))
        {
            dxgiFormat = (int)Format.B8G8R8A8_UNorm;
            buffer = buffer.Filter(0, 0, TexFile.TextureFormat.B8G8R8A8);
            bpp = 32;
        }

        var pitch = buffer is BlockCompressionTextureBuffer
                        ? Math.Max(1, (buffer.Width + 3) / 4) * 2 * bpp
                        : ((buffer.Width * bpp) + 7) / 8;
        return im.LoadImageFromDxgiFormat(buffer.RawData, pitch, buffer.Width, buffer.Height, (Format)dxgiFormat);
    }
    
    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTexture(string path)
        => this.GetImGuiTexture(this.GetFile<TexFile>(path));

    /// <summary>
    /// Get a <see cref="TextureWrap"/> containing the icon with the given ID.
    /// </summary>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
    /// TODO(v9): remove in api9 in favor of GetImGuiTextureIcon(uint iconId, bool highResolution)
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(uint iconId) 
        => this.GetImGuiTexture(this.GetIcon(iconId, false));

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(uint iconId, bool highResolution)
        => this.GetImGuiTexture(this.GetIcon(iconId, highResolution));

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(bool isHq, uint iconId)
        => this.GetImGuiTexture(this.GetIcon(isHq, iconId));

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(ClientLanguage iconLanguage, uint iconId)
        => this.GetImGuiTexture(this.GetIcon(iconLanguage, iconId));

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(string type, uint iconId)
        => this.GetImGuiTexture(this.GetIcon(type, iconId));

    /// <inheritdoc/>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureHqIcon(uint iconId)
        => this.GetImGuiTexture(this.GetHqIcon(iconId));

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

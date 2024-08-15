using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;

using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Rsv;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides data for Dalamud-internal features, but can also be used by plugins if needed.
/// </summary>
public interface IDataManager
{
    /// <summary>
    /// Gets the current game client language.
    /// </summary>
    public ClientLanguage Language { get; }

    /// <summary>
    /// Gets a <see cref="Lumina"/> object which gives access to any excel/game data.
    /// </summary>
    public GameData GameData { get; }

    /// <summary>
    /// Gets an <see cref="ExcelModule"/> object which gives access to any of the game's sheet data.
    /// </summary>
    public ExcelModule Excel { get; }

    /// <summary>
    /// Gets a <see cref="IRsvProvider"/> object which gives access to all resolved RSV strings that the game has seen so far.
    /// </summary>
    public IRsvProvider RsvProvider { get; }

    /// <summary>
    /// Gets a value indicating whether the game data files have been modified by another third-party tool.
    /// </summary>
    public bool HasModifiedGameDataFiles { get; }

    /// <summary>
    /// Get an <see cref="ExcelSheet{T}"/> with the given Excel sheet row type.
    /// </summary>
    /// <typeparam name="T">The excel sheet type to get.</typeparam>
    /// <returns>The <see cref="ExcelSheet{T}"/>, giving access to game rows.</returns>
    public ExcelSheet<T> GetExcelSheet<T>() where T : struct, IExcelRow<T>;

    /// <summary>
    /// Get an <see cref="ExcelSheet{T}"/> with the given Excel sheet row type with a specified language.
    /// </summary>
    /// <param name="language">Language of the sheet to get.</param>
    /// <typeparam name="T">The excel sheet type to get.</typeparam>
    /// <returns>The <see cref="ExcelSheet{T}"/>, giving access to game rows.</returns>
    public ExcelSheet<T> GetExcelSheet<T>(ClientLanguage language) where T : struct, IExcelRow<T>;

    /// <summary>
    /// Get a <see cref="FileResource"/> with the given path.
    /// </summary>
    /// <param name="path">The path inside of the game files.</param>
    /// <returns>The <see cref="FileResource"/> of the file.</returns>
    public FileResource? GetFile(string path);

    /// <summary>
    /// Get a <see cref="FileResource"/> with the given path, of the given type.
    /// </summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    /// <param name="path">The path inside of the game files.</param>
    /// <returns>The <see cref="FileResource"/> of the file.</returns>
    public T? GetFile<T>(string path) where T : FileResource;

    /// <summary>
    /// Get a <see cref="FileResource"/> with the given path, of the given type.
    /// </summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    /// <param name="path">The path inside of the game files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the <see cref="FileResource"/> of the file on success.
    /// </returns>
    public Task<T> GetFileAsync<T>(string path, CancellationToken cancellationToken) where T : FileResource;

    /// <summary>
    /// Check if the file with the given path exists within the game's index files.
    /// </summary>
    /// <param name="path">The path inside of the game files.</param>
    /// <returns>True if the file exists.</returns>
    public bool FileExists(string path);
}

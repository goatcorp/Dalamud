using Dalamud.Data;

using Lumina.Excel;

namespace Dalamud.Game.ClientState.Resolvers;

/// <summary>
/// This object resolves a rowID within an Excel sheet.
/// </summary>
/// <typeparam name="T">The type of Lumina sheet to resolve.</typeparam>
public class ExcelResolver<T> where T : ExcelRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelResolver{T}"/> class.
    /// </summary>
    /// <param name="id">The ID of the classJob.</param>
    internal ExcelResolver(uint id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID to be resolved.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets GameData linked to this excel row.
    /// </summary>
    public T? GameData => Service<DataManager>.Get().GetExcelSheet<T>()?.GetRow(this.Id);

    /// <summary>
    /// Gets GameData linked to this excel row with the specified language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The ExcelRow in the specified language.</returns>
    public T? GetWithLanguage(ClientLanguage language) => Service<DataManager>.Get().GetExcelSheet<T>(language)?.GetRow(this.Id);
}

using Lumina.Excel;

namespace Dalamud.Data;

/// <summary>
/// A helper class to easily resolve Lumina data within Dalamud.
/// </summary>
internal static class LuminaUtils
{
    private static ExcelModule Module => Service<DataManager>.Get().Excel;

    /// <summary>
    /// Initializes a new instance of the <see cref="RowRef{T}"/> class using the default <see cref="ExcelModule"/>.
    /// </summary>
    /// <typeparam name="T">The type of Lumina sheet to resolve.</typeparam>
    /// <param name="rowId">The id of the row to resolve.</param>
    /// <returns>A new <see cref="RowRef{T}"/> object.</returns>
    public static RowRef<T> CreateRef<T>(uint rowId) where T : struct, IExcelRow<T>
    {
        return new(Module, rowId);
    }
}

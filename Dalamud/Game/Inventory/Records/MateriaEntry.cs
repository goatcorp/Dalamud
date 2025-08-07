using Dalamud.Data;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Game.Inventory.Records;

/// <summary>
/// A record to hold easy information about a given piece of Materia.
/// </summary>
public record MateriaEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MateriaEntry"/> class.
    /// </summary>
    /// <param name="typeId">The ID of the materia.</param>
    /// <param name="gradeValue">The grade of the materia.</param>
    public MateriaEntry(ushort typeId, byte gradeValue)
    {
        this.Type = LuminaUtils.CreateRef<Materia>(typeId);
        this.Grade = LuminaUtils.CreateRef<MateriaGrade>(gradeValue);
    }

    /// <summary>
    /// Gets the Lumina row for this Materia.
    /// </summary>
    public RowRef<Materia> Type { get; }

    /// <summary>
    /// Gets the Lumina row for this Materia's grade.
    /// </summary>
    public RowRef<MateriaGrade> Grade { get; }

    /// <summary>
    /// Checks if this MateriaEntry is valid.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    internal bool IsValid()
    {
        return this.Type.IsValid && this.Grade.IsValid;
    }
}

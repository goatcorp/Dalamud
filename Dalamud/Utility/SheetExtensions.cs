using Dalamud.Excel.Sheets;

namespace Dalamud.Utility;

#pragma warning disable SA1101 // Prefix local calls with this

/// <summary>
/// Extensions for <see cref="Excel.Sheets"/>.
/// </summary>
public static class SheetExtensions
{
    extension(Level row)
    {
        /// <summary>
        /// Converts the Level row into a <see cref="WorldCoordinate"/>.
        /// </summary>
        /// <returns>The <see cref="WorldCoordinate"/> of this Level row.</returns>
        public WorldCoordinate ToWorldCoordinate()
        {
            return new WorldCoordinate(
                row.Territory.RowId,
                row.Map.RowId,
                row.X,
                row.Y,
                row.Z);
        }

        /// <summary>
        /// Converts the Level row into a <see cref="MapCoordinate"/>.
        /// </summary>
        /// <returns>The <see cref="MapCoordinate"/> of this Level row.</returns>
        public MapCoordinate ToMapCoordinate()
        {
            return row.ToWorldCoordinate().ToMapCoordinate();
        }
    }

    extension(Map row)
    {
        /// <summary>
        /// Converts a raw world X position to a user-friendly map X coordinate.
        /// </summary>
        /// <param name="x">The raw X world position.</param>
        /// <returns>The calculated map X coordinate.</returns>
        public uint ToMapCoordX(float x)
        {
            return row.ToMapCoord(row.OffsetX, x);
        }

        /// <summary>
        /// Converts a raw world position to a user-friendly map Y coordinate.
        /// </summary>
        /// <param name="y">The raw coordinate position representing the map's Y-axis.</param>
        /// <returns>The calculated map Y coordinate.</returns>
        /// <remarks>
        /// In FFXIV's 3D coordinate system, <c>Y</c> represents altitude/elevation, while <c>Z</c> represents north-south depth.<br/>
        /// Pass the world position's <b>Z coordinate</b> into this method to calculate the 2D map's Y coordinate.
        /// </remarks>
        public uint ToMapCoordY(float y)
        {
            return row.ToMapCoord(row.OffsetY, y);
        }

        /// <summary>
        /// Converts a raw world coordinate to a 1-41 map coordinate value.
        /// </summary>
        /// <param name="offset">The coordinate offset (X or Y offset from map data).</param>
        /// <param name="value">The raw coordinate position value to convert.</param>
        /// <returns>The calculated integer map coordinate value.</returns>
        private uint ToMapCoord(short offset, float value)
        {
            var scale = row.SizeFactor / 100.0f;
            return (uint)(10 - (int)((((value + offset) * scale) + 1024f) * -0.2f / scale));
        }
    }
}

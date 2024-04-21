using System.Numerics;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Utility;

/// <summary>
/// Utility helper class for game maps and coordinate translations that don't require state.
///
/// The conversion methods were found in 89 54 24 10 56 41 55 41 56 48 81 EC, which itself was found by looking for
/// uses of AddonText 1631.
/// </summary>
public static class MapUtil
{
    /// <summary>
    /// Helper method to convert one of the game's Vector3 X/Z provided by the game to a map coordinate suitable for
    /// display to the player.
    /// </summary>
    /// <param name="value">The raw float of a game Vector3 X or Z coordinate to convert.</param>
    /// <param name="scale">The scale factor of the map, generally retrieved from Lumina.</param>
    /// <param name="offset">The dimension offset for either X or Z, generally retrieved from Lumina.</param>
    /// <returns>Returns a converted float for display to the player.</returns>
    public static float ConvertWorldCoordXZToMapCoord(float value, uint scale, int offset)
    {
        // Derived from E8 ?? ?? ?? ?? 0F B7 4B 1C and simplified.

        return (0.02f * offset) + (2048f / scale) + (0.02f * value) + 1.0f;
    }

    /// <summary>
    /// Helper method to convert a game Vector3 Y coordinate to a map coordinate suitable for display to the player.
    /// </summary>
    /// <param name="value">The raw float of a game Vector3 Y coordinate to convert.</param>
    /// <param name="zOffset">The zOffset for this map. Retrieved from TerritoryTypeTransient.</param>
    /// <param name="correctZOffset">Optionally enable Z offset correction. When a Z offset of -10,000 is set, replace
    /// it with 0 for calculation purposes to show a more sane Z coordinate.</param>
    /// <returns>Returns a converted float for display to the player.</returns>
    public static float ConvertWorldCoordYToMapCoord(float value, int zOffset, bool correctZOffset = false)
    {
        // Derived from 48 83 EC 38 80 3D ?? ?? ?? ?? ?? 0F 29 74 24

        // zOffset of -10000 indicates that the map should not display a Z coordinate.
        if (zOffset == -10000 && correctZOffset) zOffset = 0;

        return (value - zOffset) / 100;
    }

    /// <summary>
    /// All-in-one helper method to convert a World Coordinate (internal to the game) to a Map Coordinate (visible to
    /// players in the minimap/elsewhere).
    /// </summary>
    /// <remarks>
    /// Note that this method will swap Y and Z in the resulting Vector3 to appropriately reflect the game's display.
    /// </remarks>
    /// <param name="worldCoordinates">A Vector3 of raw World coordinates from the game.</param>
    /// <param name="xOffset">The offset to apply to the incoming X parameter, generally Lumina's Map.OffsetX.</param>
    /// <param name="yOffset">The offset to apply to the incoming Y parameter, generally Lumina's Map.OffsetY.</param>
    /// <param name="zOffset">The offset to apply to the incoming Z parameter, generally Lumina's TerritoryTypeTransient.OffsetZ.</param>
    /// <param name="scale">The global scale to apply to the incoming X and Y parameters, generally Lumina's Map.SizeFactor.</param>
    /// <param name="correctZOffset">An optional mode to "correct" a Z offset of -10000 to be a more human-friendly value.</param>
    /// <returns>Returns a Vector3 representing visible map coordinates.</returns>
    public static Vector3 WorldToMap(
        Vector3 worldCoordinates,
        int xOffset = 0,
        int yOffset = 0,
        int zOffset = 0,
        uint scale = 100,
        bool correctZOffset = false)
    {
        return new Vector3(
            ConvertWorldCoordXZToMapCoord(worldCoordinates.X, scale, xOffset),
            ConvertWorldCoordXZToMapCoord(worldCoordinates.Z, scale, yOffset),
            ConvertWorldCoordYToMapCoord(worldCoordinates.Y, zOffset, correctZOffset));
    }

    /// <summary>
    /// All-in-one helper method to convert a World Coordinate (internal to the game) to a Map Coordinate (visible to
    /// players in the minimap/elsewhere).
    /// </summary>
    /// <remarks>
    /// Note that this method will swap Y and Z to appropriately reflect the game's display.
    /// </remarks>
    /// <param name="worldCoordinates">A Vector3 of raw World coordinates from the game.</param>
    /// <param name="map">A Lumina map to use for offset/scale information.</param>
    /// <param name="territoryTransient">A TerritoryTypeTransient to use for Z offset information.</param>
    /// <param name="correctZOffset">An optional mode to "correct" a Z offset of -10000 to be a more human-friendly value.</param>
    /// <returns>Returns a Vector3 representing visible map coordinates.</returns>
    public static Vector3 WorldToMap(
        Vector3 worldCoordinates, Map map, TerritoryTypeTransient territoryTransient, bool correctZOffset = false)
    {
        return WorldToMap(
            worldCoordinates,
            map.OffsetX,
            map.OffsetY,
            territoryTransient.OffsetZ,
            map.SizeFactor,
            correctZOffset);
    }

    /// <summary>
    /// All-in-one helper method to convert a World Coordinate (internal to the game) to a Map Coordinate (visible to
    /// players in the minimap/elsewhere).
    /// </summary>
    /// <param name="worldCoordinates">A Vector2 of raw World coordinates from the game.</param>
    /// <param name="xOffset">The offset to apply to the incoming X parameter, generally Lumina's Map.OffsetX.</param>
    /// <param name="yOffset">The offset to apply to the incoming Y parameter, generally Lumina's Map.OffsetY.</param>
    /// <param name="scale">The global scale to apply to the incoming X and Y parameters, generally Lumina's Map.SizeFactor.</param>
    /// <returns>Returns a Vector2 representing visible map coordinates.</returns>
    public static Vector2 WorldToMap(
        Vector2 worldCoordinates,
        int xOffset = 0,
        int yOffset = 0,
        uint scale = 100)
    {
        return new Vector2(
            ConvertWorldCoordXZToMapCoord(worldCoordinates.X, scale, xOffset),
            ConvertWorldCoordXZToMapCoord(worldCoordinates.Y, scale, yOffset));
    }

    /// <summary>
    /// All-in-one helper method to convert a World Coordinate (internal to the game) to a Map Coordinate (visible to
    /// players in the minimap/elsewhere).
    /// </summary>
    /// <param name="worldCoordinates">A Vector2 of raw World coordinates from the game.</param>
    /// <param name="map">A Lumina map to use for offset/scale information.</param>
    /// <returns>Returns a Vector2 representing visible map coordinates.</returns>
    public static Vector2 WorldToMap(Vector2 worldCoordinates, Map map)
    {
        return WorldToMap(worldCoordinates, map.OffsetX, map.OffsetY, map.SizeFactor);
    }

    /// <summary>
    /// Extension method to get the current position of a GameObject in Map Coordinates (visible to players in the
    /// minimap or chat). A Z (height) value will always be returned, even on maps that do not natively show one.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if ClientState is unavailable.</exception>
    /// <param name="go">The GameObject to get the position for.</param>
    /// <param name="correctZOffset">Whether to "correct" a Z offset to sane values for maps that don't have one.</param>
    /// <returns>A Vector3 that represents the X (east/west), Y (north/south), and Z (height) position of this object.</returns>
    public static unsafe Vector3 GetMapCoordinates(this GameObject go, bool correctZOffset = false)
    {
        var agentMap = AgentMap.Instance();

        if (agentMap == null || agentMap->CurrentMapId == 0)
            throw new InvalidOperationException("Could not determine active map - data may not be loaded yet?");

        var territoryTransient = Service<DataManager>.Get()
                                                     .GetExcelSheet<TerritoryTypeTransient>()!
                                                     .GetRow(agentMap->CurrentTerritoryId);

        return WorldToMap(
            go.Position,
            agentMap->CurrentOffsetX,
            agentMap->CurrentOffsetY,
            territoryTransient?.OffsetZ ?? 0,
            (uint)agentMap->CurrentMapSizeFactor,
            correctZOffset);
    }
}

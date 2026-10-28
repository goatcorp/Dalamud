using System.Numerics;

using Dalamud.Data;
using Dalamud.Excel.Sheets;
using Dalamud.Game.Text;

using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// Represents world-space coordinate within a specific territory, map, and instance.
/// </summary>
public struct WorldCoordinate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldCoordinate"/> struct using raw coordinate values.
    /// </summary>
    /// <param name="territoryId">The TerritoryType RowId.</param>
    /// <param name="mapId">The Map RowId.<br/>If set to 0, falls back to <see cref="TerritoryType.Map"/> from the given <paramref name="territoryId"/>.</param>
    /// <param name="x">The world X coordinate.</param>
    /// <param name="y">The world Y coordinate. This is the altitude of the object.</param>
    /// <param name="z">The world Z coordinate.</param>
    /// <param name="instanceId">The Instance Id.</param>
    public WorldCoordinate(uint territoryId, uint mapId, float x, float y, float z, uint instanceId = 0)
    {
        this.TerritoryId = territoryId;
        this.MapId = mapId;
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.InstanceId = instanceId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldCoordinate"/> struct using a <see cref="Vector3"/> position.
    /// </summary>
    /// <param name="territoryId">The TerritoryType RowId.</param>
    /// <param name="mapId">The Map RowId.<br/>If set to 0, falls back to <see cref="TerritoryType.Map"/> from the given <paramref name="territoryId"/>.</param>
    /// <param name="pos">The 3D vector position, where Y is the altitude.</param>
    /// <param name="instanceId">The Instance Id.</param>
    public WorldCoordinate(uint territoryId, uint mapId, Vector3 pos, uint instanceId = 0)
        : this(territoryId, mapId, pos.X, pos.Y, pos.Z, instanceId)
    {
    }

    /// <summary>
    /// Gets or sets the TerritoryType RowId.
    /// </summary>
    public uint TerritoryId { get; set; }

    /// <summary>
    /// Gets or sets the Map RowId.<br/>
    /// If set to 0, falls back to <see cref="TerritoryType.Map"/> from the given <see cref="TerritoryId"/>.
    /// </summary>
    public uint MapId { get; set; }

    /// <summary>
    /// Gets or sets the world X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the world Y coordinate. This is the altitude.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the world Z coordinate.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Gets or sets the Instance Id.
    /// </summary>
    public uint InstanceId { get; set; }

    /// <summary>
    /// Converts this world-space coordinate into the corresponding map-space coordinate.
    /// </summary>
    /// <returns>
    /// A <see cref="MapCoordinate"/> representing the equivalent position on the map,
    /// or a default value if the map data cannot be retrieved.
    /// </returns>
    public MapCoordinate ToMapCoordinate()
    {
        var dataManager = Service<DataManager>.Get();

        var mapId = MapUtil.ResolveMapId(this.TerritoryId, this.MapId);
        if (mapId == 0 || !dataManager.GetExcelSheet<Map>().TryGetRow(mapId, out var mapRow))
            return default;

        float? z = null;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (this.Y != MapUtil.NoRawZ && MapUtil.TryGetOffsetZ(this.TerritoryId, out var offsetZ))
        {
            z = MapUtil.ConvertWorldCoordYToMapCoord(this.Y, offsetZ);
        }

        return new MapCoordinate(
            this.TerritoryId,
            this.MapId,
            MapUtil.ConvertWorldCoordXZToMapCoord(this.X, mapRow.SizeFactor, mapRow.OffsetX),
            MapUtil.ConvertWorldCoordXZToMapCoord(this.Z, mapRow.SizeFactor, mapRow.OffsetY),
            z,
            this.InstanceId);
    }

    /// <inheritdoc cref="SeString.CreateMapLink(WorldCoordinate, uint)"/>
    public ReadOnlySeString ToMapLink()
    {
        return SeString.CreateMapLink(this);
    }
}

/// <summary>
/// Represents map-space coordinate within a specific territory, map, and instance.
/// </summary>
public struct MapCoordinate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapCoordinate"/> struct.
    /// </summary>
    /// <param name="territoryId">The TerritoryType RowId.</param>
    /// <param name="mapId">The Map RowId.<br/>If set to 0, falls back to <see cref="TerritoryType.Map"/> from the given <paramref name="territoryId"/>.</param>
    /// <param name="x">The map X coordinate.</param>
    /// <param name="y">The map Y coordinate.</param>
    /// <param name="z">An optional map Z coordinate. This is the altitude.</param>
    /// <param name="instanceId">The Instance Id.</param>
    public MapCoordinate(uint territoryId, uint mapId, float x, float y, float? z = null, uint instanceId = 0)
    {
        this.TerritoryId = territoryId;
        this.MapId = mapId;
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.InstanceId = instanceId;
    }

    /// <summary>
    /// Gets or sets the TerritoryType RowId.
    /// </summary>
    public uint TerritoryId { get; set; }

    /// <summary>
    /// Gets or sets the Map RowId.<br/>
    /// If set to 0, falls back to <see cref="TerritoryType.Map"/> from the given <see cref="TerritoryId"/>.
    /// </summary>
    public uint MapId { get; set; }

    /// <summary>
    /// Gets or sets the map X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the map Y coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the optional map Z coordinate. This is the altitude.
    /// </summary>
    public float? Z { get; set; }

    /// <summary>
    /// Gets or sets the Instance Id.
    /// </summary>
    public uint InstanceId { get; set; }

    /// <summary>
    /// Converts this map-space coordinate into the corresponding, approximate world-space coordinate.
    /// </summary>
    /// <returns>
    /// A <see cref="WorldCoordinate"/> representing the approximate position in the world,
    /// or a default value if the map data cannot be retrieved.
    /// </returns>
    public WorldCoordinate ToWorldCoordinate()
    {
        var mapId = MapUtil.ResolveMapId(this.TerritoryId, this.MapId);
        if (mapId == 0 || !Service<DataManager>.Get().GetExcelSheet<Map>().TryGetRow(mapId, out var mapRow))
            return default;

        float y = MapUtil.NoRawZ;
        if (this.Z.HasValue)
        {
            MapUtil.TryGetOffsetZ(this.TerritoryId, out var offsetZ);
            y = MathF.Round(this.Z.Value * 100f, 2) + offsetZ;
        }

        return new WorldCoordinate(
            this.TerritoryId,
            this.MapId,
            MapUtil.ConvertMapCoordXYToWorldCoord(this.X, mapRow.SizeFactor, mapRow.OffsetX),
            y,
            MapUtil.ConvertMapCoordXYToWorldCoord(this.Y, mapRow.SizeFactor, mapRow.OffsetY),
            this.InstanceId);
    }

    /// <inheritdoc cref="SeString.CreateMapLink(MapCoordinate)"/>
    public ReadOnlySeString ToMapLink()
    {
        return SeString.CreateMapLink(this);
    }
}

/// <summary>
/// Utility helper class for game maps and coordinate translations that don't require state.
///
/// The conversion methods were found in 89 54 24 10 56 41 55 41 56 48 81 EC, which itself was found by looking for
/// uses of AddonText 1631.
/// </summary>
public static class MapUtil
{
    /// <summary>
    /// A raw Z coordinate of -30000 indicates that it has no value.
    /// </summary>
    public const int NoRawZ = -30000;

    /// <summary>
    /// A <see cref="TerritoryTypeTransient.OffsetZ"/> value of -10000 indicates that the map position should not display a Z coordinate.
    /// </summary>
    public const int NoOffsetZ = -10000;

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
    /// Helper method to convert one of the map's X/Y coordinate to a world coordinate.
    /// </summary>
    /// <param name="value">The coordinate value.</param>
    /// <param name="scale">The SizeFactor value of the Map row.</param>
    /// <param name="offset">The OffsetX/OffsetY value of the Map row.</param>
    /// <returns>A 3D world coordinate.</returns>
    public static float ConvertMapCoordXYToWorldCoord(float value, float scale, short offset)
    {
        return (value - 1.0f - (0.02f * offset) - (2048.0f / scale)) * 50.0f;
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
        if (zOffset == NoOffsetZ && correctZOffset) zOffset = 0;

        return (value - zOffset) / 100;
    }

    /// <summary>
    /// Helper method to apply <see cref="TerritoryTypeTransient.OffsetZ"/> to a Y (altitude) value of a coordinate.
    /// </summary>
    /// <param name="territoryId">The TerritoryType Id.</param>
    /// <param name="y">The y-coordinate value.</param>
    /// <returns>An adjusted altitude.</returns>
    public static int ApplyTerritoryOffsetToWorldCoordianteY(uint territoryId, float y)
    {
        if (!Service<DataManager>.Get().GetExcelSheet<TerritoryTypeTransient>().TryGetRow(territoryId, out var transientRow) || transientRow.OffsetZ == NoOffsetZ)
            return MapUtil.NoRawZ;

        var adjustedY = y - transientRow.OffsetZ;
        var result = (int)adjustedY;
        if (adjustedY < 0.0f && adjustedY != result)
            result -= 10;
        return result;
    }

    /// <summary>
    /// Helper method to remove the <see cref="TerritoryTypeTransient.OffsetZ"/> from a Y (altitude) value of a coordinate.
    /// </summary>
    /// <param name="territoryId">The TerritoryType Id.</param>
    /// <param name="y">The y-coordinate value.</param>
    /// <returns>An adjusted altitude.</returns>
    public static float RemoveTerritoryOffsetFromWorldCoordinateY(uint territoryId, float y)
    {
        if (!Service<DataManager>.Get().GetExcelSheet<TerritoryTypeTransient>().TryGetRow(territoryId, out var transientRow) || transientRow.OffsetZ == NoOffsetZ)
            return y;

        if (y < 0.0f)
            y += 10;
        return y + transientRow.OffsetZ;
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
    /// Resolves the Map Id for a given TerritoryType Id, or returns the provided default Map Id if specified.
    /// </summary>
    /// <param name="territoryTypeId">The Id of the TerritoryType to look up in the Excel sheet.</param>
    /// <param name="defaultMapId">An optional fallback Map Id. If non-zero, this value is returned immediately.</param>
    /// <returns>The resolved Map Id from the TerritoryType sheet, or the default Map Id.</returns>
    internal static uint ResolveMapId(uint territoryTypeId, uint defaultMapId = 0)
    {
        if (defaultMapId != 0)
            return defaultMapId;

        if (territoryTypeId == 0)
            return 0;

        return Service<DataManager>.Get().GetExcelSheet<TerritoryType>().TryGetRow(territoryTypeId, out var territoryRow)
            ? territoryRow.Map.RowId
            : 0;
    }

    /// <summary>
    /// Gets the <see cref="TerritoryTypeTransient.OffsetZ"/> of a territory that displays a Z coordinate.
    /// </summary>
    /// <param name="territoryTypeId">The TerritoryType Id.</param>
    /// <param name="offsetZ">The OffsetZ, or 0 if the territory displays no Z coordinate.</param>
    /// <returns>Whether the territory displays a Z coordinate.</returns>
    internal static bool TryGetOffsetZ(uint territoryTypeId, out int offsetZ)
    {
        offsetZ = 0;

        if (!Service<DataManager>.Get().GetExcelSheet<TerritoryTypeTransient>().TryGetRow(territoryTypeId, out var transientRow) || transientRow.OffsetZ == NoOffsetZ)
        {
            return false;
        }

        offsetZ = transientRow.OffsetZ;
        return true;
    }
}

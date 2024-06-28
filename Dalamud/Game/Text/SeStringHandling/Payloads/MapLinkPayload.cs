using System.Collections.Generic;
using System.IO;

using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing an interactable map position link.
/// </summary>
public class MapLinkPayload : Payload
{
    private Map map;
    private TerritoryType territoryType;
    private string placeNameRegion;
    private string placeName;

    [JsonProperty]
    private uint territoryTypeId;

    [JsonProperty]
    private uint mapId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapLinkPayload"/> class.
    /// Creates an interactable MapLinkPayload from a human-readable position.
    /// </summary>
    /// <param name="territoryTypeId">The id of the TerritoryType entry for this link.</param>
    /// <param name="mapId">The id of the Map entry for this link.</param>
    /// <param name="niceXCoord">The human-readable x-coordinate for this link.</param>
    /// <param name="niceYCoord">The human-readable y-coordinate for this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    public MapLinkPayload(uint territoryTypeId, uint mapId, float niceXCoord, float niceYCoord, float fudgeFactor = 0.05f)
    {
        this.territoryTypeId = territoryTypeId;
        this.mapId = mapId;
        // this fudge is necessary basically to ensure we don't shift down a full tenth
        // because essentially values are truncated instead of rounded, so 3.09999f will become
        // 3.0f and not 3.1f
        this.RawX = this.ConvertMapCoordinateToRawPosition(niceXCoord + fudgeFactor, this.Map.SizeFactor, this.Map.OffsetX);
        this.RawY = this.ConvertMapCoordinateToRawPosition(niceYCoord + fudgeFactor, this.Map.SizeFactor, this.Map.OffsetY);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapLinkPayload"/> class.
    /// Creates an interactable MapLinkPayload from a raw position.
    /// </summary>
    /// <param name="territoryTypeId">The id of the TerritoryType entry for this link.</param>
    /// <param name="mapId">The id of the Map entry for this link.</param>
    /// <param name="rawX">The internal raw x-coordinate for this link.</param>
    /// <param name="rawY">The internal raw y-coordinate for this link.</param>
    public MapLinkPayload(uint territoryTypeId, uint mapId, int rawX, int rawY)
    {
        this.territoryTypeId = territoryTypeId;
        this.mapId = mapId;
        this.RawX = rawX;
        this.RawY = rawY;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapLinkPayload"/> class.
    /// Creates an interactable MapLinkPayload from a human-readable position.
    /// </summary>
    internal MapLinkPayload()
    {
    }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.MapLink;

    /// <summary>
    /// Gets the Map specified for this map link.
    /// </summary>
    /// <remarks>
    /// The value is evaluated lazily and cached.
    /// </remarks>
    [JsonIgnore]
    public Map Map => this.map ??= this.DataResolver.GetExcelSheet<Map>().GetRow(this.mapId);

    /// <summary>
    /// Gets the TerritoryType specified for this map link.
    /// </summary>
    /// <remarks>
    /// The value is evaluated lazily and cached.
    /// </remarks>
    [JsonIgnore]
    public TerritoryType TerritoryType => this.territoryType ??= this.DataResolver.GetExcelSheet<TerritoryType>().GetRow(this.territoryTypeId);

    /// <summary>
    /// Gets the internal x-coordinate for this map position.
    /// </summary>
    public int RawX { get; private set; }

    /// <summary>
    /// Gets the internal y-coordinate for this map position.
    /// </summary>
    public int RawY { get; private set; }

    // these could be cached, but this isn't really too egregious

    /// <summary>
    /// Gets the readable x-coordinate position for this map link.  This value is approximate and unrounded.
    /// </summary>
    public float XCoord => this.ConvertRawPositionToMapCoordinate(this.RawX, this.Map.SizeFactor, this.Map.OffsetX);

    /// <summary>
    /// Gets the readable y-coordinate position for this map link.  This value is approximate and unrounded.
    /// </summary>
    [JsonIgnore]
    public float YCoord => this.ConvertRawPositionToMapCoordinate(this.RawY, this.Map.SizeFactor, this.Map.OffsetY);

    // there is no Z; it's purely in the text payload where applicable

    /// <summary>
    /// Gets the printable map coordinates for this link.  This value tries to match the in-game printable text as closely
    /// as possible but is an approximation and may be slightly off for some positions.
    /// </summary>
    [JsonIgnore]
    public string CoordinateString
    {
        get
        {
            // this truncates the values to one decimal without rounding, which is what the game does
            // the fudge also just attempts to correct the truncated/displayed value for rounding/fp issues
            // TODO: should this fudge factor be the same as in the ctor? currently not since that is customizable
            const float fudge = 0.02f;
            var x = Math.Truncate((this.XCoord + fudge) * 10.0f) / 10.0f;
            var y = Math.Truncate((this.YCoord + fudge) * 10.0f) / 10.0f;

            // the formatting and spacing the game uses
            var clientState = Service<ClientState.ClientState>.Get();
            return clientState.ClientLanguage switch
            {
                ClientLanguage.German => $"( {x:0.0}, {y:0.0} )",
                ClientLanguage.Japanese => $"({x:0.0}, {y:0.0})",
                _ => $"( {x:0.0}  , {y:0.0} )",
            };
        }
    }

    /// <summary>
    /// Gets the region name for this map link. This corresponds to the upper zone name found in the actual in-game map UI. eg, "La Noscea".
    /// </summary>
    [JsonIgnore]
    public string PlaceNameRegion => this.placeNameRegion ??= this.TerritoryType.PlaceNameRegion.Value?.Name;

    /// <summary>
    /// Gets the place name for this map link. This corresponds to the lower zone name found in the actual in-game map UI. eg, "Limsa Lominsa Upper Decks".
    /// </summary>
    [JsonIgnore]
    public string PlaceName => this.placeName ??= this.TerritoryType.PlaceName.Value?.Name;

    /// <summary>
    /// Gets the data string for this map link, for use by internal game functions that take a string variant and not a binary payload.
    /// </summary>
    public string DataString => $"m:{this.TerritoryType.RowId},{this.Map.RowId},{this.RawX},{this.RawY}";

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - TerritoryTypeId: {this.territoryTypeId}, MapId: {this.mapId}, RawX: {this.RawX}, RawY: {this.RawY}, display: {this.PlaceName} {this.CoordinateString}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var packedTerritoryAndMapBytes = MakePackedInteger(this.territoryTypeId, this.mapId);
        var xBytes = MakeInteger(unchecked((uint)this.RawX));
        var yBytes = MakeInteger(unchecked((uint)this.RawY));

        var chunkLen = 4 + packedTerritoryAndMapBytes.Length + xBytes.Length + yBytes.Length;

        var bytes = new List<byte>()
        {
            START_BYTE,
            (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.MapPositionLink,
        };

        bytes.AddRange(packedTerritoryAndMapBytes);
        bytes.AddRange(xBytes);
        bytes.AddRange(yBytes);

        // unk
        bytes.AddRange(new byte[] { 0xFF, 0x01, END_BYTE });

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        // for debugging for now
        var oldPos = reader.BaseStream.Position;
        var bytes = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position));
        reader.BaseStream.Position = oldPos;

        try
        {
            (this.territoryTypeId, this.mapId) = GetPackedIntegers(reader);
            this.RawX = unchecked((int)GetInteger(reader));
            this.RawY = unchecked((int)GetInteger(reader));
            // the Z coordinate is never in this chunk, just the text (if applicable)

            // seems to always be FF 01
            reader.ReadBytes(2);
        }
        catch (NotSupportedException)
        {
            Serilog.Log.Information($"Unsupported map bytes {BitConverter.ToString(bytes).Replace("-", " ")}");
            // we still want to break here for now, or we'd just throw again later
            throw;
        }
    }

    #region ugliness

    // from https://github.com/xivapi/ffxiv-datamining/blob/master/docs/MapCoordinates.md
    // from https://github.com/xivapi/xivapi-mappy/blob/master/Mappy/Helpers/MapHelper.cs
    // the raw scale from the map needs to be scaled down by a factor of 100
    // the raw pos also needs to be scaled down by a factor of 1000
    // the tile scale is ~50, but is exactly 2048/41, more info in the md file
    private float ConvertRawPositionToMapCoordinate(int pos, float scale, short offset)
    {
        // extra 1/1000 because that is how the network ints are done
        const float networkAdjustment = 1f;

        // scaling
        var trueScale = scale / 100f;
        var truePos = pos / 1000f;

        var computedPos = (truePos + offset) * trueScale;
        // pretty weird formula, but obviously has something to do with the tile scaling
        return (41f / trueScale * ((computedPos + 1024f) / 2048f)) + networkAdjustment;
    }

    // Created as the inverse of ConvertRawPositionToMapCoordinate(), since no one seemed to have a version of that
    private int ConvertMapCoordinateToRawPosition(float pos, float scale, short offset)
    {
        const float networkAdjustment = 1f;

        // scaling
        var trueScale = scale / 100f;

        var num2 = (((pos - networkAdjustment) * trueScale / 41f * 2048f) - 1024f) / trueScale;
        // (pos - offset) / scale, with the scaling on num2 done before for precision
        num2 *= 1000f;
        return (int)num2 - (offset * 1000);
    }

    #endregion
}

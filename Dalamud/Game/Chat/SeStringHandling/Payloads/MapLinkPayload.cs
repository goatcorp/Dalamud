using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Data;
using Newtonsoft.Json;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload representing an interactable map position link.
    /// </summary>
    public class MapLinkPayload : Payload
    {
        public override PayloadType Type => PayloadType.MapLink;

        private Map map;
        /// <summary>
        /// The Map specified for this map link.
        /// </summary>
        /// <remarks>
        /// Value is evaluated lazily and cached.
        /// </remarks>
        [JsonIgnore]
        public Map Map
        {
            get
            {
                this.map ??= this.DataResolver.GetExcelSheet<Map>().GetRow(this.mapId);
                return this.map;
            }
        }

        private TerritoryType territoryType;
        /// <summary>
        /// The TerritoryType specified for this map link.
        /// </summary>
        /// <remarks>
        /// Value is evaluated lazily and cached.
        /// </remarks>
        [JsonIgnore]
        public TerritoryType TerritoryType
        {
            get
            {
                this.territoryType ??= this.DataResolver.GetExcelSheet<TerritoryType>().GetRow(this.territoryTypeId);
                return this.territoryType;
            }
        }

        /// <summary>
        /// The internal x-coordinate for this map position.
        /// </summary>
        public int RawX { get; private set; }

        /// <summary>
        /// The internal y-coordinate for this map position.
        /// </summary>
        public int RawY { get; private set; }

        // these could be cached, but this isn't really too egregious
        /// <summary>
        /// The readable x-coordinate position for this map link.  This value is approximate and unrounded.
        /// </summary>
        public float XCoord
        {
            get
            {
                return ConvertRawPositionToMapCoordinate(RawX, Map.SizeFactor);
            }
        }

        /// <summary>
        /// The readable y-coordinate position for this map link.  This value is approximate and unrounded.
        /// </summary>
        [JsonIgnore]
        public float YCoord
        {
            get
            {
                return ConvertRawPositionToMapCoordinate(RawY, Map.SizeFactor);
            }
        }

        /// <summary>
        /// The printable map coordinates for this link.  This value tries to match the in-game printable text as closely as possible
        /// but is an approximation and may be slightly off for some positions.
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
                var x = Math.Truncate((XCoord+fudge) * 10.0f) / 10.0f;
                var y = Math.Truncate((YCoord+fudge) * 10.0f) / 10.0f;

                // the formatting and spacing the game uses
                return $"( {x.ToString("0.0")}  , {y.ToString("0.0")} )";
            }
        }

        private string placeNameRegion;
        /// <summary>
        /// The region name for this map link.  This corresponds to the upper zone name found in the actual in-game map UI.  eg, "La Noscea"
        /// </summary>
        [JsonIgnore]
        public string PlaceNameRegion
        {
            get
            {
                this.placeNameRegion ??= TerritoryType.PlaceNameRegion.Value?.Name;
                return this.placeNameRegion;
            }
        }

        private string placeName;
        /// <summary>
        /// The place name for this map link.  This corresponds to the lower zone name found in the actual in-game map UI.  eg, "Limsa Lominsa Upper Decks"
        /// </summary>
        [JsonIgnore]
        public string PlaceName
        {
            get
            {
                this.placeName ??= TerritoryType.PlaceName.Value?.Name;
                return this.placeName;
            }
        }

        /// <summary>
        /// The data string for this map link, for use by internal game functions that take a string variant and not a binary payload.
        /// </summary>
        public string DataString => $"m:{TerritoryType.RowId},{Map.RowId},{RawX},{RawY}";

        [JsonProperty]
        private uint territoryTypeId;

        [JsonProperty]
        private uint mapId;
        // there is no Z; it's purely in the text payload where applicable

        internal MapLinkPayload() { }

        /// <summary>
        /// Creates an interactable MapLinkPayload from a human-readable position.
        /// </summary>
        /// <param name="data">DataManager instance needed to resolve game data.</param>
        /// <param name="territoryTypeId">The id of the TerritoryType entry for this link.</param>
        /// <param name="mapId">The id of the Map entry for this link.</param>
        /// <param name="niceXCoord">The human-readable x-coordinate for this link.</param>
        /// <param name="niceYCoord">The human-readable y-coordinate for this link.</param>
        /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
        public MapLinkPayload(DataManager data, uint territoryTypeId, uint mapId, float niceXCoord, float niceYCoord, float fudgeFactor = 0.05f) {
            this.DataResolver = data;
            this.territoryTypeId = territoryTypeId;
            this.mapId = mapId;
            // this fudge is necessary basically to ensure we don't shift down a full tenth
            // because essentially values are truncated instead of rounded, so 3.09999f will become
            // 3.0f and not 3.1f
            RawX = this.ConvertMapCoordinateToRawPosition(niceXCoord + fudgeFactor, Map.SizeFactor);
            RawY = this.ConvertMapCoordinateToRawPosition(niceYCoord + fudgeFactor, Map.SizeFactor);
        }

        /// <summary>
        /// Creates an interactable MapLinkPayload from a raw position.
        /// </summary>
        /// <param name="data">DataManager instance needed to resolve game data.</param>
        /// <param name="territoryTypeId">The id of the TerritoryType entry for this link.</param>
        /// <param name="mapId">The id of the Map entry for this link.</param>
        /// <param name="rawX">The internal raw x-coordinate for this link.</param>
        /// <param name="rawY">The internal raw y-coordinate for this link.</param>
        public MapLinkPayload(DataManager data, uint territoryTypeId, uint mapId, int rawX, int rawY)
        {
            this.DataResolver = data;
            this.territoryTypeId = territoryTypeId;
            this.mapId = mapId;
            RawX = rawX;
            RawY = rawY;
        }

        public override string ToString()
        {
            return $"{Type} - TerritoryTypeId: {territoryTypeId}, MapId: {mapId}, RawX: {RawX}, RawY: {RawY}, display: {PlaceName} {CoordinateString}";
        }

        protected override byte[] EncodeImpl()
        {
            var packedTerritoryAndMapBytes = MakePackedInteger(this.territoryTypeId, this.mapId);
            var xBytes = MakeInteger(unchecked((uint)RawX));
            var yBytes = MakeInteger(unchecked((uint)RawY));

            var chunkLen = 4 + packedTerritoryAndMapBytes.Length + xBytes.Length + yBytes.Length;

            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.MapPositionLink
            };

            bytes.AddRange(packedTerritoryAndMapBytes);
            bytes.AddRange(xBytes);
            bytes.AddRange(yBytes);

            // unk
            bytes.AddRange(new byte[] { 0xFF, 0x01, END_BYTE });

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            // for debugging for now
            var oldPos = reader.BaseStream.Position;
            var bytes = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position));
            reader.BaseStream.Position = oldPos;

            try
            {
                (this.territoryTypeId, this.mapId) = GetPackedIntegers(reader);
                RawX = unchecked((int)GetInteger(reader));
                RawY = unchecked((int)GetInteger(reader));
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
        // extra 1/1000 because that is how the network ints are done
        private float ConvertRawPositionToMapCoordinate(int pos, float scale)
        {
            var c = scale / 100.0f;
            var scaledPos = pos * c / 1000.0f;

            return ((41.0f / c) * ((scaledPos + 1024.0f) / 2048.0f)) + 1.0f;
        }

        // Created as the inverse of ConvertRawPositionToMapCoordinate(), since no one seemed to have a version of that
        private int ConvertMapCoordinateToRawPosition(float pos, float scale)
        {
            var c = scale / 100.0f;

            var scaledPos = ((((pos - 1.0f) * c / 41.0f) * 2048.0f) - 1024.0f) / c;
            scaledPos *= 1000.0f;

            return (int)scaledPos;
        }
        #endregion

        protected override byte GetMarkerForIntegerBytes(byte[] bytes)
        {
            var type = bytes.Length switch
            {
                3 => (byte)IntegerType.Int24Special,                              // used because seen in incoming data
                2 => (byte)IntegerType.Int16,
                1 => (byte)IntegerType.None,                                      // single bytes seem to have no prefix at all here
                _ => base.GetMarkerForIntegerBytes(bytes)
            };

            return type;
        }
    }
}

using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class MapLinkPayload : Payload
    {
        public override PayloadType Type => PayloadType.MapLink;

        private Map map;
        public Map Map
        {
            get
            {
                this.map ??= this.dataResolver.GetExcelSheet<Map>().GetRow((int)this.mapId);
                return this.map;
            }
        }

        private TerritoryType territoryType;
        public TerritoryType TerritoryType
        {
            get
            {
                this.territoryType ??= this.dataResolver.GetExcelSheet<TerritoryType>().GetRow((int)this.territoryTypeId);
                return this.territoryType;
            }
        }

        // these could be cached, but this isn't really too egregious
        public float XCoord
        {
            get
            {
                return ConvertRawPositionToMapCoordinate(this.rawX, Map.SizeFactor);
            }
        }

        public float YCoord
        {
            get
            {
                return ConvertRawPositionToMapCoordinate(this.rawY, Map.SizeFactor);
            }
        }

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

                return $"( {x.ToString("0.0")}  , {y.ToString("0.0")} )";
            }
        }

        private string placeNameRegion;
        public string PlaceNameRegion
        {
            get
            {
                this.placeNameRegion ??= this.dataResolver.GetExcelSheet<PlaceName>().GetRow(TerritoryType.PlaceNameRegion).Name;
                return this.placeNameRegion;
            }
        }

        private string placeName;
        public string PlaceName
        {
            get
            {
                this.placeName ??= this.dataResolver.GetExcelSheet<PlaceName>().GetRow(TerritoryType.PlaceName).Name;
                return this.placeName;
            }
        }

        public string DataString => $"m:{TerritoryType.RowId},{Map.RowId},{rawX},{rawY}";

        private uint territoryTypeId;
        private uint mapId;
        private int rawX;
        private int rawY;
        // there is no Z; it's purely in the text payload where applicable

        internal MapLinkPayload() { }

        public MapLinkPayload(uint territoryTypeId, uint mapId, float niceXCoord, float niceYCoord, float fudgeFactor = 0.05f)
        {
            this.territoryTypeId = territoryTypeId;
            this.mapId = mapId;
            // this fudge is necessary basically to ensure we don't shift down a full tenth
            // because essentially values are truncated instead of rounded, so 3.09999f will become
            // 3.0f and not 3.1f
            this.rawX = this.ConvertMapCoordinateToRawPosition(niceXCoord + fudgeFactor, Map.SizeFactor);
            this.rawY = this.ConvertMapCoordinateToRawPosition(niceYCoord + fudgeFactor, Map.SizeFactor);
        }

        public MapLinkPayload(uint territoryTypeId, uint mapId, int rawX, int rawY)
        {
            this.territoryTypeId = territoryTypeId;
            this.mapId = mapId;
            this.rawX = rawX;
            this.rawY = rawY;
        }

        public override string ToString()
        {
            return $"{Type} - TerritoryTypeId: {territoryTypeId}, MapId: {mapId}, RawX: {rawX}, RawY: {rawY}";
        }

        protected override byte[] EncodeImpl()
        {
            var packedTerritoryAndMapBytes = MakePackedInteger(this.territoryTypeId, this.mapId);
            var xBytes = MakeInteger(unchecked((uint)this.rawX));
            var yBytes = MakeInteger(unchecked((uint)this.rawY));

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
                this.rawX = unchecked((int)GetInteger(reader));
                this.rawY = unchecked((int)GetInteger(reader));
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

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

        public string DataString => $"m:{TerritoryType.RowId},{Map.RowId},{unchecked((int)rawX)},{unchecked((int)rawY)}";

        private uint territoryTypeId;
        private uint mapId;
        private uint rawX;
        private uint rawY;
        // there is no Z; it's purely in the text payload where applicable

        public override string ToString()
        {
            return $"{Type} - TerritoryTypeId: {territoryTypeId}, MapId: {mapId}, RawX: {rawX}, RawY: {rawY}";
        }

        protected override byte[] EncodeImpl()
        {
            // TODO: for now we just encode the raw/internal values
            // eventually we should allow creation using 'nice' values that then encode properly

            var packedTerritoryAndMapBytes = MakePackedInteger(this.territoryTypeId, this.mapId);
            var xBytes = MakeInteger(this.rawX);
            var yBytes = MakeInteger(this.rawY);

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
                this.rawX = (uint)GetInteger(reader);
                this.rawY = (uint)GetInteger(reader);
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
        private float ConvertRawPositionToMapCoordinate(uint pos, float scale)
        {
            var c = scale / 100.0f;
            var scaledPos = (int)pos * c / 1000.0f;

            return ((41.0f / c) * ((scaledPos + 1024.0f) / 2048.0f)) + 1.0f;
        }

        // Created as the inverse of ConvertRawPositionToMapCoordinate(), since no one seemed to have a version of that
        private float ConvertMapCoordinateToRawPosition(float pos, float scale)
        {
            var c = scale / 100.0f;

            var scaledPos = ((((pos - 1.0f) * c / 41.0f) * 2048.0f) - 1024.0f) / c;
            scaledPos *= 1000.0f;

            return (int)Math.Round(scaledPos);
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

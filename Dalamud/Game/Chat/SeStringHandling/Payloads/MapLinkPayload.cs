using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class MapLinkPayload : Payload
    {
        public override PayloadType Type => PayloadType.MapLink;

        // pre-Resolve() values
        public int TerritoryTypeId { get; set; }
        public int MapId { get; set;  }
        public uint RawX { get; set; }
        public uint RawY { get; set; }

        // Resolved values
        // It might make sense to have Territory be an external type, that has assorted relevant info
        public string Territory { get; private set; }
        public string Zone { get; private set; }
        public float XCoord { get; private set; }
        public float YCoord { get; private set; }
        // Z?

        public override byte[] Encode()
        {
            // TODO: for now we just encode the raw/internal values
            // eventually we should allow creation using 'nice' values that then encode properly

            var packedTerritoryAndMapBytes = WrapPackedIntegerAndMarker(TerritoryTypeId, MapId);
            var xBytes = WrapIntegerAndMarker((int)RawX);
            var yBytes = WrapIntegerAndMarker((int)RawY);

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

        public override void Resolve()
        {
            if (string.IsNullOrEmpty(Territory))
            {
                var terrRow = dataResolver.GetExcelSheet<TerritoryType>().GetRow(TerritoryTypeId);
                Territory = dataResolver.GetExcelSheet<PlaceName>().GetRow(terrRow.PlaceName).Name;
                Zone = dataResolver.GetExcelSheet<PlaceName>().GetRow(terrRow.PlaceNameZone).Name;

                var mapSizeFactor = dataResolver.GetExcelSheet<Map>().GetRow(MapId).SizeFactor;
                XCoord = ConvertRawPositionToMapCoordinate(RawX, mapSizeFactor);
                YCoord = ConvertRawPositionToMapCoordinate(RawY, mapSizeFactor);
            }
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            (TerritoryTypeId, MapId) = GetPackedIntegers(reader);
            RawX = (uint)GetInteger(reader);
            RawY = (uint)GetInteger(reader);
            // the Z coordinate is never in this chunk, just the text (if applicable)

            // seems to always be FF 01
            reader.ReadBytes(2);
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

        private byte[] WrapIntegerAndMarker(int value)
        {
            var encodedValue = MakeInteger(value);
            var marker = GetTypeForIntegerBytes_Custom(encodedValue, false);

            var bytes = new List<byte>();
            if ((IntegerType)marker != IntegerType.None)
            {
                bytes.Add(marker);
            }
            bytes.AddRange(encodedValue);

            return bytes.ToArray();
        }

        private byte[] WrapPackedIntegerAndMarker(int value1, int value2)
        {
            var encodedValue = MakePackedInteger(TerritoryTypeId, MapId);
            var marker = GetTypeForIntegerBytes_Custom(encodedValue, true);

            var bytes = new List<byte>() { marker };
            bytes.AddRange(encodedValue);

            return bytes.ToArray();
        }

        // This seems entirely custom in various payload types
        // I'm not really sure if it matters if we use one version of Int16 over another, for example
        // but for now I try to match what incoming messages seem to use
        private byte GetTypeForIntegerBytes_Custom(byte[] bytes, bool packed = false)
        {
            var type = bytes.Length switch
            {
                3 => IntegerType.Int24Special,                              // used because seen in incoming data
                2 => packed ? IntegerType.Int16Packed : IntegerType.Int16,  // packed bytes seem to get a different marker.. but not packed shorts
                1 => IntegerType.None,                                      // single bytes seem to have no prefix at all here
                _ => GetTypeForIntegerBytes(bytes)
            };

            return (byte)type;
        }
    }
}

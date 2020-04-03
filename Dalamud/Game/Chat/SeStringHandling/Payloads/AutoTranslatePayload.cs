using System;
using System.Collections.Generic;
using System.IO;


namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class AutoTranslatePayload : Payload
    {
        public override PayloadType Type => PayloadType.AutoTranslateText;

        public uint Group { get; set; }

        public uint Key { get; set; }

        public string Text { get; set; }

        public override void Resolve()
        {
            // TODO: fixup once lumina DI is in

            //if (string.IsNullOrEmpty(Text))
            //{
            //    var sheet = dalamud.Data.GetExcelSheet<Completion>();

            //    Completion row = null;
            //    try
            //    {
            //        // try to get the row in the Completion table itself, because this is 'easiest'
            //        // The row may not exist at all (if the Key is for another table), or it could be the wrong row
            //        // (again, if it's meant for another table)
            //        row = sheet.GetRow(Key);
            //    } 
            //    catch {}    // don't care, row will be null

            //    if (row?.Group == Group)
            //    {
            //        // if the row exists in this table and the group matches, this is actually the correct data
            //        Text = $"{{ {row.Text} }} ";
            //    }
            //    else
            //    {
            //        Log.Verbose("row mismatch");
            //        try
            //        {
            //            // we need to get the linked table and do the lookup there instead
            //            // in this case, there will only be one entry for this group id
            //            row = sheet.GetRows().First(r => r.Group == Group);
            //            // many of the names contain valid id ranges after the table name, but we don't need those
            //            var actualTableName = row.LookupTable.Split('[')[0];

            //            var name = actualTableName switch
            //            {
            //                // TODO: rest of xref'd tables
            //                "Action" => dalamud.Data.GetExcelSheet<Data.TransientSheet.Action>().GetRow(Key).Name,
            //                "ClassJob" => dalamud.Data.GetExcelSheet<ClassJob>().GetRow(Key).Name,
            //                "CraftAction" => dalamud.Data.GetExcelSheet<CraftAction>().GetRow(Key).Name,
            //                "Mount" => dalamud.Data.GetExcelSheet<Mount>().GetRow(Key).Singular,
            //                "PlaceName" => dalamud.Data.GetExcelSheet<PlaceName>().GetRow(Key).Name,
            //                "Race" => dalamud.Data.GetExcelSheet<Race>().GetRow(Key).Masculine,
            //                _ => throw new Exception(actualTableName)
            //            };

            //            Text = $"{{ {name} }} ";
            //        }
            //        catch (Exception e)
            //        {
            //            Log.Error(e, $"AutoTranslatePayload - failed to resolve: {this}");
            //        }
            //    }
            //}
        }

        public override byte[] Encode()
        {
            var keyBytes = MakeInteger(Key);

            var chunkLen = keyBytes.Length + 2;
            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.AutoTranslateKey, (byte)chunkLen,
                (byte)Group
            };
            bytes.AddRange(keyBytes);
            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        public override string ToString()
        {
            return $"{Type} - Group: {Group}, Key: {Key}, Text: {Text}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            // this seems to always be a bare byte, and not following normal integer encoding
            // the values in the table are all <70 so this is presumably ok
            Group = reader.ReadByte();

            Key = GetInteger(reader);
        }
    }
}

using Dalamud.Data.TransientSheet;
using Lumina.Excel.GeneratedSheets;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class AutoTranslatePayload : Payload, ITextProvider
    {
        public override PayloadType Type => PayloadType.AutoTranslateText;

        private string text;
        public string Text
        {
            get
            {
                // wrap the text in the colored brackets that is uses in-game, since those
                // are not actually part of any of the payloads
                this.text ??= $"{(char)SeIconChar.AutoTranslateOpen} {Resolve()} {(char)SeIconChar.AutoTranslateClose}";
                return this.text;
            }
        }

        private uint group;
        private uint key;

        internal AutoTranslatePayload() { }

        public AutoTranslatePayload(uint group, uint key)
        {
            this.group = group;
            this.key = key;
        }

        // TODO: friendlier ctor? not sure how to handle that given how weird the tables are

        public override string ToString()
        {
            return $"{Type} - Group: {group}, Key: {key}";
        }

        protected override byte[] EncodeImpl()
        {
            var keyBytes = MakeInteger(this.key);

            var chunkLen = keyBytes.Length + 2;
            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.AutoTranslateKey, (byte)chunkLen,
                (byte)this.group
            };
            bytes.AddRange(keyBytes);
            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            // this seems to always be a bare byte, and not following normal integer encoding
            // the values in the table are all <70 so this is presumably ok
            this.group = reader.ReadByte();

            this.key = GetInteger(reader);
        }

        private string Resolve()
        {
            string value = null;

            var sheet = this.dataResolver.GetExcelSheet<Completion>();

            Completion row = null;
            try
            {
                // try to get the row in the Completion table itself, because this is 'easiest'
                // The row may not exist at all (if the Key is for another table), or it could be the wrong row
                // (again, if it's meant for another table)
                row = sheet.GetRow((int)this.key);
            }
            catch { }    // don't care, row will be null

            if (row?.Group == this.group)
            {
                // if the row exists in this table and the group matches, this is actually the correct data
                value = row.Text;
            }
            else
            {
                try
                {
                    // we need to get the linked table and do the lookup there instead
                    // in this case, there will only be one entry for this group id
                    row = sheet.GetRows().First(r => r.Group == this.group);
                    // many of the names contain valid id ranges after the table name, but we don't need those
                    var actualTableName = row.LookupTable.Split('[')[0];

                    var ikey = (int)this.key;

                    var name = actualTableName switch
                    {
                        "Action" => this.dataResolver.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>().GetRow(ikey).Name,
                        "ActionComboRoute" => this.dataResolver.GetExcelSheet<ActionComboRoute>().GetRow(ikey).Name,
                        "BuddyAction" => this.dataResolver.GetExcelSheet<BuddyAction>().GetRow(ikey).Name,
                        "ClassJob" => this.dataResolver.GetExcelSheet<ClassJob>().GetRow(ikey).Name,
                        "Companion" => this.dataResolver.GetExcelSheet<Companion>().GetRow(ikey).Singular,
                        "CraftAction" => this.dataResolver.GetExcelSheet<CraftAction>().GetRow(ikey).Name,
                        "GeneralAction" => this.dataResolver.GetExcelSheet<GeneralAction>().GetRow(ikey).Name,
                        "GuardianDeity" => this.dataResolver.GetExcelSheet<GuardianDeity>().GetRow(ikey).Name,
                        "MainCommand" => this.dataResolver.GetExcelSheet<MainCommand>().GetRow(ikey).Name,
                        "Mount" => this.dataResolver.GetExcelSheet<Mount>().GetRow(ikey).Singular,
                        "Pet" => this.dataResolver.GetExcelSheet<Pet>().GetRow(ikey).Name,
                        "PetAction" => this.dataResolver.GetExcelSheet<PetAction>().GetRow(ikey).Name,
                        // TODO: lumina doesn't have PetMirage
                        "PlaceName" => this.dataResolver.GetExcelSheet<PlaceName>().GetRow(ikey).Name,
                        "Race" => this.dataResolver.GetExcelSheet<Race>().GetRow(ikey).Masculine,
                        "TextCommand" => this.dataResolver.GetExcelSheet<TextCommand>().GetRow(ikey).Command,
                        "Tribe" => this.dataResolver.GetExcelSheet<Tribe>().GetRow(ikey).Masculine,
                        "Weather" => this.dataResolver.GetExcelSheet<Weather>().GetRow(ikey).Name,
                        _ => throw new Exception(actualTableName)
                    };

                    value = name;
                }
                catch (Exception e)
                {
                    Log.Error(e, $"AutoTranslatePayload - failed to resolve: {this}");
                }
            }

            return value;
        }
    }
}

using Lumina.Excel.GeneratedSheets;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Data;
using Dalamud.Data.TransientSheet;
using Newtonsoft.Json;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload containing an auto-translation/completion chat message.
    /// </summary>
    public class AutoTranslatePayload : Payload, ITextProvider
    {
        public override PayloadType Type => PayloadType.AutoTranslateText;

        private string text;
        /// <summary>
        /// The actual text displayed in-game for this payload.
        /// </summary>
        /// <remarks>
        /// Value is evaluated lazily and cached.
        /// </remarks>
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

        [JsonProperty]
        private uint group;

        [JsonProperty]
        private uint key;

        internal AutoTranslatePayload() { }

        /// <summary>
        /// Creates a new auto-translate payload.
        /// </summary>
        /// <param name="data">DataManager instance needed to resolve game data.</param>
        /// <param name="group">The group id for this message.</param>
        /// <param name="key">The key/row id for this message.  Which table this is in depends on the group id and details the Completion table.</param>
        /// <remarks>
        /// This table is somewhat complicated in structure, and so using this constructor may not be very nice.
        /// There is probably little use to create one of these, however.
        /// </remarks>
        public AutoTranslatePayload(DataManager data, uint group, uint key) {
            this.DataResolver = data;
            this.group = group;
            this.key = key;
        }

        // TODO: friendlier ctor? not sure how to handle that given how weird the tables are

        public override string ToString()
        {
            return $"{Type} - Group: {group}, Key: {key}, Text: {Text}";
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

            var sheet = this.DataResolver.GetExcelSheet<Completion>();

            Completion row = null;
            try
            {
                // try to get the row in the Completion table itself, because this is 'easiest'
                // The row may not exist at all (if the Key is for another table), or it could be the wrong row
                // (again, if it's meant for another table)
                row = sheet.GetRow(this.key);
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

                    var name = actualTableName switch
                    {
                        "Action" => this.DataResolver.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>().GetRow(this.key).Name,
                        "ActionComboRoute" => this.DataResolver.GetExcelSheet<ActionComboRoute>().GetRow(this.key).Name,
                        "BuddyAction" => this.DataResolver.GetExcelSheet<BuddyAction>().GetRow(this.key).Name,
                        "ClassJob" => this.DataResolver.GetExcelSheet<ClassJob>().GetRow(this.key).Name,
                        "Companion" => this.DataResolver.GetExcelSheet<Companion>().GetRow(this.key).Singular,
                        "CraftAction" => this.DataResolver.GetExcelSheet<CraftAction>().GetRow(this.key).Name,
                        "GeneralAction" => this.DataResolver.GetExcelSheet<GeneralAction>().GetRow(this.key).Name,
                        "GuardianDeity" => this.DataResolver.GetExcelSheet<GuardianDeity>().GetRow(this.key).Name,
                        "MainCommand" => this.DataResolver.GetExcelSheet<MainCommand>().GetRow(this.key).Name,
                        "Mount" => this.DataResolver.GetExcelSheet<Mount>().GetRow(this.key).Singular,
                        "Pet" => this.DataResolver.GetExcelSheet<Pet>().GetRow(this.key).Name,
                        "PetAction" => this.DataResolver.GetExcelSheet<PetAction>().GetRow(this.key).Name,
                        "PetMirage" => this.DataResolver.GetExcelSheet<PetMirage>().GetRow(this.key).Name,
                        "PlaceName" => this.DataResolver.GetExcelSheet<PlaceName>().GetRow(this.key).Name,
                        "Race" => this.DataResolver.GetExcelSheet<Race>().GetRow(this.key).Masculine,
                        "TextCommand" => this.DataResolver.GetExcelSheet<TextCommand>().GetRow(this.key).Command,
                        "Tribe" => this.DataResolver.GetExcelSheet<Tribe>().GetRow(this.key).Masculine,
                        "Weather" => this.DataResolver.GetExcelSheet<Weather>().GetRow(this.key).Name,
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

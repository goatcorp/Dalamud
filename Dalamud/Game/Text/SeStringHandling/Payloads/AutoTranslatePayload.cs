using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Data;

using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload containing an auto-translation/completion chat message.
/// </summary>
public class AutoTranslatePayload : Payload, ITextProvider
{
    private string? text;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoTranslatePayload"/> class.
    /// Creates a new auto-translate payload.
    /// </summary>
    /// <param name="group">The group id for this message.</param>
    /// <param name="key">The key/row id for this message.  Which table this is in depends on the group id and details the Completion table.</param>
    /// <remarks>
    /// This table is somewhat complicated in structure, and so using this constructor may not be very nice.
    /// There is probably little use to create one of these, however.
    /// </remarks>
    public AutoTranslatePayload(uint group, uint key)
    {
        // TODO: friendlier ctor? not sure how to handle that given how weird the tables are
        this.Group = group;
        this.Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoTranslatePayload"/> class.
    /// </summary>
    internal AutoTranslatePayload()
    {
    }
    
    /// <summary>
    /// Gets the autotranslate group.
    /// </summary>
    [JsonProperty("group")]
    public uint Group { get; private set; }

    /// <summary>
    /// Gets the autotranslate key.
    /// </summary>
    [JsonProperty("key")]
    public uint Key { get; private set; }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.AutoTranslateText;

    /// <summary>
    /// Gets the actual text displayed in-game for this payload.
    /// </summary>
    /// <remarks>
    /// Value is evaluated lazily and cached.
    /// </remarks>
    public string Text
    {
        get
        {
            // wrap the text in the colored brackets that is uses in-game, since those are not actually part of any of the payloads
            return this.text ??= $"{(char)SeIconChar.AutoTranslateOpen} {this.Resolve()} {(char)SeIconChar.AutoTranslateClose}";
        }
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"{this.Type} - Group: {this.Group}, Key: {this.Key}, Text: {this.Text}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var keyBytes = MakeInteger(this.Key);

        var chunkLen = keyBytes.Length + 2;
        var bytes = new List<byte>()
        {
            START_BYTE,
            (byte)SeStringChunkType.AutoTranslateKey, (byte)chunkLen,
            (byte)this.Group,
        };
        bytes.AddRange(keyBytes);
        bytes.Add(END_BYTE);

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        // this seems to always be a bare byte, and not following normal integer encoding
        // the values in the table are all <70 so this is presumably ok
        this.Group = reader.ReadByte();

        this.Key = GetInteger(reader);
    }

    private static ReadOnlySeString ResolveTextCommand(TextCommand command)
    {
        // TextCommands prioritize the `Alias` field, if it not empty
        // Example for this is /rangerpose2l which becomes /blackrangerposeb in chat
        return !command.Alias.IsEmpty ? command.Alias : command.Command;
    }

    private string Resolve()
    {
        string value = null;

        var excelModule = Service<DataManager>.Get().Excel;
        var completionSheet = excelModule.GetSheet<Completion>();

        // try to get the row in the Completion table itself, because this is 'easiest'
        // The row may not exist at all (if the Key is for another table), or it could be the wrong row
        // (again, if it's meant for another table)

        if (completionSheet.GetRowOrDefault(this.Key) is { } completion && completion.Group == this.Group)
        {
            // if the row exists in this table and the group matches, this is actually the correct data
            value = completion.Text.ExtractText();
        }
        else
        {
            try
            {
                // we need to get the linked table and do the lookup there instead
                // in this case, there will only be one entry for this group id
                var row = completionSheet.First(r => r.Group == this.Group);
                // many of the names contain valid id ranges after the table name, but we don't need those
                var actualTableName = row.LookupTable.ExtractText().Split('[')[0];

                var name = actualTableName switch
                {
                    "Action" => excelModule.GetSheet<Lumina.Excel.Sheets.Action>().GetRow(this.Key).Name,
                    "ActionComboRoute" => excelModule.GetSheet<ActionComboRoute>().GetRow(this.Key).Name,
                    "BuddyAction" => excelModule.GetSheet<BuddyAction>().GetRow(this.Key).Name,
                    "ClassJob" => excelModule.GetSheet<ClassJob>().GetRow(this.Key).Name,
                    "Companion" => excelModule.GetSheet<Companion>().GetRow(this.Key).Singular,
                    "CraftAction" => excelModule.GetSheet<CraftAction>().GetRow(this.Key).Name,
                    "GeneralAction" => excelModule.GetSheet<GeneralAction>().GetRow(this.Key).Name,
                    "GuardianDeity" => excelModule.GetSheet<GuardianDeity>().GetRow(this.Key).Name,
                    "MainCommand" => excelModule.GetSheet<MainCommand>().GetRow(this.Key).Name,
                    "Mount" => excelModule.GetSheet<Mount>().GetRow(this.Key).Singular,
                    "Pet" => excelModule.GetSheet<Pet>().GetRow(this.Key).Name,
                    "PetAction" => excelModule.GetSheet<PetAction>().GetRow(this.Key).Name,
                    "PetMirage" => excelModule.GetSheet<PetMirage>().GetRow(this.Key).Name,
                    "PlaceName" => excelModule.GetSheet<PlaceName>().GetRow(this.Key).Name,
                    "Race" => excelModule.GetSheet<Race>().GetRow(this.Key).Masculine,
                    "TextCommand" => AutoTranslatePayload.ResolveTextCommand(excelModule.GetSheet<TextCommand>().GetRow(this.Key)),
                    "Tribe" => excelModule.GetSheet<Tribe>().GetRow(this.Key).Masculine,
                    "Weather" => excelModule.GetSheet<Weather>().GetRow(this.Key).Name,
                    _ => throw new Exception(actualTableName),
                };

                value = name.ExtractText();
            }
            catch (Exception e)
            {
                Log.Error(e, $"AutoTranslatePayload - failed to resolve: {this.Type} - Group: {this.Group}, Key: {this.Key}");
            }
        }

        return value;
    }
}

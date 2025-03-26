using System.Collections.Generic;

using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Dalamud.Game.Text.SeStringHandling;

/// <summary>
/// Helper class to build SeStrings using a builder pattern.
/// </summary>
public class SeStringBuilder
{
    /// <summary>
    /// Gets the built SeString.
    /// </summary>
    public SeString BuiltString { get; init; } = new SeString();

    /// <summary>
    /// Append another SeString to the builder.
    /// </summary>
    /// <param name="toAppend">The SeString to append.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder Append(SeString toAppend)
    {
        this.BuiltString.Append(toAppend);
        return this;
    }

    /// <summary>
    /// Append raw text to the builder.
    /// </summary>
    /// <param name="text">The raw text.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder Append(string text) => this.AddText(text);

    /// <summary>
    /// Append payloads to the builder.
    /// </summary>
    /// <param name="payloads">A list of payloads.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder Append(IEnumerable<Payload> payloads)
    {
        this.BuiltString.Payloads.AddRange(payloads);
        return this;
    }

    /// <summary>
    /// Append raw text to the builder.
    /// </summary>
    /// <param name="text">The raw text.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddText(string text) => this.Add(new TextPayload(text));

    /// <summary>
    /// Start colored text in the current builder.
    /// </summary>
    /// <param name="colorKey">The text color.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddUiForeground(ushort colorKey) => this.Add(new UIForegroundPayload(colorKey));

    /// <summary>
    /// Turn off a previous colored text.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddUiForegroundOff() => this.Add(UIForegroundPayload.UIForegroundOff);

    /// <summary>
    /// Add colored text to the current builder.
    /// </summary>
    /// <param name="text">The raw text.</param>
    /// <param name="colorKey">The text color.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddUiForeground(string text, ushort colorKey)
    {
        this.AddUiForeground(colorKey);
        this.AddText(text);
        return this.AddUiForegroundOff();
    }

    /// <summary>
    /// Start an UiGlow in the current builder.
    /// </summary>
    /// <param name="colorKey">The glow color.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddUiGlow(ushort colorKey) => this.Add(new UIGlowPayload(colorKey));

    /// <summary>
    /// Turn off a previous UiGlow.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddUiGlowOff() => this.Add(UIGlowPayload.UIGlowOff);

    /// <summary>
    /// Add glowing text to the current builder.
    /// </summary>
    /// <param name="text">The raw text.</param>
    /// <param name="colorKey">The glow color.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddUiGlow(string text, ushort colorKey)
    {
        this.AddUiGlow(colorKey);
        this.AddText(text);
        return this.AddUiGlowOff();
    }

    /// <summary>
    /// Add an icon to the builder.
    /// </summary>
    /// <param name="icon">The icon to add.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddIcon(BitmapFontIcon icon) => this.Add(new IconPayload(icon));

    /// <summary>
    /// Add an item link to the builder.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="isHq">Whether or not the item is high quality.</param>
    /// <param name="itemNameOverride">Override for the item's name.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddItemLink(uint itemId, bool isHq, string? itemNameOverride = null) =>
        this.Append(SeString.CreateItemLink(itemId, isHq, itemNameOverride));

    /// <summary>
    /// Add an item link to the builder.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="kind">Kind of item to encode.</param>
    /// <param name="itemNameOverride">Override for the item's name.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddItemLink(uint itemId, ItemPayload.ItemKind kind = ItemPayload.ItemKind.Normal, string? itemNameOverride = null) =>
        this.Append(SeString.CreateItemLink(itemId, kind, itemNameOverride));

    /// <summary>
    /// Add an item link to the builder.
    /// </summary>
    /// <param name="rawItemId">The raw item ID.</param>
    /// <returns>The current builder.</returns>
    /// <remarks>To terminate this item link, add a <see cref="RawPayload.LinkTerminator"/>.</remarks>
    public SeStringBuilder AddItemLinkRaw(uint rawItemId) =>
        this.Add(ItemPayload.FromRaw(rawItemId));

    /// <summary>
    /// Add italicized raw text to the builder.
    /// </summary>
    /// <param name="text">The raw text.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddItalics(string text)
    {
        this.Add(EmphasisItalicPayload.ItalicsOn);
        this.AddText(text);
        return this.Add(EmphasisItalicPayload.ItalicsOff);
    }

    /// <summary>
    /// Turn italics on.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddItalicsOn() => this.Add(EmphasisItalicPayload.ItalicsOn);

    /// <summary>
    /// Turn italics off.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddItalicsOff() => this.Add(EmphasisItalicPayload.ItalicsOff);

    /// <summary>
    /// Add a map link payload to the builder.
    /// </summary>
    /// <param name="territoryTypeId">The id of the TerritoryType entry for this link.</param>
    /// <param name="mapId">The id of the Map entry for this link.</param>
    /// <param name="rawX">The internal raw x-coordinate for this link.</param>
    /// <param name="rawY">The internal raw y-coordinate for this link.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddMapLink(uint territoryTypeId, uint mapId, int rawX, int rawY) =>
        this.Add(new MapLinkPayload(territoryTypeId, mapId, rawX, rawY));

    /// <summary>
    /// Add a map link payload to the builder.
    /// </summary>
    /// <param name="territoryTypeId">The id of the TerritoryType entry for this link.</param>
    /// <param name="mapId">The id of the Map entry for this link.</param>
    /// <param name="niceXCoord">The human-readable x-coordinate for this link.</param>
    /// <param name="niceYCoord">The human-readable y-coordinate for this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddMapLink(
        uint territoryTypeId, uint mapId, float niceXCoord, float niceYCoord, float fudgeFactor = 0.05f) =>
        this.Add(new MapLinkPayload(territoryTypeId, mapId, niceXCoord, niceYCoord, fudgeFactor));

    /// <summary>
    /// Add a quest link to the builder.
    /// </summary>
    /// <param name="questId">The quest ID.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddQuestLink(uint questId) => this.Add(new QuestPayload(questId));

    /// <summary>
    /// Add a status effect link to the builder.
    /// </summary>
    /// <param name="statusId">The status effect ID.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddStatusLink(uint statusId) => this.Add(new StatusPayload(statusId));

    /// <summary>
    /// Add a link to the party finder search conditions to the builder.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddPartyFinderSearchConditionsLink() => this.Add(new PartyFinderPayload());

    /// <summary>
    /// Add a party finder listing link to the builder.
    /// </summary>
    /// <param name="id">The listing ID of the party finder listing.</param>
    /// <param name="isCrossWorld">Whether the listing is limited to the recruiting world.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder AddPartyFinderLink(uint id, bool isCrossWorld = false) => this.Add(new PartyFinderPayload(id, isCrossWorld ? PartyFinderPayload.PartyFinderLinkType.NotSpecified : PartyFinderPayload.PartyFinderLinkType.LimitedToHomeWorld));

    /// <summary>
    /// Add a payload to the builder.
    /// </summary>
    /// <param name="payload">The payload to add.</param>
    /// <returns>The current builder.</returns>
    public SeStringBuilder Add(Payload payload)
    {
        this.BuiltString.Payloads.Add(payload);
        return this;
    }

    /// <summary>
    /// Return the built string.
    /// </summary>
    /// <returns>The built string.</returns>
    public SeString Build() => this.BuiltString;

    /// <summary>
    /// Encode the built string to bytes.
    /// </summary>
    /// <returns>The built string, encoded to UTF-8 bytes.</returns>
    public byte[] Encode() => this.BuiltString.Encode();

    /// <summary>
    /// Return the text representation of this string.
    /// </summary>
    /// <returns>The text representation of this string.</returns>
    public override string ToString() => this.BuiltString.ToString();
}

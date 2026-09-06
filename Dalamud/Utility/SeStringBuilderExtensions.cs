using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;

using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

using SeString = Dalamud.Game.Text.SeString;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for SeStringBuilder.
/// </summary>
public static class SeStringBuilderExtensions
{
    /// <summary>
    /// Determines whether the <see cref="SeStringBuilder"/> contains the specified text.
    /// </summary>
    /// <param name="builder">The builder to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><c>true</c> if the text is found; otherwise, <c>false</c>.</returns>
    public static bool ContainsText(this SeStringBuilder builder, ReadOnlySpan<byte> needle)
    {
        return builder.ToReadOnlySeString().ContainsText(needle);
    }

    /// <summary>
    /// Replaces occurrences of a specified text in a <see cref="ReadOnlySeString"/> with another text.
    /// </summary>
    /// <param name="ross">The original string.</param>
    /// <param name="toFind">The text to find.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>A new <see cref="ReadOnlySeString"/> with the replacements made.</returns>
    public static ReadOnlySeString ReplaceText(
        this ReadOnlySeString ross,
        ReadOnlySpan<byte> toFind,
        ReadOnlySpan<byte> replacement)
    {
        if (ross.IsEmpty)
            return ross;

        using var rssb = new RentedSeStringBuilder();

        foreach (var payload in ross)
        {
            if (payload.Type == ReadOnlySePayloadType.Invalid)
                continue;

            if (payload.Type != ReadOnlySePayloadType.Text)
            {
                rssb.Builder.Append(payload);
                continue;
            }

            var index = payload.Body.Span.IndexOf(toFind);
            if (index == -1)
            {
                rssb.Builder.Append(payload);
                continue;
            }

            var lastIndex = 0;
            while (index != -1)
            {
                rssb.Builder.Append(payload.Body.Span[lastIndex..index]);

                if (!replacement.IsEmpty)
                {
                    rssb.Builder.Append(replacement);
                }

                lastIndex = index + toFind.Length;
                index = payload.Body.Span[lastIndex..].IndexOf(toFind);

                if (index != -1)
                    index += lastIndex;
            }

            rssb.Builder.Append(payload.Body.Span[lastIndex..]);
        }

        return rssb.Builder.ToReadOnlySeString();
    }

    /// <summary>
    /// Replaces occurrences of a specified text in an <see cref="SeStringBuilder"/> with another text.
    /// </summary>
    /// <param name="builder">The builder to modify.</param>
    /// <param name="toFind">The text to find.</param>
    /// <param name="replacement">The replacement text.</param>
    public static void ReplaceText(
        this SeStringBuilder builder,
        ReadOnlySpan<byte> toFind,
        ReadOnlySpan<byte> replacement)
    {
        if (toFind.IsEmpty)
            return;

        var str = builder.ToReadOnlySeString();
        if (str.IsEmpty)
            return;

        var replaced = ReplaceText(new ReadOnlySeString(builder.GetViewAsMemory()), toFind, replacement);
        builder.Clear().Append(replaced);
    }

    /// <summary>
    /// Appends a custom Dalamud link macro to the string builder.
    /// </summary>
    /// <param name="builder">The builder instance to append to.</param>
    /// <param name="commandId">The custom command id to trigger upon interaction.</param>
    /// <param name="pluginName">The internal name of the plugin handling the link.</param>
    /// <param name="extra1">An optional user-defined integer parameter passed to the click handler.</param>
    /// <param name="extra2">An optional second user-defined integer parameter passed to the click handler.</param>
    /// <param name="extraString">An optional user-defined string parameter passed to the click handler.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance for fluent chaining.</returns>
    public static SeStringBuilder PushDalamudLink(this SeStringBuilder builder, uint commandId, string pluginName, int extra1, int extra2, string extraString)
    {
        return builder
            .BeginMacro(MacroCode.Link)
            .AppendIntExpression((int)DalamudLinkPayload.LinkType)
            .AppendUIntExpression(commandId)
            .AppendIntExpression(extra1)
            .AppendIntExpression(extra2)
            .BeginStringExpression()
            .Append(JsonConvert.SerializeObject(new[] { pluginName, extraString }))
            .EndExpression()
            .EndMacro();
    }

    /// <summary>
    /// Appends a custom Dalamud link macro to the string builder.
    /// </summary>
    /// <param name="builder">The builder instance to append to.</param>
    /// <param name="payload">The link payload containing handler and custom parameter data.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance for fluent chaining.</returns>
    public static SeStringBuilder PushDalamudLink(this SeStringBuilder builder, DalamudLinkPayload payload)
    {
        return PushDalamudLink(builder, payload.CommandId, payload.PluginName, payload.Extra1, payload.Extra2, payload.ExtraString);
    }

    /// <summary>
    /// Appends a formatted item link payload using a combined raw item Id.
    /// </summary>
    /// <param name="builder">The builder to append the item link to.</param>
    /// <param name="itemId">The Id of the Item or EventItem to link. Automatically extracted into its base Id and <see cref="ItemKind"/>.</param>
    /// <param name="displayNameOverride">An optional name override to display instead of the actual item name.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance with the item link appended for method chaining.</returns>
    public static SeStringBuilder PushItemLink(this SeStringBuilder builder, uint itemId, string? displayNameOverride = null)
    {
        var (baseItemId, itemKind) = ItemUtil.GetBaseId(itemId);
        return builder.Append(SeString.CreateItemLink(baseItemId, itemKind, displayNameOverride));
    }

    /// <summary>
    /// Appends a formatted item link payload.
    /// </summary>
    /// <param name="builder">The builder to append the item link to.</param>
    /// <param name="baseItemId">The base Id of the item to link.</param>
    /// <param name="itemKind">The <see cref="ItemKind"/> variant of the item (e.g., normal, high-quality, collectable).</param>
    /// <param name="displayNameOverride">An optional name override to display instead of the actual item name.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance with the item link appended for method chaining.</returns>
    public static SeStringBuilder PushItemLink(this SeStringBuilder builder, uint baseItemId, ItemKind itemKind, string? displayNameOverride = null)
    {
        return builder.Append(SeString.CreateItemLink(baseItemId, itemKind, displayNameOverride));
    }

    /// <summary>
    /// Appends a formatted item link payload.
    /// </summary>
    /// <param name="builder">The builder to append the item link to.</param>
    /// <param name="item">The Lumina <see cref="Item"/> data object to link.</param>
    /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display instead of the actual item name.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance with the item link appended for method chaining.</returns>
    public static SeStringBuilder PushItemLink(this SeStringBuilder builder, Item item, bool isHq, string? displayNameOverride = null)
    {
        return builder.Append(SeString.CreateItemLink(item, isHq, displayNameOverride));
    }

    /// <summary>
    /// Appends a formatted map link payload for the position of a <see cref="IGameObject"/>.
    /// </summary>
    /// <param name="builder">The builder to append the map link to.</param>
    /// <param name="obj">The game object whose current position should be linked.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance with the map link appended for method chaining.</returns>
    public static SeStringBuilder PushMapLink(this SeStringBuilder builder, IGameObject obj)
    {
        return builder.Append(SeString.CreateMapLink(obj));
    }

    /// <summary>
    /// Appends a formatted map link payload.
    /// </summary>
    /// <param name="builder">The builder to append the map link to.</param>
    /// <param name="territoryId">The Id of the <c>TerritoryType</c> for this map link.</param>
    /// <param name="mapId">The Id of the <c>Map</c> for this map link.</param>
    /// <param name="xCoord">The human-readable X-coordinate for this link.</param>
    /// <param name="yCoord">The human-readable Y-coordinate for this link.</param>
    /// <param name="zCoord">An optional human-readable Z-coordinate for this link.</param>
    /// <param name="instanceId">An optional area instance number to be included in this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance with the map link appended for method chaining.</returns>
    public static SeStringBuilder PushMapLink(
        this SeStringBuilder builder,
        uint territoryId,
        uint mapId,
        float xCoord,
        float yCoord,
        float zCoord = 0,
        int instanceId = 0,
        float fudgeFactor = 0.05f)
    {
        return builder.Append(SeString.CreateMapLink(territoryId, mapId, xCoord, yCoord, zCoord, instanceId, fudgeFactor));
    }

    /// <summary>
    /// Appends a formatted party finder listing link payload.
    /// </summary>
    /// <param name="builder">The builder to append the party finder link to.</param>
    /// <param name="listingId">The listing Id of the party finder entry.</param>
    /// <param name="recruiterName">The name of the recruiter.</param>
    /// <param name="isCrossWorld">Whether the listing is limited to the current world or not.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance with the party finder link appended for method chaining.</returns>
    public static SeStringBuilder PushPartyFinderLink(this SeStringBuilder builder, uint listingId, string recruiterName, bool isCrossWorld = false)
    {
        return builder.Append(SeString.CreatePartyFinderLink(listingId, recruiterName, isCrossWorld));
    }

    /// <summary>
    /// Appends a formatted party finder search conditions link payload.
    /// </summary>
    /// <param name="builder">The builder to append the search conditions link to.</param>
    /// <param name="message">The text that should be displayed for the link.</param>
    /// <returns>The <see cref="SeStringBuilder"/> instance with the search conditions link appended for method chaining.</returns>
    public static SeStringBuilder PushPartyFinderSearchConditionsLink(this SeStringBuilder builder, string message)
    {
        return builder.Append(SeString.CreatePartyFinderSearchConditionsLink(message));
    }
}

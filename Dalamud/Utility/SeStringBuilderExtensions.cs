using Dalamud.Game.Text.SeStringHandling.Payloads;

using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.Payloads;

using Newtonsoft.Json;

using EmbeddedInfoType = Dalamud.Game.Text.SeStringHandling.Payload.EmbeddedInfoType;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;

namespace Dalamud.Utility;

// Note: Do not forget to add forwards to RentedSeStringBuilder.

/// <summary>
/// Extension methods for SeStringBuilder.
/// </summary>
public static class SeStringBuilderExtensions
{
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
            .AppendIntExpression((int)EmbeddedInfoType.DalamudLink)
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
        return PushDalamudLink(builder, payload.CommandId, payload.Plugin, payload.Extra1, payload.Extra2, payload.ExtraString);
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

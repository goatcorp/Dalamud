using Dalamud.Excel.Sheets;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;

using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

using SeString = Dalamud.Game.Text.SeString;

namespace Dalamud.Utility;

#pragma warning disable SA1101

// Note: Do not forget to add forwards to RentedSeStringBuilder.

/// <summary>
/// Extension methods for SeStringBuilder.
/// </summary>
public static class SeStringBuilderExtensions
{
    extension(SeStringBuilder builder)
    {
        /// <summary>
        /// Determines whether the <see cref="SeStringBuilder"/> contains the specified text.
        /// </summary>
        /// <param name="needle">The text to find.</param>
        /// <returns><c>true</c> if the text is found; otherwise, <c>false</c>.</returns>
        public bool ContainsText(ReadOnlySpan<byte> needle)
        {
            return builder.ToReadOnlySeString().ContainsText(needle);
        }

        /// <summary>
        /// Replaces occurrences of a specified text in an <see cref="SeStringBuilder"/> with another text.
        /// </summary>
        /// <param name="toFind">The text to find.</param>
        /// <param name="replacement">The replacement text.</param>
        public void ReplaceText(ReadOnlySpan<byte> toFind, ReadOnlySpan<byte> replacement)
        {
            if (toFind.IsEmpty)
                return;

            var str = builder.ToReadOnlySeString();
            if (str.IsEmpty)
                return;

            var replaced = new ReadOnlySeString(builder.GetViewAsMemory()).ReplaceText(toFind, replacement);
            builder.Clear().Append(replaced);
        }

        /// <summary>
        /// Appends a custom Dalamud link macro to the string builder.
        /// </summary>
        /// <param name="commandId">The custom command id to trigger upon interaction.</param>
        /// <param name="pluginName">The internal name of the plugin handling the link.</param>
        /// <param name="extra1">An optional user-defined integer parameter passed to the click handler.</param>
        /// <param name="extra2">An optional second user-defined integer parameter passed to the click handler.</param>
        /// <param name="extraString">An optional user-defined string parameter passed to the click handler.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance for fluent chaining.</returns>
        public SeStringBuilder PushDalamudLink(uint commandId, string pluginName, int extra1, int extra2, string extraString)
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
        /// <param name="payload">The link payload containing handler and custom parameter data.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance for fluent chaining.</returns>
        public SeStringBuilder PushDalamudLink(DalamudLinkPayload payload)
        {
            return PushDalamudLink(builder, payload.CommandId, payload.PluginName, payload.Extra1, payload.Extra2, payload.ExtraString);
        }

        /// <summary>
        /// Appends a formatted item link payload using a combined raw item Id.
        /// </summary>
        /// <param name="itemId">The Id of the Item or EventItem to link. Automatically extracted into its base Id and <see cref="ItemKind"/>.</param>
        /// <param name="displayNameOverride">An optional name override to display instead of the actual item name.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the item link appended for method chaining.</returns>
        public SeStringBuilder PushItemLink(uint itemId, ReadOnlySeString displayNameOverride = default)
        {
            var (baseItemId, itemKind) = ItemUtil.GetBaseId(itemId);
            return builder.Append(SeString.CreateItemLink(baseItemId, itemKind, displayNameOverride));
        }

        /// <summary>
        /// Appends a formatted item link payload.
        /// </summary>
        /// <param name="baseItemId">The base Id of the item to link.</param>
        /// <param name="itemKind">The <see cref="ItemKind"/> variant of the item (e.g., normal, high-quality, collectable).</param>
        /// <param name="displayNameOverride">An optional name override to display instead of the actual item name.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the item link appended for method chaining.</returns>
        public SeStringBuilder PushItemLink(uint baseItemId, ItemKind itemKind, ReadOnlySeString displayNameOverride = default)
        {
            return builder.Append(SeString.CreateItemLink(baseItemId, itemKind, displayNameOverride));
        }

        /// <summary>
        /// Appends a formatted item link payload.
        /// </summary>
        /// <param name="item">The Lumina <see cref="Item"/> data object to link.</param>
        /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name override to display instead of the actual item name.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the item link appended for method chaining.</returns>
        public SeStringBuilder PushItemLink(Item item, bool isHq, ReadOnlySeString displayNameOverride = default)
        {
            return builder.Append(SeString.CreateItemLink(item, isHq, displayNameOverride));
        }

        /// <summary>
        /// Appends a formatted map link payload for the position of a <see cref="IGameObject"/>.
        /// </summary>
        /// <param name="obj">The game object whose current position should be linked.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the map link appended for method chaining.</returns>
        public SeStringBuilder PushMapLink(IGameObject obj)
        {
            return builder.Append(SeString.CreateMapLink(obj));
        }

        /// <summary>
        /// Appends a formatted map link payload for the position of a <see cref="WorldCoordinate"/>.
        /// </summary>
        /// <param name="worldPos">The world-space coordinate that should be linked.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the map link appended for method chaining.</returns>
        public SeStringBuilder PushMapLink(WorldCoordinate worldPos)
        {
            return builder.Append(SeString.CreateMapLink(worldPos));
        }

        /// <summary>
        /// Appends a formatted map link payload for the position of a <see cref="MapCoordinate"/>.
        /// </summary>
        /// <param name="mapPos">The map-space coordinate that should be linked.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the map link appended for method chaining.</returns>
        public SeStringBuilder PushMapLink(MapCoordinate mapPos)
        {
            return builder.Append(SeString.CreateMapLink(mapPos));
        }

        /// <summary>
        /// Appends a formatted party finder listing link payload.
        /// </summary>
        /// <param name="listingId">The listing Id of the party finder entry.</param>
        /// <param name="recruiterName">The name of the recruiter.</param>
        /// <param name="isCrossWorld">Whether the listing is limited to the current world or not.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the party finder link appended for method chaining.</returns>
        public SeStringBuilder PushPartyFinderLink(uint listingId, string recruiterName, bool isCrossWorld = false)
        {
            return builder.Append(SeString.CreatePartyFinderLink(listingId, recruiterName, isCrossWorld));
        }

        /// <summary>
        /// Appends a formatted party finder search conditions link payload.
        /// </summary>
        /// <param name="message">The text that should be displayed for the link.</param>
        /// <returns>The <see cref="SeStringBuilder"/> instance with the search conditions link appended for method chaining.</returns>
        public SeStringBuilder PushPartyFinderSearchConditionsLink(string message)
        {
            return builder.Append(SeString.CreatePartyFinderSearchConditionsLink(message));
        }

        /// <inheritdoc cref="SeStringBuilder.AppendIcon(uint)"/>
        public SeStringBuilder AppendIcon(BitmapFontIcon icon)
        {
            return builder.AppendIcon((uint)icon);
        }
    }
}

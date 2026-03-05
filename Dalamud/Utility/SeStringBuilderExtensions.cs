using Dalamud.Game.Text;

using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

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
}

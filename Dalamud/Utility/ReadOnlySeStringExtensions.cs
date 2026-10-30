using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

#pragma warning disable SA1101

/// <summary>
/// Extension methods for <see cref="ReadOnlySeString"/>.
/// </summary>
public static class ReadOnlySeStringExtensions
{
    extension(ReadOnlySeString input)
    {
        /// <inheritdoc cref="ReadOnlySeStringSpanExtensions.IsTextOnly(ReadOnlySeStringSpan)"/>
        public bool IsTextOnly()
        {
            return input.AsSpan().IsTextOnly();
        }

        /// <inheritdoc cref="ReadOnlySeStringSpanExtensions.ContainsText(ReadOnlySeStringSpan, ReadOnlySpan{byte})"/>
        public bool ContainsText(ReadOnlySpan<byte> needle)
        {
            return input.AsSpan().ContainsText(needle);
        }

        /// <inheritdoc cref="ReadOnlySeStringSpanExtensions.ContainsText(ReadOnlySeStringSpan, ReadOnlySpan{char})"/>
        public bool ContainsText(ReadOnlySpan<char> needle)
        {
            return input.AsSpan().ContainsText(needle);
        }

        /// <summary>
        /// Replaces occurrences of a specified text in a <see cref="ReadOnlySeString"/> with another text.
        /// </summary>
        /// <param name="toFind">The text to find.</param>
        /// <param name="replacement">The replacement text.</param>
        /// <returns>A new <see cref="ReadOnlySeString"/> with the replacements made.</returns>
        public ReadOnlySeString ReplaceText(ReadOnlySpan<byte> toFind, ReadOnlySpan<byte> replacement)
        {
            if (input.IsEmpty)
                return input;

            using var rssb = new RentedSeStringBuilder();

            foreach (var payload in input)
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
    }
}

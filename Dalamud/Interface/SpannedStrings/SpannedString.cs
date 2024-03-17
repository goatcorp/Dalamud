using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Utility;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>A character sequence with embedded styling information.</summary>
public sealed class SpannedString : ISpannableDataProvider, ISpanParsable<SpannedString>
{
    private static readonly (MethodInfo Info, SpannedParseInstructionAttribute Attr)[] SsbMethods =
        typeof(ISpannedStringBuilder<>)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .Select(
                x => (
                         Info: typeof(SpannedStringBuilder).GetMethod(
                             x.Name,
                             BindingFlags.Instance | BindingFlags.Public,
                             x.GetParameters().Select(y => y.ParameterType).ToArray()),
                         Attr: x.GetCustomAttribute<SpannedParseInstructionAttribute>()))
            .Where(x => x.Attr is not null)
            .OrderBy(x => x.Info.Name)
            .ThenByDescending(x => x.Info.GetParameters().Length)
            .ToArray();

    private readonly byte[] textStream;
    private readonly byte[] dataStream;
    private readonly SpannedRecord[] records;
    private readonly FontHandleVariantSet[] fontSets;
    private readonly IDalamudTextureWrap?[] textures;
    private readonly SpannedStringCallbackDelegate?[] callbacks;

    /// <summary>Initializes a new instance of the <see cref="SpannedString"/> class.</summary>
    /// <param name="textStream">The text storage.</param>
    /// <param name="dataStream">The link strorage.</param>
    /// <param name="records">The spans.</param>
    /// <param name="fontSets">The font sets.</param>
    /// <param name="textures">The textures.</param>
    /// <param name="callbacks">The callbacks.</param>
    internal SpannedString(
        byte[] textStream,
        byte[] dataStream,
        SpannedRecord[] records,
        FontHandleVariantSet[] fontSets,
        IDalamudTextureWrap?[] textures,
        SpannedStringCallbackDelegate?[] callbacks)
    {
        this.textStream = textStream;
        this.dataStream = dataStream;
        this.records = records;
        this.fontSets = fontSets;
        this.textures = textures;
        this.callbacks = callbacks;
    }

    /// <summary>Gets the font handle sets.</summary>
    public IList<FontHandleVariantSet> FontHandleSets => this.fontSets;

    /// <summary>Gets the textures.</summary>
    public IList<IDalamudTextureWrap?> Textures => this.textures;

    /// <summary>Gets the callbacks.</summary>
    public IList<SpannedStringCallbackDelegate?> Callbacks => this.callbacks;

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)"/>
    public static SpannedString Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var result, out var exception) ? result : throw exception;

    /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)"/>
    public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)] out SpannedString? result) =>
        TryParse(s, provider, out result, out _);

    /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)"/>
    public static SpannedString Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var result, out var exception) ? result : throw exception;

    /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)"/>
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out SpannedString? result) =>
        TryParse(s, provider, out result, out _);

    /// <summary>Decodes a spannable from source bytes.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="value">The decoded instance.</param>
    /// <returns><c>true</c> if decoded.</returns>
    /// <remarks>Font handles and textures need to be supplied back separately.</remarks>
    public static bool TryDecode(ReadOnlySpan<byte> source, [NotNullWhen(true)] out SpannedString? value)
    {
        value = null;
        if (!Utf8Value.TryDecode(ref source, out var version, out _))
            return false;
        if (version != 1)
            return false;

        if (!Utf8Value.TryDecode(ref source, out var textLength, out _))
            return false;
        if (!Utf8Value.TryDecode(ref source, out var dataLength, out _))
            return false;
        if (!Utf8Value.TryDecode(ref source, out var numRecords, out _))
            return false;
        if (!Utf8Value.TryDecode(ref source, out var numFontSets, out _))
            return false;
        if (!Utf8Value.TryDecode(ref source, out var numTextures, out _))
            return false;
        if (!Utf8Value.TryDecode(ref source, out var numCallbacks, out _))
            return false;

        if (source.Length < textLength.IntValue)
            return false;
        var textByteSpan = source[..textLength.IntValue];
        source = source[textLength.IntValue..];

        if (source.Length < dataLength.IntValue)
            return false;
        var dataByteSpan = source[..dataLength.IntValue];
        source = source[dataLength.IntValue..];

        var records = new SpannedRecord[numRecords];
        foreach (ref var rec in records.AsSpan())
        {
            if (!SpannedRecord.TryDecode(ref source, out rec, out _))
                return false;
        }

        value = new(
            textByteSpan.ToArray(),
            dataByteSpan.ToArray(),
            records,
            new FontHandleVariantSet[numFontSets],
            new IDalamudTextureWrap?[numTextures],
            new SpannedStringCallbackDelegate?[numCallbacks]);
        return true;
    }

    /// <summary>Encodes this spannable for serialization.</summary>
    /// <param name="target">The stream to encode to. Optional.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Font handles and textures are not serialized.</remarks>
    public int Encode(Span<byte> target)
    {
        var length = 0;
        length += Utf8Value.Encode(ref target, 1); // version
        length += Utf8Value.Encode(ref target, this.textStream.Length);
        length += Utf8Value.Encode(ref target, this.dataStream.Length);
        length += Utf8Value.Encode(ref target, this.records.Length);
        length += Utf8Value.Encode(ref target, this.fontSets.Length);
        length += Utf8Value.Encode(ref target, this.textures.Length);
        length += Utf8Value.Encode(ref target, this.callbacks.Length);

        length += this.textStream.Length;
        if (!target.IsEmpty)
        {
            this.textStream.AsSpan().CopyTo(target);
            target = target[this.textStream.Length..];
        }

        length += this.dataStream.Length;
        if (!target.IsEmpty)
        {
            this.dataStream.AsSpan().CopyTo(target);
            target = target[this.dataStream.Length..];
        }

        foreach (ref var rec in this.records.AsSpan())
            length += rec.Encode(ref target);

        return length;
    }

    /// <inheritdoc/>
    public override string ToString() => this.ToString(null);

    /// <inheritdoc cref="object.ToString"/>
    public string ToString(IFormatProvider? formatProvider)
    {
        var sb = new StringBuilder();
        Span<bool> typeNeedsReverting = stackalloc bool[1 + Enum.GetValues<SpannedRecordType>().Length];
        foreach (var segment in this.GetData())
        {
            if (segment.TryGetRawText(out var text))
            {
                foreach (var c in text.AsUtf8Enumerable())
                {
                    if (c.Value.TryGetRune(out var rune))
                        sb.Append(rune.Value == '{' ? "{{" : rune.ToString());
                    else
                        sb.Append('\uFFFD');
                }
            }
            else if (segment.TryGetRecord(out var record, out var data))
            {
                foreach (var method in SsbMethods)
                {
                    if (method.Attr.RecordType != record.Type || method.Attr.IsRevert != record.IsRevert)
                        continue;
                    if (record.IsRevert)
                    {
                        sb.Append('{').Append(method.Attr.Name).Append('}');
                        typeNeedsReverting[(int)record.Type] = false;
                        break;
                    }

                    if (typeNeedsReverting[(int)record.Type])
                    {
                        // Write stack revert if possible
                        foreach (var m2 in SsbMethods)
                        {
                            if (m2.Attr.RecordType != record.Type || !m2.Attr.IsRevert)
                                continue;
                            sb.Append('{').Append(m2.Attr.Name).Append('}');
                            break;
                        }
                    }

                    sb.Append('{').Append(method.Attr.Name);
                    record.WritePushParameters(sb, data, formatProvider);
                    sb.Append('}');
                    typeNeedsReverting[(int)record.Type] = true;
                    break;
                }
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    SpannedStringData ISpannableDataProvider.GetData() => this.GetData();

    /// <summary>Tries to parse a span of characters into a value.</summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information about
    /// <paramref name="s"/>.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/>,
    /// or an undefined value on failure.</param>
    /// <param name="exception">Exception, on any.</param>
    /// <returns><c>true</c> if <paramref name="s"/> was successfuly parsed.</returns>
    internal static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out SpannedString? result,
        [NotNullWhen(false)] out Exception? exception)
    {
        const string whitespaces = " \t\r\n";
        const string argumentEndTokens = " \t\r\n}";

        result = null;
        exception = null;
        var ssb = new SpannedStringBuilder();
        var ms = default(MemoryStream);
        while (!s.IsEmpty)
        {
            // Find instructions region.
            var opener = s.IndexOf('{');
            if (opener == -1)
            {
                ssb.Append(s);
                break;
            }

            ssb.Append(s[..opener]);
            s = s[opener..];

            // Doubles cascade into one.
            if (s.StartsWith("{{"))
            {
                ssb.Append('{');
                s = s[2..];
                continue;
            }

            s = s[1..];

            var sep = s.IndexOfAny(argumentEndTokens);
            if (sep == -1)
            {
                exception = new FormatException("Missing }");
                return false;
            }

            var name = s[..sep];
            s = s[sep..];

            var found = false;
            foreach (var method in SsbMethods)
            {
                if (!method.Attr.Matches(name))
                    continue;

                var allArgsSpan = s;
                if (method.Info.Name == nameof(SpannedStringBuilder.PushLink))
                {
                    // special-casing because of ReadOnlySpan arg

                    ms ??= new();
                    if (!ConsumeEscapedStringToken(ref allArgsSpan, ms))
                        continue;
                    ssb.PushLink(ms.GetDataSpan());
                    ms.Clear();
                    found = true;
                    s = allArgsSpan;
                    break;
                }

                var argDefinitions = method.Info.GetParameters();
                var argValues = new object[argDefinitions.Length];
                var valid = true;
                for (var i = 0; i < argDefinitions.Length; i++)
                {
                    if (!ConsumeArgumentToken(ref allArgsSpan, out var arg))
                    {
                        if (argDefinitions[i].HasDefaultValue)
                        {
                            argValues[i] = argDefinitions[i].DefaultValue;
                            continue;
                        }

                        valid = false;
                        break;
                    }

                    var ptype = argDefinitions[i].ParameterType;
                    if (ptype.IsEnum)
                    {
                        var parseArgs = new object?[]
                        {
                            ptype,
                            arg.Trim().ToString(),
                            true,
                            null,
                        };
                        var parseResult =
                            (bool)typeof(Enum)
                                  .GetMethod(
                                      nameof(Enum.TryParse),
                                      BindingFlags.Static | BindingFlags.Public,
                                      new[]
                                      {
                                          typeof(Type),
                                          typeof(string),
                                          typeof(bool),
                                          typeof(object).MakeByRefType(),
                                      })!
                                  .Invoke(null, parseArgs)!;
                        if (!parseResult)
                        {
                            exception = new FormatException($"Failed to parse: {ptype} {name} at #{i}");
                            valid = false;
                            break;
                        }

                        argValues[i] = parseArgs[3];
                    }
                    else if (ptype == typeof(Vector2))
                    {
                        if (!ConsumeArgumentToken(ref allArgsSpan, out var arg2)
                            || !float.TryParse(arg, provider, out var v1)
                            || !float.TryParse(arg2, provider, out var v2))
                        {
                            exception = new FormatException($"Failed to parse: {ptype} {name} at #{i}");
                            valid = false;
                            break;
                        }

                        argValues[i] = new Vector2(v1, v2);
                    }
                    else if (ptype.GetInterfaces().Any(
                                 x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISpanParsable<>)))
                    {
                        var parseArgs = new[]
                        {
                            arg.ToString(),
                            provider,
                            Activator.CreateInstance(ptype),
                        };
                        var parseResult =
                            (bool)ptype
                                  .GetMethod(
                                      nameof(ISpanParsable<int>.TryParse),
                                      BindingFlags.Static | BindingFlags.Public,
                                      new[]
                                      {
                                          typeof(string),
                                          typeof(IFormatProvider),
                                          ptype.MakeByRefType(),
                                      })!
                                  .Invoke(null, parseArgs)!;
                        if (!parseResult)
                        {
                            exception = new FormatException($"Failed to parse: {ptype} {name} at #{i}");
                            valid = false;
                            break;
                        }

                        argValues[i] = parseArgs[2];
                    }
                    else
                    {
                        exception = new FormatException($"Failed to parse: {ptype} {name} at #{i}");
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;

                method.Info.Invoke(ssb, argValues);

                s = allArgsSpan;
                found = true;
                break;
            }

            if (!found)
            {
                exception ??= new FormatException($"Unknown instruction: {name}");
                return false;
            }

            s = s.TrimStart();
            if (!s.StartsWith("}"))
            {
                exception = new FormatException("Closer is missing");
                return false;
            }

            s = s[1..];
        }

        result = ssb.Build();
        return true;

        static bool ConsumeArgumentToken(ref ReadOnlySpan<char> from, out ReadOnlySpan<char> arg)
        {
            arg = default;

            var test = from.TrimStart(whitespaces);
            if (test.IsEmpty || test[0] == '}')
                return false;

            Span<char> parenthesisStack = stackalloc char[64];
            var parenthesisLevel = 0;
            var len = 0;
            for (var test2 = test; !test2.IsEmpty; len++)
            {
                if (parenthesisLevel == 0 && argumentEndTokens.Contains(test2[0]))
                    break;
                switch (test2[0])
                {
                    case '(' or '[' or '{' or '<':
                        if (parenthesisLevel == parenthesisStack.Length)
                            return false;
                        parenthesisStack[parenthesisLevel++] = test2[0] switch
                        {
                            '(' => ')',
                            '[' => ']',
                            '{' => '}',
                            '<' => '>',
                            _ => throw new InvalidOperationException(),
                        };
                        break;
                    case ')' or ']' or '}' or '>':
                        if (parenthesisLevel == 0 || parenthesisStack[--parenthesisLevel] != test2[0])
                            return false;
                        break;
                }

                test2 = test2[1..];
            }

            arg = test[..len];
            from = test[len..];
            return !arg.IsEmpty;
        }

        static bool ConsumeEscapedStringToken(ref ReadOnlySpan<char> from, MemoryStream writeTo)
        {
            from = from.TrimStart();
            if (from.IsEmpty)
                return false;
            if (argumentEndTokens.Contains(from[0]))
                return true;

            var currentQuote = '\0';
            while (!from.IsEmpty)
            {
                if (currentQuote == 0 && argumentEndTokens.Contains(from[0]))
                    break;

                switch (from[0])
                {
                    case '\'' when currentQuote == '\'':
                    case '"' when currentQuote == '"':
                        currentQuote = '\0';
                        from = from[1..];
                        continue;
                    case '\'' when currentQuote == 0:
                    case '"' when currentQuote == 0:
                        currentQuote = from[0];
                        from = from[1..];
                        continue;
                    case var _ when from.Length >= 2 && char.IsSurrogatePair(from[0], from[1]):
                    {
                        var rune = new Rune(from[0], from[1]);
                        var off = (int)writeTo.Length;
                        writeTo.SetLength(writeTo.Position = off + rune.Utf8SequenceLength);
                        rune.EncodeToUtf8(writeTo.GetBuffer().AsSpan(off));
                        from = from[2..];
                        continue;
                    }

                    case not '\\' when Rune.IsValid(from[0]):
                    case '\\' when from.Length < 2:
                    {
                        var rune = new Rune(from[0]);
                        var off = (int)writeTo.Length;
                        writeTo.SetLength(writeTo.Position = off + rune.Utf8SequenceLength);
                        rune.EncodeToUtf8(writeTo.GetBuffer().AsSpan(off));
                        from = from[1..];
                        continue;
                    }

                    case not '\\':
                    {
                        var rune = Rune.ReplacementChar;
                        var off = (int)writeTo.Length;
                        writeTo.SetLength(writeTo.Position = off + rune.Utf8SequenceLength);
                        rune.EncodeToUtf8(writeTo.GetBuffer().AsSpan(off));
                        from = from[1..];
                        continue;
                    }
                }

                switch (from[1])
                {
                    case '\'' or '"' or '\\':
                        writeTo.WriteByte((byte)from[1]);
                        from = from[2..];
                        continue;
                    case '0':
                        writeTo.WriteByte(0);
                        from = from[2..];
                        continue;
                    case 'a':
                        writeTo.WriteByte((byte)'\a');
                        from = from[2..];
                        continue;
                    case 'b':
                        writeTo.WriteByte((byte)'\b');
                        from = from[2..];
                        continue;
                    case 'f':
                        writeTo.WriteByte((byte)'\f');
                        from = from[2..];
                        continue;
                    case 'n':
                        writeTo.WriteByte((byte)'\n');
                        from = from[2..];
                        continue;
                    case 'r':
                        writeTo.WriteByte((byte)'\r');
                        from = from[2..];
                        continue;
                    case 't':
                        writeTo.WriteByte((byte)'\t');
                        from = from[2..];
                        continue;
                    case 'v':
                        writeTo.WriteByte((byte)'\v');
                        from = from[2..];
                        continue;
                    case 'x' when from.Length >= 4:
                        if (!byte.TryParse(from[2..4], NumberStyles.HexNumber, null, out var u8))
                            return false;
                        writeTo.WriteByte(u8);
                        from = from[4..];
                        continue;
                    case 'u' when from.Length >= 6:
                        if (!uint.TryParse(from[2..6], NumberStyles.HexNumber, null, out var u32))
                            return false;
                        Utf8Value.Encode(writeTo, (int)u32);
                        from = from[6..];
                        continue;
                    case 'U' when from.Length >= 10:
                        if (!uint.TryParse(from[2..10], NumberStyles.HexNumber, null, out u32))
                            return false;
                        Utf8Value.Encode(writeTo, (int)u32);
                        from = from[10..];
                        continue;
                    default:
                        return false;
                }
            }

            return true;
        }
    }

    /// <inheritdoc cref="ISpannableDataProvider.GetData"/>
    internal SpannedStringData GetData() =>
        new(this.textStream, this.dataStream, this.records, this.fontSets, this.textures, this.callbacks);
}

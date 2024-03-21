using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Utility;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.SpannedStrings.Spannables;

/// <summary>A UTF-8 character sequence with embedded styling information.</summary>
public sealed partial class SpannedString
{
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
        if (!UtfValue.TryDecode8(ref source, out var version, out _))
            return false;
        if (version != 1)
            return false;

        if (!UtfValue.TryDecode8(ref source, out var textLength, out _))
            return false;
        if (!UtfValue.TryDecode8(ref source, out var dataLength, out _))
            return false;
        if (!UtfValue.TryDecode8(ref source, out var numRecords, out _))
            return false;
        if (!UtfValue.TryDecode8(ref source, out var numFontSets, out _))
            return false;
        if (!UtfValue.TryDecode8(ref source, out var numTextures, out _))
            return false;
        if (!UtfValue.TryDecode8(ref source, out var numCallbacks, out _))
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

        var fontSets = new FontHandleVariantSet[numFontSets];
        foreach (ref var f in fontSets.AsSpan())
        {
            if (!SpannedRecordCodec.TryDecode(ref source, out f.FontFamilyId))
                return false;
        }

        value = new(
            textByteSpan.ToArray(),
            dataByteSpan.ToArray(),
            records,
            fontSets,
            new IDalamudTextureWrap?[numTextures],
            new ISpannable?[numCallbacks]);
        return true;
    }

    /// <summary>Encodes this spannable for serialization.</summary>
    /// <param name="target">The stream to encode to. Optional.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Font handles and textures are not serialized.</remarks>
    public int Encode(Span<byte> target)
    {
        var length = 0;
        length += UtfValue.Encode8(ref target, 1); // version
        length += UtfValue.Encode8(ref target, this.textStream.Length);
        length += UtfValue.Encode8(ref target, this.dataStream.Length);
        length += UtfValue.Encode8(ref target, this.records.Length);
        length += UtfValue.Encode8(ref target, this.fontSets.Length);
        length += UtfValue.Encode8(ref target, this.textures.Length);
        length += UtfValue.Encode8(ref target, this.spannables.Length);

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

        foreach (ref var f in this.fontSets.AsSpan())
            length += SpannedRecordCodec.Encode(ref target, f.FontFamilyId);

        return length;
    }

    /// <inheritdoc cref="object.ToString"/>
    public string ToString(IFormatProvider? formatProvider)
    {
        var sb = new StringBuilder();
        Span<bool> typeNeedsReverting = stackalloc bool[1 + Enum.GetValues<SpannedRecordType>().Length];
        foreach (var segment in this.GetData())
        {
            if (segment.TryGetRawText(out var text))
            {
                foreach (var c in text.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                {
                    if (c.Value.TryGetRune(out var rune))
                        sb.Append(rune.Value == '{' ? "{{" : rune.ToString());
                    else
                        sb.Append('\uFFFD');
                }
            }
            else if (segment.TryGetRecord(out var record, out var data))
            {
                // Special shortcuts for font sets.
                if (record.Type == SpannedRecordType.FontHandleSetIndex
                    && SpannedRecordCodec.TryDecodeFontHandleSetIndex(data, out var setIndex))
                {
                    var functionName = this.fontSets[setIndex].FontFamilyId switch
                    {
                        DalamudDefaultFontAndFamilyId => nameof(ISpannedStringBuilder.PushDefaultFontFamily),
                        DalamudAssetFontAndFamilyId => nameof(ISpannedStringBuilder.PushAssetFontFamily),
                        GameFontAndFamilyId => nameof(ISpannedStringBuilder.PushGameFontFamily),
                        SystemFontFamilyId => nameof(ISpannedStringBuilder.PushSystemFontFamilyIfAvailable),
                        not null => nameof(ISpannedStringBuilder.PushFontFamily),
                        _ => nameof(ISpannedStringBuilder.PushFontSet),
                    };
                    sb.Append('{').Append(SsbMethods.Single(x => x.Info.Name == functionName).Attr.Name);
                    record.WritePushParameters(sb, data, this.fontSets, formatProvider);
                    sb.Append('}');
                    continue;
                }

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
                    record.WritePushParameters(sb, data, this.fontSets, formatProvider);
                    sb.Append('}');
                    typeNeedsReverting[(int)record.Type] = true;
                    break;
                }
            }
        }

        return sb.ToString();
    }

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
        const string flagEnumSeps = " \t\r\n,|/\\;";

        result = null;
        exception = null;
        var sOrig = s;
        var ssb = new SpannedStringBuilder();
        var ms = new MemoryStream();
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
                exception ??= new FormatException($"Missing }} at offset {sOrig.Length - s.Length}");
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

                    ms.Clear();
                    if (!ConsumeArgumentToken(ref allArgsSpan, ms))
                        continue;
                    ssb.PushLink(ms.GetDataSpan());
                    found = true;
                    s = allArgsSpan;
                    break;
                }

                var argDefinitions = method.Info.GetParameters();
                var argValues = new object[argDefinitions.Length];
                var valid = true;
                for (var i = 0; i < argDefinitions.Length; i++)
                {
                    ms.Clear();
                    if (!ConsumeArgumentToken(ref allArgsSpan, ms))
                    {
                        if (argDefinitions[i].HasDefaultValue)
                        {
                            argValues[i] = argDefinitions[i].DefaultValue;
                            continue;
                        }

                        valid = false;
                        break;
                    }

                    var arg = Encoding.UTF8.GetString(ms.GetDataSpan());
                    var ptype = argDefinitions[i].ParameterType;
                    if (ptype.IsEnum)
                    {
                        arg = arg.Trim();
                        var parseArgs = new object?[]
                        {
                            ptype,
                            arg,
                            true,
                            null,
                        };
                        var parseResult =
                            (bool)typeof(Enum)
                                  .GetMethod(
                                      nameof(Enum.TryParse),
                                      BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                      [
                                          typeof(Type),
                                          typeof(string),
                                          typeof(bool),
                                          typeof(object).MakeByRefType()
                                      ])!
                                  .Invoke(null, parseArgs)!;
                        if (!parseResult && !TryParseFlagsEnum(ptype, arg, out parseArgs[3], out var ex2))
                        {
                            exception = ex2 ?? new FormatException(
                                            $"Failed to parse: {ptype} {name} at #{i} at offset {sOrig.Length - s.Length}");
                            valid = false;
                            break;
                        }

                        argValues[i] = parseArgs[3];
                    }
                    else if (ptype == typeof(Vector2))
                    {
                        if (!float.TryParse(arg, provider, out var v1))
                        {
                            exception ??= new FormatException(
                                $"Failed to parse: {ptype} {name} at #{i} at offset {sOrig.Length - s.Length}");
                            valid = false;
                            break;
                        }

                        ms.Clear();
                        if (!ConsumeArgumentToken(ref allArgsSpan, ms)
                            || !float.TryParse(Encoding.UTF8.GetString(ms.GetDataSpan()), provider, out var v2))
                        {
                            exception ??= new FormatException(
                                $"Failed to parse: {ptype} {name} at #{i} at offset {sOrig.Length - s.Length}");
                            valid = false;
                            break;
                        }

                        argValues[i] = new Vector2(v1, v2);
                    }
                    else if (ptype.GetInterfaces().Any(
                                 x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISpanParsable<>)))
                    {
                        var parseArgs = new object?[]
                        {
                            arg,
                            provider,
                            null,
                        };
                        var parseResult =
                            (bool)GetTryParseMethod(
                                    ptype,
                                    typeof(string),
                                    typeof(IFormatProvider),
                                    ptype.MakeByRefType())
                                .Invoke(null, parseArgs)!;
                        if (!parseResult)
                        {
                            exception ??= new FormatException(
                                $"Failed to parse: {ptype} {name} at #{i} at offset {sOrig.Length - s.Length}");
                            valid = false;
                            break;
                        }

                        argValues[i] = parseArgs[2];
                    }
                    else
                    {
                        exception ??= new FormatException(
                            $"Failed to parse: {ptype} {name} at #{i} at offset {sOrig.Length - s.Length}");
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    exception ??= new FormatException($"Failed to parse: {name} at offset {sOrig.Length - s.Length}");
                    continue;
                }

                method.Info.Invoke(ssb, argValues);

                s = allArgsSpan;
                found = true;
                break;
            }

            if (name.StartsWith("\\x", StringComparison.InvariantCultureIgnoreCase)
                || name.StartsWith("\\u", StringComparison.InvariantCultureIgnoreCase))
            {
                if (int.TryParse(
                        name[2..],
                        NumberStyles.HexNumber
                        | NumberStyles.AllowLeadingWhite
                        | NumberStyles.AllowTrailingWhite,
                        provider,
                        out var codepoint))
                {
                    ssb.AppendChar(codepoint);
                    found = true;
                }
            }

            if (!found)
            {
                exception ??= new FormatException($"Unknown instruction: {name}");
                return false;
            }

            exception = null;
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

        static MethodInfo GetTryParseMethod(Type type, params Type[] parameterTypes)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var m in methods)
            {
                if (!m.Name.Equals("TryParse", StringComparison.Ordinal)
                    && !m.Name.EndsWith(".TryParse", StringComparison.Ordinal))
                    continue;
                var ps = m.GetParameters();
                if (ps.Length != parameterTypes.Length)
                    continue;
                var i = 0;
                for (; i < ps.Length; i++)
                {
                    if (ps[i].ParameterType != parameterTypes[i])
                        break;
                }

                if (i != ps.Length)
                    continue;
                return m;
            }

            throw new NullReferenceException($"TryParse could not be found in {type}.");
        }

        static bool ConsumeArgumentToken(ref ReadOnlySpan<char> from, MemoryStream writeTo)
        {
            from = from.TrimStart(whitespaces);
            if (from.IsEmpty || from[0] == '}')
                return false;
            if (argumentEndTokens.Contains(from[0]))
                return true;

            var currentQuote = '\0';
            Span<char> parenthesisStack = stackalloc char[64];
            var parenthesisLevel = 0;
            while (!from.IsEmpty)
            {
                if (currentQuote == 0 && parenthesisLevel == 0 && argumentEndTokens.Contains(from[0]))
                    break;

                switch (from[0])
                {
                    case '(' or '[' or '{' or '<' when currentQuote == 0:
                        if (parenthesisLevel == parenthesisStack.Length)
                            return false;
                        parenthesisStack[parenthesisLevel++] = from[0] switch
                        {
                            '(' => ')',
                            '[' => ']',
                            '{' => '}',
                            '<' => '>',
                            _ => throw new InvalidOperationException(),
                        };
                        goto default;

                    case ')' or ']' or '}' or '>' when currentQuote == 0:
                        if (parenthesisLevel == 0 || parenthesisStack[--parenthesisLevel] != from[0])
                            return false;
                        goto default;

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

                    case '\\' when from.Length >= 2:
                        break;

                    default:
                    {
                        var rune = Rune.IsValid(from[0]) ? new(from[0]) : Rune.ReplacementChar;
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
                        UtfValue.Encode8(writeTo, (int)u32);
                        from = from[6..];
                        continue;
                    case 'U' when from.Length >= 10:
                        if (!uint.TryParse(from[2..10], NumberStyles.HexNumber, null, out u32))
                            return false;
                        UtfValue.Encode8(writeTo, (int)u32);
                        from = from[10..];
                        continue;
                    default:
                        return false;
                }
            }

            return true;
        }

        static bool TryParseFlagsEnum(Type enumType, ReadOnlySpan<char> span, out object result, out Exception? exc)
        {
            result = Activator.CreateInstance(enumType)!;
            exc = null;
            var isFlag = enumType.GetCustomAttribute<FlagsAttribute>() is not null;
            var foundCount = 0;
            while (!span.IsEmpty)
            {
                var sep = span.IndexOfAny(flagEnumSeps);
                var current = sep == -1 ? span : span[..sep];
                span = sep == -1 ? default : span[(sep + 1)..];

                object? foundValue = null;
                foreach (var name in Enum.GetNames(enumType))
                {
                    var efield = enumType.GetField(name)!;
                    if (current.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                        || efield.GetCustomAttribute<SpannedParseShortNameAttribute>()?.Matches(current) is true)
                    {
                        foundValue = efield.GetRawConstantValue()!;
                        break;
                    }
                }

                if (foundValue != null)
                {
                    foundCount++;
                    if (foundCount > 1 && !isFlag)
                    {
                        exc = new FormatException($"{enumType} is not a flag enum.");
                        return false;
                    }

                    var ut = enumType.GetEnumUnderlyingType();
                    object? ored;
                    if (ut == typeof(byte))
                        ored = (byte)((byte)foundValue | (byte)result);
                    else if (ut == typeof(sbyte))
                        ored = (sbyte)((sbyte)foundValue | (sbyte)result);
                    else if (ut == typeof(short))
                        ored = (short)((short)foundValue | (short)result);
                    else if (ut == typeof(ushort))
                        ored = (ushort)((ushort)foundValue | (ushort)result);
                    else if (ut == typeof(int))
                        ored = (int)foundValue | (int)result;
                    else if (ut == typeof(uint))
                        ored = (uint)foundValue | (uint)result;
                    else if (ut == typeof(long))
                        ored = (long)foundValue | (long)result;
                    else if (ut == typeof(ulong))
                        ored = (ulong)foundValue | (ulong)result;
                    else
                        ored = null;
                    if (ored is null)
                    {
                        exc = new FormatException($"{current} has an {enumType} unsupported for or operation.");
                        return false;
                    }

                    result = ored;
                }
                else
                {
                    exc = new FormatException($"{current} is an invalid {enumType}.");
                    return false;
                }
            }

            return true;
        }
    }
}

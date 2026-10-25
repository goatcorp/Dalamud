using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Configuration.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Writes and reads text styles as plain values, and reads the font specs Dalamud saved before.</summary>
/// <remarks><ul>
/// <li>Old <c>SingleFontSpec</c> objects are read with their glyph ranges, offset and spacing as a
/// <see cref="ChainFamilyId"/>. Old font id type names are read as those of <see cref="Interface.Fonts"/>, by
/// <see cref="MovedTypeSerializationBinder.MapTypeName"/>.</li>
/// <li><see cref="TextStyle.ExtraDecoration"/> is written only for built-in decorations.</li>
/// <li>An unknown <see cref="TextStyle.Locale"/> is read as null.</li>
/// </ul></remarks>
internal sealed class TextStyleJsonConverter : JsonConverter
{
    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) =>
        objectType == typeof(TextStyle) || objectType == typeof(TextStyle?);

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not TextStyle s)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        if (s.FamilyId is { } family)
        {
            writer.WritePropertyName(nameof(TextStyle.FamilyId));
            serializer.Serialize(writer, family, typeof(IFontFamilyId));
        }

        var o = new JObject();
        Add(o, nameof(s.Size), s.Size);
        Add(o, nameof(s.SizeBasis), s.SizeBasis.ToString(), nameof(FontSizeBasis.Auto));
        Add(o, nameof(s.LineHeight), s.LineHeight);
        Add(o, nameof(s.Color), s.Color.Value);
        if (s.Shadow.IsOn)
        {
            o[nameof(s.Shadow)] = new JObject
            {
                ["Color"] = s.Shadow.Color.Value,
                ["X"] = s.Shadow.Offset.X,
                ["Y"] = s.Shadow.Offset.Y,
                ["Unit"] = s.Shadow.Unit.ToString(),
            };
        }

        if (s.Edge.IsOn)
        {
            o[nameof(s.Edge)] = new JObject
            {
                ["Color"] = s.Edge.Color.Value,
                ["Width"] = s.Edge.Width.Unit == StyleUnit.Default ? null : s.Edge.Width.ToString(),
                ["Softness"] = s.Edge.Softness,
            };
        }

        Add(o, nameof(s.Background), s.Background.Value);
        if (s.Lines.IsOn)
        {
            o[nameof(s.Lines)] = new JObject
            {
                ["Positions"] = (int)s.Lines.Positions,
                ["Style"] = (int)s.Lines.Style,
                ["Color"] = s.Lines.Color.Value,
                ["Thickness"] = s.Lines.Thickness,
                ["Count"] = s.Lines.Count,
            };
        }

        if (s.ExtraDecoration.Id.IsBuiltIn)
        {
            o[nameof(s.ExtraDecoration)] = new JObject
            {
                ["Id"] = s.ExtraDecoration.Id.Value,
                ["Context"] = s.ExtraDecoration.Context,
                ["Data"] = s.ExtraDecoration.Data,
            };
        }

        Add(o, nameof(s.Bold), s.Bold);
        Add(o, nameof(s.Italic), s.Italic);
        Add(o, nameof(s.Markup), s.Markup.ToString(), TextMarkup.Plain.ToString());
        Add(o, nameof(s.NoSubpixel), s.NoSubpixel);
        Add(o, nameof(s.NoHinting), s.NoHinting);
        Add(o, nameof(s.Weight), s.Weight);
        Add(o, nameof(s.Stretch), s.Stretch);
        Add(o, nameof(s.LetterSpacing), s.LetterSpacing);
        Add(o, nameof(s.Locale), s.Locale);
        if (!s.Features.IsEmpty)
            o[nameof(s.Features)] = new JArray(s.Features.Select(static f => new JArray(f.TagName, f.Value)));
        foreach (var p in o.Properties())
            p.WriteTo(writer);
        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public override object? ReadJson(
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return objectType == typeof(TextStyle) ? default(TextStyle) : null;
        var o = JObject.Load(reader);
        o.Remove("$type");
        foreach (var p in o.Descendants().OfType<JProperty>().Where(static p => p.Name == "$type").ToList())
        {
            if ((string?)p.Value is { } typeName)
                p.Value = MovedTypeSerializationBinder.MapTypeName(typeName);
        }

        if (o.ContainsKey("FontId"))
            return ReadLegacy(o, serializer);

        var s = new TextStyle
        {
            FamilyId = o[nameof(TextStyle.FamilyId)]?.ToObject<IFontFamilyId>(serializer),
            Size = StyleLength.Parse((string?)o[nameof(TextStyle.Size)]),
            SizeBasis = Enum.TryParse<FontSizeBasis>((string?)o[nameof(TextStyle.SizeBasis)], out var b) ? b : default,
            LineHeight = StyleLength.Parse((string?)o[nameof(TextStyle.LineHeight)]),
            Color = (uint?)o[nameof(TextStyle.Color)] ?? 0,
            Background = (uint?)o[nameof(TextStyle.Background)] ?? 0,
            Bold = (bool?)o[nameof(TextStyle.Bold)] ?? false,
            Italic = (bool?)o[nameof(TextStyle.Italic)] ?? false,
            Markup = Enum.TryParse<TextMarkup>((string?)o[nameof(TextStyle.Markup)], out var m) ? m : default,
            NoSubpixel = (bool?)o[nameof(TextStyle.NoSubpixel)] ?? false,
            NoHinting = (bool?)o[nameof(TextStyle.NoHinting)] ?? false,
            Weight = (int?)o[nameof(TextStyle.Weight)] ?? 0,
            Stretch = (int?)o[nameof(TextStyle.Stretch)] ?? 0,
            LetterSpacing = StyleLength.Parse((string?)o[nameof(TextStyle.LetterSpacing)]),
            Features =
            [
                .. o[nameof(TextStyle.Features)]?.Select(static f => new FontFeature((string)f[0]!, (uint)f[1]!)) ?? [],
            ],
        };
        try
        {
            s.Locale = (string?)o[nameof(TextStyle.Locale)];
        }
        catch (CultureNotFoundException)
        {
        }

        if (o[nameof(TextStyle.Shadow)] is JObject sh)
        {
            s.Shadow = new(
                (uint)sh["Color"]!,
                new((float)sh["X"]!, (float)sh["Y"]!),
                Enum.Parse<StyleUnit>((string)sh["Unit"]!));
        }

        if (o[nameof(TextStyle.Edge)] is JObject e)
        {
            s.Edge = new((uint)e["Color"]!, StyleLength.Parse((string?)e["Width"]))
            {
                Softness = (float?)e["Softness"] ?? 0,
            };
        }

        if (o[nameof(TextStyle.Lines)] is JObject l)
        {
            s.Lines = new(
                (TextLinePosition)(int)l["Positions"]!,
                (TextLineStyle)(int)l["Style"]!,
                (uint?)l["Color"] ?? 0,
                (float?)l["Thickness"] ?? 0,
                (byte?)l["Count"] ?? 1);
        }

        if (o[nameof(TextStyle.ExtraDecoration)] is JObject d)
            s.ExtraDecoration = new(new((uint)d["Id"]!), (uint?)d["Context"] ?? 0, (ulong?)d["Data"] ?? 0);
        return s;
    }

    private static void Add<T>(JObject o, string name, T value, T defaultValue = default!)
    {
        if (!Equals(value, defaultValue))
            o[name] = value is StyleLength length ? length.ToString() : JToken.FromObject(value!);
    }

    private static TextStyle ReadLegacy(JObject o, JsonSerializer serializer)
    {
        var sizePx = (float?)o["SizePx"] ?? 0;
        var k = sizePx > 0 ? 1 / sizePx : 1;
        var face = o["FontId"]?.ToObject<IFontFaceId>(serializer);
        if (face is DalamudAssetFontId a && (int?)o["FontNo"] is { } fontNo and not 0)
            face = new DalamudAssetFontId(a.Asset, fontNo);
        var ranges = o["GlyphRanges"] is JObject wrapped ? wrapped["$values"] : o["GlyphRanges"];
        var offset = o["GlyphOffset"];
        var element = face is null
                          ? null
                          : new ChainFamilyId.Element(face.Family)
                          {
                              CodepointRanges =
                                  [.. ranges?.Select(static r => (uint)r).TakeWhile(static r => r != 0) ?? []],
                              GlyphOffset = offset is null
                                                ? default
                                                : new Vector2((float)offset["X"]!, (float)offset["Y"]!) * k,
                              LetterSpacing = (float?)o["LetterSpacing"] is { } ls and not 0
                                                  ? StyleLength.Em(ls * k)
                                                  : default,
                          };
        return new TextStyle
        {
            FamilyId = element is null ? null
                       : element == new ChainFamilyId.Element(element.Family) ? element.Family
                       : new ChainFamilyId([element]),
            Size = sizePx > 0 ? StyleLength.Px(sizePx) : default,
            LineHeight = (float?)o["LineHeight"] is > 0 and not 1 and var lh ? StyleLength.Em(lh) : default,
        }.WithFace(face?.Style ?? default);
    }
}

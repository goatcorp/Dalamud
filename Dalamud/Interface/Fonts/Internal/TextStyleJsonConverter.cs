using System.Collections.Frozen;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Configuration.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Serializes <see cref="TextStyle"/> values and deserializes both current styles and legacy <c>SingleFontSpec</c> JSON.</summary>
/// <remarks><ul>
/// <li>Font ids are written with a <c>Kind</c> property that names their type, so that any serializer reads them
/// back, whether it writes type names or not.</li>
/// <li>Font ids written with <c>$type</c> by a serializer that writes type names are read too. Old
/// <c>SingleFontSpec</c> objects are read with their face, size, line height and letter spacing. Their glyph ranges
/// and offset are dropped. Old font id type names are read as those of <see cref="Interface.Fonts"/>, by
/// <see cref="MovedTypeSerializationBinder.MapTypeName"/>.</li>
/// <li>A family made of a <see cref="FontRegistration"/> is not written, so that the style is read with the default
/// font.</li>
/// <li><see cref="TextStyle.ExtraDecoration"/> is written only for built-in decorations.</li>
/// <li>An unknown <see cref="TextStyle.Locale"/> is read as null. A font id, shadow, edge, line or decoration that
/// cannot be read is read as the default.</li>
/// </ul></remarks>
internal sealed class TextStyleJsonConverter : JsonConverter
{
    private const string KindProperty = "Kind";
    private const string TypeProperty = "$type";

    private static readonly FrozenDictionary<string, Type> KindTypes = new[]
    {
        typeof(ChainFamilyId), typeof(DalamudAssetFontId), typeof(DalamudDefaultFontId), typeof(GameFontId),
        typeof(SystemFontFaceId), typeof(SystemFontFamilyId),
    }.ToFrozenDictionary(static t => t.Name);

    // Writes the type of font ids where their declared type is an interface, and reads only font id types.
    private static readonly JsonSerializer FontIdSerializer = JsonSerializer.Create(
        new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            SerializationBinder = new FontIdBinder(),
        });

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

        var o = new JObject();
        if (s.FamilyId is { } family && !family.Registrations.Any())
        {
            using var tokenWriter = new JTokenWriter();

            // Pass typeof(IFontFamilyId) so TypeNameHandling.Auto emits $type for the concrete family type.
            FontIdSerializer.Serialize(tokenWriter, family, typeof(IFontFamilyId));
            var token = tokenWriter.Token!;
            RenameTypes(token);
            o[nameof(TextStyle.FamilyId)] = token;
        }

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
            o[nameof(s.Features)] = new JArray(s.Features.Select(static f => new JArray(f.Tag, f.Value)));
        o.WriteTo(writer);
    }

    /// <inheritdoc/>
    public override object? ReadJson(
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return objectType == typeof(TextStyle) ? default(TextStyle) : null;
        var o = JObject.Load(reader);
        Normalize(o);
        if (o.ContainsKey("FontId"))
            return ReadLegacy(o);

        var s = new TextStyle
        {
            FamilyId = ReadPart(
                o, nameof(TextStyle.FamilyId), static t => t.ToObject<IFontFamilyId>(FontIdSerializer)),
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
                .. o[nameof(TextStyle.Features)]?.Select(static f => new FontFeature((uint)f[0]!, (uint)f[1]!)) ?? [],
            ],
        };
        try
        {
            s.Locale = (string?)o[nameof(TextStyle.Locale)];
        }
        catch (CultureNotFoundException)
        {
        }

        s.Shadow = ReadPart(
            o,
            nameof(TextStyle.Shadow),
            static sh => new TextShadow(
                (uint)sh["Color"]!,
                new((float)sh["X"]!, (float)sh["Y"]!),
                Enum.Parse<StyleUnit>((string)sh["Unit"]!)));
        s.Edge = ReadPart(
            o,
            nameof(TextStyle.Edge),
            static e => new TextEdge((uint)e["Color"]!, StyleLength.Parse((string?)e["Width"]))
            {
                Softness = (float?)e["Softness"] ?? 0,
            });
        s.Lines = ReadPart(
            o,
            nameof(TextStyle.Lines),
            static l => new TextLineDecoration(
                (TextLinePosition)(int)l["Positions"]!,
                (TextLineStyle)(int)l["Style"]!,
                (uint?)l["Color"] ?? 0,
                (float?)l["Thickness"] ?? 0,
                (byte?)l["Count"] ?? 1));
        s.ExtraDecoration = ReadPart(
            o,
            nameof(TextStyle.ExtraDecoration),
            static d => new TextDecoration(new((uint)d["Id"]!), (uint?)d["Context"] ?? 0, (ulong?)d["Data"] ?? 0));
        return s;
    }

    private static T? ReadPart<T>(JObject o, string name, Func<JObject, T?> read)
    {
        if (o[name] is not JObject part)
            return default;
        try
        {
            return read(part);
        }
        catch (Exception e) when (e is JsonException or ArgumentException or FormatException or InvalidCastException
                                      or InvalidOperationException or OverflowException)
        {
            return default;
        }
    }

    private static void Add<T>(JObject o, string name, T value, T defaultValue = default!)
    {
        if (!Equals(value, defaultValue))
            o[name] = value is StyleLength length ? length.ToString() : JToken.FromObject(value!);
    }

    // Replace "$type" with "Kind" for known font ID types and strip any others.
    private static void RenameTypes(JToken token)
    {
        if (token is not JContainer container)
            return;
        foreach (var p in container.Descendants().OfType<JProperty>().Where(static p => p.Name == TypeProperty)
                                   .ToList())
        {
            if (p.Value is JValue { Type: JTokenType.String } v && KindTypes.ContainsKey((string)v!))
                p.Replace(new JProperty(KindProperty, p.Value));
            else
                p.Remove();
        }
    }

    // Makes font ids as written by any serializer readable by FontIdSerializer. Kinds and the type names of font ids
    // become types it reads, other type names are dropped, and collections written with their type become arrays.
    private static void Normalize(JToken token)
    {
        if (token is JObject { Parent: not null } o && o["$values"] is JArray values && o.Count <= 2)
        {
            var array = (JArray)values.DeepClone();
            token.Replace(array);
            token = array;
        }

        foreach (var child in token.Children().ToList())
            Normalize(child is JProperty p ? p.Value : child);

        if (token is not JObject obj)
            return;
        if (obj.Property(KindProperty) is { Value: JValue { Type: JTokenType.String } kind } kindProperty &&
            KindTypes.ContainsKey((string)kind!))
        {
            kindProperty.Replace(new JProperty(TypeProperty, kind));
        }
        else if (obj.Property(TypeProperty) is { } typeProperty)
        {
            if (FontIdKindOf((string?)typeProperty.Value) is { } kindName)
                typeProperty.Value = kindName;
            else
                typeProperty.Remove();
        }
    }

    // The kind of a font id type name written by a serializer, with or without the assembly, or null if it is not one.
    private static string? FontIdKindOf(string? typeName)
    {
        if (typeName is null)
            return null;
        var mapped = MovedTypeSerializationBinder.MapTypeName(typeName);
        var comma = mapped.IndexOf(',');
        var fullName = (comma < 0 ? mapped : mapped[..comma]).Trim();
        var dot = fullName.LastIndexOf('.');
        return KindTypes.TryGetValue(fullName[(dot + 1)..], out var type) && type.FullName == fullName
                   ? type.Name
                   : null;
    }

    private static TextStyle ReadLegacy(JObject o)
    {
        var sizePx = (float?)o["SizePx"] ?? 0;
        var k = sizePx > 0 ? 1 / sizePx : 1;
        var face = ReadPart(o, "FontId", static t => t.ToObject<IFontFaceId>(FontIdSerializer));
        if (face is DalamudAssetFontId a && (int?)o["FontNo"] is { } fontNo and not 0)
            face = new DalamudAssetFontId(a.Asset, fontNo);
        return new TextStyle
        {
            FamilyId = face?.Family,
            Size = sizePx > 0 ? StyleLength.Px(sizePx) : default,
            LineHeight = (float?)o["LineHeight"] is > 0 and not 1 and var lh ? StyleLength.Em(lh) : default,
            LetterSpacing = (float?)o["LetterSpacing"] is { } ls and not 0 ? StyleLength.Em(ls * k) : default,
        }.WithFace(face?.Style ?? default);
    }

    // Names font id types by their kind, and finds only font id types by kind.
    private sealed class FontIdBinder : ISerializationBinder
    {
        public Type BindToType(string? assemblyName, string typeName) =>
            KindTypes.TryGetValue(typeName, out var type)
                ? type
                : throw new JsonSerializationException($"\"{typeName}\" is not a kind of font id.");

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }
}

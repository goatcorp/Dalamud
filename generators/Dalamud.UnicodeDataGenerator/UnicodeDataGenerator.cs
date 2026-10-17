using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Dalamud.UnicodeDataGenerator;

/// <summary>Implements the partial properties marked <c>[GeneratedUnicodeData(fileName, defaultValue)]</c>.</summary>
/// <remarks>
/// A table is the index of each block of code points among the distinct blocks, and the distinct blocks' values one
/// after another. A block is <c>1 &lt;&lt; UnicodePropertyTable&lt;T&gt;.BlockBits</c> code points.
/// </remarks>
[Generator]
public sealed class UnicodeDataGenerator : IIncrementalGenerator
{
    private const string AttributeName = "GeneratedUnicodeDataAttribute";
    private const string AttributeShortName = "GeneratedUnicodeData";
    private const string NewLine = "\r\n";
    private const int CodePointCount = 0x110000;

    private static readonly DiagnosticDescriptor InvalidPropertyDescriptor = new(
        id: "UNIGEN001",
        title: "Invalid generated Unicode data property",
        messageFormat: "{0}: {1}",
        category: "UnicodeDataGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidDataDescriptor = new(
        id: "UNIGEN002",
        title: "Invalid Unicode data",
        messageFormat: "{0}({1}): {2}",
        category: "UnicodeDataGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var properties =
            context
                .SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is PropertyDeclarationSyntax p
                                        && p.AttributeLists
                                            .SelectMany(l => l.Attributes)
                                            .Any(a => GetSimpleName(a.Name) is AttributeName or AttributeShortName),
                    static (ctx, ct) => ReadProperty(ctx, ct))
                .Where(static p => p is not null)
                .Select(static (p, _) => p!)
                .Collect();

        var files =
            context
                .AdditionalTextsProvider
                .Select(static (f, ct) => new DataFile(f.Path, f.GetText(ct)?.ToString() ?? string.Empty));

        var tables =
            files
                .Combine(properties)
                .SelectMany(static (pair, ct) => pair.Right
                                                     .Where(p => string.Equals(
                                                                p.FileName,
                                                                Path.GetFileName(pair.Left.Path),
                                                                StringComparison.OrdinalIgnoreCase))
                                                     .Select(p => Generate(p, pair.Left, ct)));
        context.RegisterSourceOutput(tables, static (spc, table) => Report(spc, table));

        var invalid =
            properties
                .Combine(files.Select(static (f, _) => Path.GetFileName(f.Path)).Collect())
                .SelectMany(static (pair, _) =>
                                pair.Left.Select(p => (p.DisplayName,
                                                          Error: p.Error
                                                                 ?? (pair.Right.Contains(
                                                                         p.FileName,
                                                                         StringComparer.OrdinalIgnoreCase)
                                                                         ? null
                                                                         : $"'{p.FileName}' isn't one of the AdditionalFiles")))
                                    .Where(p => p.Error is not null));
        context.RegisterSourceOutput(
            invalid,
            static (spc, p) => spc.ReportDiagnostic(
                Diagnostic.Create(InvalidPropertyDescriptor, Location.None, p.DisplayName, p.Error)));
    }

    private static void Report(SourceProductionContext spc, Table table)
    {
        foreach (var d in table.Diagnostics)
            spc.ReportDiagnostic(Diagnostic.Create(InvalidDataDescriptor, Location.None, d.Path, d.Line, d.Message));
        if (table.Source is not null)
            spc.AddSource(table.HintName, SourceText.From(table.Source, Encoding.UTF8));
    }

    private static string GetSimpleName(NameSyntax name) => name switch
    {
        QualifiedNameSyntax q => q.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
        SimpleNameSyntax s => s.Identifier.ValueText,
        _ => string.Empty,
    };

    /// <summary>
    /// Reads what the generated code needs of a marked property; with <see cref="Property.Error"/> if it can't have it,
    /// or null if it isn't marked (an attribute of another name ending alike).
    /// </summary>
    private static Property? ReadProperty(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not IPropertySymbol symbol)
            return null;
        var attribute = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == AttributeName);
        if (attribute is null)
            return null;
        var displayName = symbol.ToDisplayString();
        var fileName = attribute.ConstructorArguments.Length > 0
                           ? attribute.ConstructorArguments[0].Value as string ?? string.Empty
                           : string.Empty;
        var defaultArgument = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1] : default;

        string? error = null;
        var table = symbol.Type as INamedTypeSymbol;
        var enumType = table is { IsGenericType: true, TypeArguments.Length: 1 }
                           ? table.TypeArguments[0] as INamedTypeSymbol
                           : null;
        var blockBits = table?.OriginalDefinition.GetMembers("BlockBits").OfType<IFieldSymbol>().FirstOrDefault()
                             ?.ConstantValue as int?;
        if (!symbol.IsStatic || !symbol.IsPartialDefinition())
            error = "the property must be a static partial property without a body";
        else if (table?.Name != "UnicodePropertyTable" || blockBits is null)
            error = "the property must be of a UnicodePropertyTable<T> that has a const int BlockBits";
        else if (enumType is not { TypeKind: TypeKind.Enum, EnumUnderlyingType.SpecialType: SpecialType.System_Byte })
            error = "the table's values must be of an enum of byte";

        var members = new StringBuilder();
        var isFlags = false;
        byte defaultValue = 0;
        if (enumType is not null && error is null)
        {
            isFlags = enumType.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.FlagsAttribute");
            foreach (var f in enumType.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue))
                members.Append(f.Name).Append('=').Append(Convert.ToByte(f.ConstantValue, CultureInfo.InvariantCulture))
                       .Append(',');

            defaultValue = Convert.ToByte(defaultArgument.Value, CultureInfo.InvariantCulture);
        }

        return new(
            displayName,
            symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            symbol.ContainingType.Name,
            symbol.Name,
            SyntaxFacts(symbol.DeclaredAccessibility),
            table?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty,
            fileName,
            enumType?.ToDisplayString() ?? string.Empty,
            members.ToString(),
            isFlags,
            defaultValue,
            blockBits ?? 0,
            error);
    }

    private static string SyntaxFacts(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "private",
    };

    private static Table Generate(Property property, DataFile file, CancellationToken ct)
    {
        var hintName = $"{property.ContainingType}.{property.Name}.g.cs";
        var diagnostics = new List<FileDiagnostic>();
        if (property.Error is not null)
            return new(hintName, null, diagnostics);

        var members = property.Members.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(m => m.Split('='))
                              .ToDictionary(
                                  m => m[0],
                                  m => byte.Parse(m[1], CultureInfo.InvariantCulture),
                                  StringComparer.Ordinal);

        var values = new byte[CodePointCount];
        for (var i = 0; i < values.Length; i++)
            values[i] = property.DefaultValue;

        // Lines of "range ; value", or "range ; property ; value", each with an optional comment after "#".
        var lines = file.Text.Split('\n');
        for (var n = 0; n < lines.Length; n++)
        {
            ct.ThrowIfCancellationRequested();
            var line = lines[n];
            var hash = line.IndexOf('#');
            if (hash >= 0)
                line = line.Substring(0, hash);
            line = line.Trim();
            if (line.Length == 0)
                continue;

            var first = line.IndexOf(';');
            if (first < 0)
            {
                diagnostics.Add(new(file.Path, n + 1, "Expected a range and a value separated by ';'"));
                continue;
            }

            var range = line.Substring(0, first).Trim();
            var name = line.Substring(line.LastIndexOf(';') + 1).Trim();
            if (!members.TryGetValue(name, out var value))
            {
                diagnostics.Add(new(file.Path, n + 1, $"'{name}' isn't a member of {property.EnumName}"));
                continue;
            }

            var dots = range.IndexOf("..", StringComparison.Ordinal);
            if (!TryParseCodePoint(dots < 0 ? range : range.Substring(0, dots), out var from) ||
                !TryParseCodePoint(dots < 0 ? range : range.Substring(dots + 2), out var to) ||
                to < from)
            {
                diagnostics.Add(new(file.Path, n + 1, $"'{range}' isn't a range of code points"));
                continue;
            }

            for (var c = from; c <= to; c++)
                values[c] = property.IsFlags ? (byte)(values[c] | value) : value;
        }

        // Each block of code points is stored once.
        var blockSize = 1 << property.BlockBits;
        var blocks = new ushort[CodePointCount / blockSize];
        var distinct = new Dictionary<string, ushort>(StringComparer.Ordinal);
        var stored = new List<byte>();
        for (var b = 0; b < blocks.Length; b++)
        {
            var key = Convert.ToBase64String(values, b * blockSize, blockSize);
            if (!distinct.TryGetValue(key, out var index))
            {
                index = (ushort)distinct.Count;
                distinct.Add(key, index);
                for (var i = 0; i < blockSize; i++)
                    stored.Add(values[(b * blockSize) + i]);
            }

            blocks[b] = index;
        }

        var sb = new StringBuilder();
        sb.Append("// <auto-generated/>").Append(NewLine).Append(NewLine);
        if (property.Namespace is not null)
            sb.Append("namespace ").Append(property.Namespace).Append(';').Append(NewLine).Append(NewLine);
        sb.Append("partial class ").Append(property.ContainingType).Append(NewLine)
          .Append('{').Append(NewLine)
          .Append("    // From ").Append(Path.GetFileName(file.Path)).Append(": ").Append(distinct.Count)
          .Append(" distinct blocks of ").Append(blockSize).Append(" code points.").Append(NewLine)
          .Append("    ").Append(property.Accessibility).Append(" static partial ").Append(property.TableType)
          .Append(' ')
          .Append(property.Name).Append(" => new(").Append(property.Name).Append("Blocks, ").Append(property.Name)
          .Append("Values);").Append(NewLine).Append(NewLine)
          .Append("    private static global::System.ReadOnlySpan<ushort> ").Append(property.Name)
          .Append("Blocks => [");
        AppendValues(sb, blocks.Select(b => (int)b));
        sb.Append("];").Append(NewLine).Append(NewLine)
          .Append("    private static global::System.ReadOnlySpan<byte> ").Append(property.Name).Append("Values => [");
        AppendValues(sb, stored.Select(v => (int)v));
        sb.Append("];").Append(NewLine).Append('}').Append(NewLine);
        return new(hintName, sb.ToString(), diagnostics);
    }

    private static bool TryParseCodePoint(string s, out int value) =>
        int.TryParse(s.Trim(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value) &&
        value is >= 0 and < CodePointCount;

    private static void AppendValues(StringBuilder sb, IEnumerable<int> values)
    {
        var i = 0;
        foreach (var v in values)
        {
            sb.Append(i % 32 == 0 ? NewLine + "        " : " ").Append(v).Append(',');
            i++;
        }

        sb.Append(NewLine).Append("    ");
    }

    /// <summary>A marked property: where it goes, what it is of, and its file and default value.</summary>
    private sealed record Property(
        string DisplayName,
        string? Namespace,
        string ContainingType,
        string Name,
        string Accessibility,
        string TableType,
        string FileName,
        string EnumName,
        string Members,
        bool IsFlags,
        byte DefaultValue,
        int BlockBits,
        string? Error);

    private sealed record DataFile(string Path, string Text);

    private sealed record FileDiagnostic(string Path, int Line, string Message);

    private sealed record Table(string HintName, string? Source, IReadOnlyList<FileDiagnostic> Diagnostics)
    {
        public bool Equals(Table? other) => other is not null
                                            && this.HintName == other.HintName
                                            && this.Source == other.Source
                                            && this.Diagnostics.SequenceEqual(other.Diagnostics);

        public override int GetHashCode() => this.HintName.GetHashCode() ^ (this.Source?.GetHashCode() ?? 0);
    }
}

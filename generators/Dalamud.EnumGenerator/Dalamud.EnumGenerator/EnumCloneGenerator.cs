using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Dalamud.EnumGenerator;

[Generator]
public class EnumCloneGenerator : IIncrementalGenerator
{
    private const string NewLine = "\r\n";

    private const string MappingFileName = "EnumCloneMap.txt";

    private static readonly DiagnosticDescriptor MissingSourceDescriptor = new(
        id: "ENUMGEN001",
        title: "Source enum not found",
        messageFormat: "Source enum '{0}' could not be resolved by the compilation",
        category: "EnumGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateTargetDescriptor = new(
        id: "ENUMGEN002",
        title: "Duplicate target mapping",
        messageFormat: "Target enum '{0}' is mapped multiple times; generation skipped for this target",
        category: "EnumGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Read mappings from additional files named EnumCloneMap.txt
        var mappingEntries = context.AdditionalTextsProvider
            .Where(at => Path.GetFileName(at.Path).Equals(MappingFileName, StringComparison.OrdinalIgnoreCase))
            .SelectMany((at, _) => ParseMappings(at.GetText()?.ToString() ?? string.Empty));

        // Combine with compilation so we can resolve types
        var compilationAndMaps = context.CompilationProvider.Combine(mappingEntries.Collect());

        context.RegisterSourceOutput(compilationAndMaps, (spc, pair) =>
        {
            var compilation = pair.Left;
            var maps = pair.Right;

            // Detect duplicate targets first and report diagnostics
            var duplicateTargets = maps.GroupBy(m => m.TargetFullName, StringComparer.OrdinalIgnoreCase)
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key)
                                       .ToImmutableArray();
            foreach (var dup in duplicateTargets)
            {
                var diag = Diagnostic.Create(DuplicateTargetDescriptor, Location.None, dup);
                spc.ReportDiagnostic(diag);
            }

            foreach (var (targetFullName, sourceFullName) in maps)
            {
                if (string.IsNullOrWhiteSpace(targetFullName) || string.IsNullOrWhiteSpace(sourceFullName))
                    continue;

                if (duplicateTargets.Contains(targetFullName, StringComparer.OrdinalIgnoreCase))
                    continue;

                // Resolve the source enum type by metadata name (namespace.type)
                var sourceSymbol = compilation.GetTypeByMetadataName(sourceFullName);
                if (sourceSymbol is null)
                {
                    // Report diagnostic for missing source type
                    var diag = Diagnostic.Create(MissingSourceDescriptor, Location.None, sourceFullName);
                    spc.ReportDiagnostic(diag);
                    continue;
                }

                if (sourceSymbol.TypeKind != TypeKind.Enum)
                    continue;

                var sourceNamed = sourceSymbol; // GetTypeByMetadataName already returns INamedTypeSymbol

                // Split target into namespace and type name
                string? targetNamespace = null;
                var targetName = targetFullName;
                var lastDot = targetFullName.LastIndexOf('.');
                if (lastDot >= 0)
                {
                    targetNamespace = targetFullName.Substring(0, lastDot);
                    targetName = targetFullName.Substring(lastDot + 1);
                }

                var underlyingType = sourceNamed.EnumUnderlyingType;
                var underlyingDisplay = underlyingType?.ToDisplayString() ?? "int";

                var fields = sourceNamed.GetMembers()
                                        .OfType<IFieldSymbol>()
                                        .Where(f => f.IsStatic && f.HasConstantValue)
                                        .ToArray();

                var memberLines = fields.Select(f =>
                {
                    var name = f.Name;
                    var constValue = f.ConstantValue;
                    string literal;

                    var st = underlyingType?.SpecialType ?? SpecialType.System_Int32;

                    if (constValue is null)
                    {
                        literal = "0";
                    }
                    else if (st == SpecialType.System_UInt64)
                    {
                        literal = Convert.ToString(constValue, CultureInfo.InvariantCulture) + "UL";
                    }
                    else if (st == SpecialType.System_UInt32)
                    {
                        literal = Convert.ToString(constValue, CultureInfo.InvariantCulture) + "U";
                    }
                    else if (st == SpecialType.System_Int64)
                    {
                        literal = Convert.ToString(constValue, CultureInfo.InvariantCulture) + "L";
                    }
                    else
                    {
                        literal = Convert.ToString(constValue, CultureInfo.InvariantCulture) ?? throw new InvalidOperationException("Unable to convert enum constant value to string.");
                    }

                    return $"    {name} = {literal},";
                });

                var membersText = string.Join(NewLine, memberLines);

                var nsPrefix = targetNamespace is null ? string.Empty : $"namespace {targetNamespace};" + NewLine + NewLine;

                var sourceFullyQualified = sourceNamed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var code = "// <auto-generated/>" + NewLine + NewLine
                           + nsPrefix
                           + $"public enum {targetName} : {underlyingDisplay}" + NewLine
                           + "{" + NewLine
                           + membersText + NewLine
                           + "}" + NewLine + NewLine;

                var extClassName = targetName + "Conversions";
                var extMethodName = "ToDalamud" + targetName;

                var extClass = $"public static class {extClassName}" + NewLine
                             + "{" + NewLine
                             + $"    public static {targetName} {extMethodName}(this {sourceFullyQualified} value) => ({targetName})(({underlyingDisplay})value);" + NewLine
                             + "}" + NewLine;

                code += extClass;

                var hintName = $"{targetName}.CloneEnum.g.cs";
                spc.AddSource(hintName, SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    internal static ImmutableArray<(string TargetFullName, string SourceFullName)> ParseMappings(string text)
    {
        var builder = ImmutableArray.CreateBuilder<(string, string)>();
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            // Remove comments starting with #
            var commentIndex = line.IndexOf('#');
            var content = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
            content = content.Trim();
            if (string.IsNullOrEmpty(content))
                continue;

            // Expected format: Target.Full.Name = Source.Full.Name
            var idx = content.IndexOf('=');
            if (idx <= 0)
                continue;

            var left = content.Substring(0, idx).Trim();
            var right = content.Substring(idx + 1).Trim();
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                continue;

            builder.Add((left, right));
        }

        return builder.ToImmutable();
    }
}

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dalamud.UnicodeDataGenerator;

/// <summary>Helpers for the symbols of Roslyn versions the generator is compiled against.</summary>
internal static class SymbolExtensions
{
    /// <summary>Gets whether a property is the defining declaration of a partial property (without a body).</summary>
    public static bool IsPartialDefinition(this IPropertySymbol property) =>
        property.DeclaringSyntaxReferences
                .Any(r => r.GetSyntax() is PropertyDeclarationSyntax p
                          && p.Modifiers.Any(m => m.ValueText == "partial")
                          && p.ExpressionBody is null
                          && p.AccessorList?.Accessors.All(a => a.Body is null && a.ExpressionBody is null) == true);
}

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Dalamud.EnumGenerator.Tests;

public class EnumCloneMapTests
{
    [Fact]
    public void ParseMappings_SimpleLines_ParsesCorrectly()
    {
        var text = @"# Comment line
My.Namespace.Target = Other.Namespace.Source

Another.Target = Some.Source";

        var results = Dalamud.EnumGenerator.EnumCloneGenerator.ParseMappings(text);

        Assert.Equal(2, results.Length);
        Assert.Equal("My.Namespace.Target", results[0].TargetFullName);
        Assert.Equal("Other.Namespace.Source", results[0].SourceFullName);
        Assert.Equal("Another.Target", results[1].TargetFullName);
    }

    [Fact]
    public void Generator_ProducesFile_WhenSourceResolved()
    {
        // We'll create a compilation that contains a source enum type and add an AdditionalText mapping
        var sourceEnum = @"namespace Foo.Bar { public enum SourceEnum { A = 1, B = 2 } }";

        var mapText = "GeneratedNs.TargetEnum = Foo.Bar.SourceEnum";

        var generator = new EnumCloneGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(new Utils.TestAdditionalFile("EnumCloneMap.txt", mapText)));

        var compilation = CSharpCompilation.Create("TestGen", [CSharpSyntaxTree.ParseText(sourceEnum)],
                                                   [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        var generated = newCompilation.SyntaxTrees.Select(t => t.FilePath).Where(p => p.EndsWith("TargetEnum.CloneEnum.g.cs")).ToArray();
        Assert.Single(generated);
    }
}

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

        var results = EnumCloneGenerator.ParseMappings(text);

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
            .AddAdditionalTexts([new Utils.TestAdditionalFile("EnumCloneMap.txt", mapText)]);

        var compilation = CSharpCompilation.Create("TestGen",
            [CSharpSyntaxTree.ParseText(sourceEnum, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics, TestContext.Current.CancellationToken);

        var generated = newCompilation.SyntaxTrees.Select(t => t.FilePath).Where(p => p.EndsWith("TargetEnum.CloneEnum.g.cs")).ToArray();
        Assert.Single(generated);
    }

    [Fact]
    public void Generator_TestNestedEnum()
    {
        var sourceEnum = @"namespace Test; public static class Nest { public enum Source { A = 1, B = 2} }";
        var mapText = "GeneratedNs.Target = Test.Nest.Source\nGeneratedNs.Target2 = Test.Nest+Source";
        var generator = new EnumCloneGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
                                          .AddAdditionalTexts([new Utils.TestAdditionalFile("EnumCloneMap.txt", mapText)]);

        var compilation = CSharpCompilation.Create("TestGen",
                                                   [CSharpSyntaxTree.ParseText(sourceEnum, cancellationToken: TestContext.Current.CancellationToken)],
                                                   [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics, TestContext.Current.CancellationToken);

        var generated = newCompilation.SyntaxTrees.Select(t => t.FilePath).Count(p => p.EndsWith("Target.CloneEnum.g.cs") || p.EndsWith("Target2.CloneEnum.g.cs"));
        Assert.Equal(2, generated);
    }

    [Fact]
    public void Generator_ProducesFlags_WhenSourceHasFlagsAttribute()
    {
        var sourceEnum = @"
using System;
namespace Foo.Bar {
    [Flags]
    public enum FlagsSource { None = 0, A = 1, B = 2, AB = 3 }
}";

        var mapText = "GeneratedNs.TargetFlags = Foo.Bar.FlagsSource";

        var generator = new EnumCloneGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([new Utils.TestAdditionalFile("EnumCloneMap.txt", mapText)]);

        var compilation = CSharpCompilation.Create("TestGenFlags",
            [CSharpSyntaxTree.ParseText(sourceEnum, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _, TestContext.Current.CancellationToken);

        var generatedTree = newCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("TargetFlags.CloneEnum.g.cs"));
        Assert.NotNull(generatedTree);

        var generatedText = generatedTree.ToString();
        Assert.Contains("[Flags]", generatedText);
    }

    [Fact]
    public void Generator_DoesNotProduceFlags_WhenSourceHasNoFlagsAttribute()
    {
        var sourceEnum = "namespace Foo.Bar { public enum PlainSource { A = 1, B = 2 } }";

        var mapText = "GeneratedNs.TargetPlain = Foo.Bar.PlainSource";

        var generator = new EnumCloneGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([new Utils.TestAdditionalFile("EnumCloneMap.txt", mapText)]);

        var compilation = CSharpCompilation.Create("TestGenPlain",
            [CSharpSyntaxTree.ParseText(sourceEnum, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _, TestContext.Current.CancellationToken);

        var generatedTree = newCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("TargetPlain.CloneEnum.g.cs"));
        Assert.NotNull(generatedTree);

        var generatedText = generatedTree.ToString();
        Assert.DoesNotContain("[Flags]", generatedText);
    }
}

using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Dalamud.EnumGenerator.Tests.Utils;

public class TestAdditionalFile : AdditionalText
{
    private readonly SourceText text;

    public TestAdditionalFile(string path, string text)
    {
        Path = path;
        this.text = SourceText.From(text);
    }

    public override SourceText GetText(CancellationToken cancellationToken = new()) => this.text;

    public override string Path { get; }
}

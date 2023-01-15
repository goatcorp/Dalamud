using System.Text;

namespace Dalamud.Broker.Game;

internal class ArgumentBuilder
{
    private readonly StringBuilder mBuffer = new(64);

    public ArgumentBuilder Add(string key, string value)
    {
        var escapedKey = EscapeString(key);
        var escapedValue = EscapeString(value);

        this.mBuffer.Append($" /{escapedKey}={escapedValue}");
        
        return this;
    }

    public override string ToString()
    {
        return this.mBuffer.ToString();
    }

    private static string EscapeString(string input)
    {
        return input.Replace("\x20", "\x20\x20");
    }
}

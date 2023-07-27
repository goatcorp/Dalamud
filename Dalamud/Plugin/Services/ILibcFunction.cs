using System.Text;

using Dalamud.Game.Libc;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class handles creating cstrings utilizing native game methods.
/// </summary>
public interface ILibcFunction
{
    /// <summary>
    /// Create a new string from the given bytes.
    /// </summary>
    /// <param name="content">The bytes to convert.</param>
    /// <returns>An owned std string object.</returns>
    public OwnedStdString NewString(byte[] content);

    /// <summary>
    /// Create a new string form the given bytes.
    /// </summary>
    /// <param name="content">The bytes to convert.</param>
    /// <param name="encoding">A non-default encoding.</param>
    /// <returns>An owned std string object.</returns>
    public OwnedStdString NewString(string content, Encoding? encoding = null);
}

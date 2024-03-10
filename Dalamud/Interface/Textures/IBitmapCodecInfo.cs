using System.Collections.Generic;

namespace Dalamud.Interface.Textures;

/// <summary>Represents an available bitmap codec.</summary>
public interface IBitmapCodecInfo
{
    /// <summary>Gets the friendly name for the codec.</summary>
    string Name { get; }

    /// <summary>Gets the <see cref="Guid"/> representing the container.</summary>
    Guid ContainerGuid { get; }

    /// <summary>Gets the suggested file extensions.</summary>
    IReadOnlyCollection<string> Extensions { get; }

    /// <summary>Gets the corresponding mime types.</summary>
    IReadOnlyCollection<string> MimeTypes { get; }
}

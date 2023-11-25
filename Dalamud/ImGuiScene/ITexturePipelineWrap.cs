namespace Dalamud.ImGuiScene;

/// <summary>
/// Represents a handle to an immutable texture pipeline.
/// </summary>
public interface ITexturePipelineWrap : ICloneable, IDisposable
{
    /// <summary>
    /// Gets a value indicating whether this instance of <see cref="ITexturePipelineWrap"/> has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <inheritdoc cref="ICloneable.Clone"/>
    new ITexturePipelineWrap Clone();
    
    /// <inheritdoc cref="ICloneable.Clone"/>
    object ICloneable.Clone() => this.Clone();
}

namespace Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;

/// <summary>Extension for <see cref="IRectScaleMode"/> for scale modes supporting variable default zoom.</summary>
public interface IRectScaleModeWithZoomInToFit : IRectScaleMode
{
    /// <summary>Gets a value indicating whether to zoom in to fit.</summary>
    public bool ZoomInToFit { get; }
}

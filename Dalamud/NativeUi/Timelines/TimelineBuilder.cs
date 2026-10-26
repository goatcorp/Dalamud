using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// The main class used for building a custom timeline/timeline animation.
/// </summary>
internal class TimelineBuilder
{
    /// <summary>
    /// Gets list of all animations being built.
    /// </summary>
    internal List<TimelineAnimation> Animations { get; private set; } = [];

    /// <summary>
    /// Gets list of all label sets being built.
    /// </summary>
    internal List<TimelineLabelSet> LabelSets { get; private set; } = [];

    /// <summary>
    /// The main function to start building a timeline.
    /// Give this function the full range of frames you want to build timelines for.
    /// </summary>
    /// <remarks>
    /// Every 60 keyframes represents a 1second animation regardless of the games actual framerate.
    /// </remarks>
    /// <param name="startFrameId">The starting frame index.</param>
    /// <param name="endFrameId">The ending frame index.</param>
    /// <returns>Frameset builder.</returns>
    public FrameSetBuilder BeginFrameSet(int startFrameId, int endFrameId)
        => new(this, startFrameId, endFrameId);

    /// <summary>
    /// Directly adds a full frameset with specified animation.
    /// </summary>
    /// <returns>Frameset builder.</returns>
    /// <param name="startFrameId">Start frame id.</param>
    /// <param name="endFrameId">End frame id.</param>
    /// <param name="frameId">Frame id.</param>
    /// <param name="position">Position.</param>
    /// <param name="alpha">Alpha.</param>
    /// <param name="addColor">Add Color.</param>
    /// <param name="multiplyColor">Multiply Color.</param>
    /// <param name="rotation">Rotation in radians.</param>
    /// <param name="scale">Scale.</param>
    /// <param name="textColor">Text color.</param>
    /// <param name="textOutlineColor">Text Outline color.</param>
    /// <param name="partId">Part Id.</param>
    public TimelineBuilder AddFrameSetWithFrame(
        int startFrameId, int endFrameId, int frameId, Vector2? position = null, byte? alpha = null, Vector3? addColor = null, Vector3? multiplyColor = null,
        float? rotation = null, Vector2? scale = null, Vector3? textColor = null, Vector3? textOutlineColor = null, uint? partId = null)
    {
        new FrameSetBuilder(this, startFrameId, endFrameId)
            .AddFrame(frameId, position, alpha, addColor, multiplyColor, rotation, scale, textColor, textOutlineColor, partId)
            .EndFrameSet();

        return this;
    }

    /// <summary>
    /// Begins a frameset builder for the specified ranges, but targeting a specific frame index for building the animation.
    /// </summary>
    /// <param name="frameSetStart">Start frame.</param>
    /// <param name="frameSetEnd">End frame.</param>
    /// <param name="frameIndex">Target frame.</param>
    /// <returns>Frameset builder.</returns>
    public KeyFrameBuilder AddFrame(int frameSetStart, int frameSetEnd, int frameIndex)
        => new(new FrameSetBuilder(this, frameSetStart, frameSetEnd), frameIndex);

    /// <summary>
    /// Constructs the actual native object that will be set in the games memory to do the animations.
    /// </summary>
    /// <returns>A complete, fully built timeline object.</returns>
    public Timeline Build()
    {
        var newTimeline = new Timeline();

        if (this.LabelSets.Count != 0)
        {
            newTimeline.LabelSets = this.LabelSets;
            newTimeline.LabelFrameIdxDuration = this.LabelSets.Max(label => label.EndFrameId) - 1;
            newTimeline.LabelEndFrameIdx = this.LabelSets.Max(label => label.EndFrameId);
        }

        if (this.Animations.Count != 0)
        {
            newTimeline.Animations = this.Animations;
        }

        return newTimeline;
    }
}

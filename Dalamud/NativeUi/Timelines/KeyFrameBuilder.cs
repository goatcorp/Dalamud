using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Represents a builder for keyframes by use from <see cref="FrameSetBuilder"/>
/// </summary>
internal class KeyFrameBuilder(FrameSetBuilder parent, int frame)
{
    private readonly List<TimelineKeyFrame> animationKeyFrames = [];

    /// <summary>
    /// Adds a position adjustment animation to this keyframe.
    /// </summary>
    /// <param name="position">Node position.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder Position(Vector2 position)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, Position = position,
        });

        return this;
    }

    /// <summary>
    /// Adds an alpha adjustment animation to this keyframe.
    /// </summary>
    /// <remarks>
    /// Values in range of 0 to 255, with 0 being invisible.
    /// </remarks>
    /// <param name="alpha">Node alpha.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder Alpha(byte alpha)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, Alpha = alpha,
        });

        return this;
    }

    /// <summary>
    /// Adds an AddColor adjustment animation to this keyframe.
    /// </summary>
    /// <remarks>
    /// Values in range of 0.0f to 255.0f.
    /// </remarks>
    /// <param name="color">Node color.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder AddColor(Vector3 color)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, AddColor = color,
        });

        return this;
    }

    /// <summary>
    /// Adds an MultiplyColor adjustment animation to this keyframe.
    /// </summary>
    /// <remarks>
    /// Values in range of 0.0f to 100.0f.
    /// </remarks>
    /// <param name="color">Node multiply color.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder MultiplyColor(Vector3 color)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, MultiplyColor = color,
        });

        return this;
    }

    /// <summary>
    /// Adds an MultiplyColor adjustment animation to this keyframe.
    /// Applies the same value to R, G, and B.
    /// </summary>
    /// <remarks>
    /// Values in range of 0.0f to 100.0f.
    /// </remarks>
    /// <param name="color">Multiply color for R G and B.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder MultiplyColor(float color)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, MultiplyColor = new Vector3(color, color, color),
        });

        return this;
    }

    /// <summary>
    /// Adds a rotation adjustment animation to this keyframe.
    /// </summary>
    /// <remarks>
    /// Value is in degrees and can be greater than 360.0f or less than 0.0f.
    /// </remarks>
    /// <param name="degrees">Node rotation.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder RotationDegrees(float degrees)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, Rotation = (degrees * MathF.PI) / 180.0f,
        });

        return this;
    }

    /// <summary>
    /// Adds a rotation adjustment animation to this keyframe.
    /// </summary>
    /// <remarks>
    /// Value is in radians and can be greater than 2pi or less than 0.
    /// </remarks>
    /// <param name="rotation">Node rotation in radians.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder Rotation(float rotation)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, Rotation = rotation,
        });

        return this;
    }

    /// <summary>
    /// Adds a scale adjustment animation to this keyframe.
    /// </summary>
    /// <param name="scale">Node scale.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder Scale(Vector2 scale)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, Scale = scale,
        });

        return this;
    }

    /// <summary>
    /// Adds a scale adjustment animation to this keyframe.
    /// Applies the scale in both X and Y directions.
    /// </summary>
    /// <param name="scale">Node scale for X and Y.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder Scale(float scale)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, Scale = new Vector2(scale, scale),
        });

        return this;
    }

    /// <summary>
    /// Adds a TextColor adjustment animation to this keyframe.
    /// </summary>
    /// <param name="textColor">Nodes TextColor.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder TextColor(Vector3 textColor)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, TextColor = textColor,
        });

        return this;
    }

    /// <summary>
    /// Adds a TextOutlineColor adjustment animation to this keyframe.
    /// </summary>
    /// <param name="textColor">Nodes TextOutline Color.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder TextOutlineColor(Vector3 textColor)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, TextEdgeColor = textColor,
        });

        return this;
    }

    /// <summary>
    /// Adds a PartId adjustment animation to this keyframe.
    /// </summary>
    /// <example>
    /// For the ants animation the game will loop through parts 0 -> 16 using a keyframe set to 0 and another keyframe set to 16.
    /// </example>
    /// <param name="partId">Part id.</param>
    /// <returns>Parent frameset builder.</returns>
    public KeyFrameBuilder Part(uint partId)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frame, PartId = partId,
        });

        return this;
    }

    /// <summary>
    /// Completes building frames and returns parent builder.
    /// </summary>
    /// <returns>Parent frameset builder.</returns>
    public FrameSetBuilder EndFrameBuilder()
    {
        parent.AddFrame(this.animationKeyFrames.ToArray());
        return parent;
    }
}

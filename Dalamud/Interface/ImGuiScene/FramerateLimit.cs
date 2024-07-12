namespace ImGuiScene
{
    /// <summary>
    /// Simple encapsulation of framerate limiting behavior, allowing for fully unbounded (no control),
    /// Vsync-enabled (sync to monitor refresh), or a specified fixed framerate (vsync disabled, hard time cap)
    /// </summary>
    public class FramerateLimit
    {
        /// <summary>
        /// The different methods of limiting framerate.
        /// </summary>
        public enum LimitType
        {
            /// <summary>
            /// No limiting at all.
            /// </summary>
            Unbounded,
            /// <summary>
            /// Vsync enabled.  Render presentation will be synced to display refresh rate.
            /// </summary>
            Vsync,
            /// <summary>
            /// Restrict rendering to a fixed (maximum) number of frames per second.
            /// This will disable vsync regardless of the fps value.
            /// </summary>
            FixedFPS
        }

        /// <summary>
        /// Which type of framerate limiting to apply.
        /// </summary>
        public LimitType Type { get; }

        private readonly int _fps;
        /// <summary>
        /// The current FPS limit.  Only valid with <see cref="LimitType.FixedFPS"/>.
        /// </summary>
        public int FPS
        {
            get
            {
                if (Type != LimitType.FixedFPS)
                    throw new InvalidOperationException();

                return _fps;
            }
        }

        /// <summary>
        /// Creates a new framerate limit description.
        /// </summary>
        /// <param name="limitType">Which type of limiting to apply.</param>
        /// <param name="targetFps">Used only with <see cref="LimitType.FixedFPS"/>, the target frames per second to restrict rendering to.</param>
        public FramerateLimit(LimitType limitType, int targetFps = 0)
        {
            if (limitType == LimitType.FixedFPS && targetFps <= 0)
            {
                limitType = LimitType.Unbounded;
            }

            Type = limitType;
            _fps = targetFps;
        }

        public override string ToString()
        {
            var str = Type.ToString();
            if (Type == LimitType.FixedFPS)
            {
                str += $" ({FPS})";
            }
            return str;
        }
    }
}

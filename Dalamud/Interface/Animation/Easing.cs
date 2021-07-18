using System;
using System.Diagnostics;
using System.Numerics;

namespace Dalamud.Interface.Animation
{
    public abstract class Easing
    {
        private readonly Stopwatch animationTimer = new();

        private double valueInternal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Easing"/> class with the specified duration.
        /// </summary>
        /// <param name="duration">The animation duration.</param>
        protected Easing(TimeSpan duration)
        {
            this.Duration = duration;
        }

        /// <summary>
        /// Gets or sets the origin point of the animation.
        /// </summary>
        public Vector2? Point1 { get; set; }

        /// <summary>
        /// Gets or sets the destination point of the animation.
        /// </summary>
        public Vector2? Point2 { get; set; }

        /// <summary>
        /// Gets the resulting point at the current timestep.
        /// </summary>
        public Vector2 EasedPoint { get; private set; }

        /// <summary>
        /// Gets the current value of the animation, from 0 to 1.
        /// </summary>
        public double Value
        {
            get => this.valueInternal;
            set
            {
                this.valueInternal = Math.Min(value, 1);

                if (Point1.HasValue && Point2.HasValue)
                    EasedPoint = Lerp(Point1.Value, Point2.Value, (float)this.valueInternal);
            }
        }

        /// <summary>
        /// Gets or sets the duration of the animation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets the progress of the animation, from 0 to 1.
        /// </summary>
        protected double Progress => this.animationTimer.ElapsedMilliseconds / this.Duration.TotalMilliseconds;

        /// <summary>
        /// Starts the animation from where it was last stopped, or from the start if it was never started before.
        /// </summary>
        public void Start()
        {
            this.animationTimer.Start();
        }

        /// <summary>
        /// Stops the animation at the current point.
        /// </summary>
        public void Stop()
        {
            this.animationTimer.Stop();
        }

        /// <summary>
        /// Restarts the animation.
        /// </summary>
        public void Restart()
        {
            this.animationTimer.Restart();
        }

        /// <summary>
        /// Updates the animation.
        /// </summary>
        public abstract void Update();

        private static float Lerp(float firstFloat, float secondFloat, float by)
        {
            return (firstFloat * (1 - @by)) + (secondFloat * by);
        }

        private static Vector2 Lerp(Vector2 firstVector, Vector2 secondVector, float by)
        {
            var retX = Lerp(firstVector.X, secondVector.X, by);
            var retY = Lerp(firstVector.Y, secondVector.Y, by);
            return new Vector2(retX, retY);
        }
    }
}

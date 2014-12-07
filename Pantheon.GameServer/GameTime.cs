using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pantheon.GameServer
{
    public struct GameTime
    {
        public static double GameTargetTime = 0.05;
        private DateTime _time;

        /// <summary>
        ///   Gets a <see cref="TimeSpan" /> representing the amount of time passed since this
        ///   <see cref="GameTime" /> was started.
        /// </summary>
        public TimeSpan Elapsed => DateTime.Now - _time;

        /// <summary>
        ///   Gets the number of milliseconds elapsed since this <see cref="GameTime" /> was started.
        /// </summary>
        /// <value>The value of this property may be fractional (e.g. 0.7).</value>
        public double ElapsedMilliseconds => Elapsed.TotalMilliseconds;

        /// <summary>
        ///   Gets the number of milliseconds elapsed since this <see cref="GameTime" /> was started
        ///   and casts the result to a <see cref="float" /> .
        /// </summary>
        /// <value>The value of this property may be fractional (e.g. 0.7).</value>
        public float ElapsedMillisecondsf => (float)ElapsedMilliseconds;

        /// <summary>
        ///   Gets the number of seconds elapsed since this <see cref="GameTime" /> was started.
        /// </summary>
        /// <value>The value of this property may be fractional (e.g. 0.7).</value>
        public double ElapsedSeconds => Elapsed.TotalSeconds;

        /// <summary>
        ///   Gets the number of seconds elapsed since this <see cref="GameTime" /> was started and
        ///   casts the result to a <see cref="float" /> .
        /// </summary>
        /// <value>The value of this property may be fractional (e.g. 0.7).</value>
        public float ElapsedSecondsf => (float)ElapsedSeconds;

        /// <summary>
        ///   Gets a value indicating whether the game is running slower than the
        ///   <see cref="GameTargetTime" /> .
        /// </summary>
        public bool IsRunningSlow => ElapsedSeconds > (GameTargetTime + GameTargetTime / 4);

        /// <summary>
        ///   Initializes a new instance of the <see cref="GameTime" /> <c>struct</c> using the
        ///   provided <see cref="DateTime" /> as the start of the current tick.
        /// </summary>
        /// <param name="start">
        ///   A <see cref="DateTime" /> value that indicates the start of the current tick.
        /// </param>
        public GameTime(DateTime start)
        {
            _time = start;
        }

        /// <summary>
        ///   Multiplies <paramref name="value" /> by the current value of
        ///   <see cref="ElapsedSeconds" /> .
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <returns>
        ///   The value of <paramref name="value" /> * <see cref="ElapsedSeconds" /> .
        /// </returns>
        public double ApplySeconds(double value) => value * ElapsedSeconds;

        public override string ToString() => ElapsedSeconds.ToString("F");
    }
}
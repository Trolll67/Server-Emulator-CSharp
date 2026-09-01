using Packets.Server.Game.Structures;
using System;

namespace Server.Game.Core.Systems
{
    /// <summary>
    ///     One step of a monster walk: where the monster stands after the step and whether the walk
    ///     is over. The position is always a new vector - the caller assigns it to the monster, and
    ///     a shared object would tie the monster to the target it walks to
    /// </summary>
    public readonly struct MonsterMoveStep
    {
        public MonsterMoveStep(Vector3 position, bool isArrived)
        {
            Position = position;
            IsArrived = isArrived;
        }

        /// <summary>
        ///     Position the monster stands on after the step
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        ///     Whether the target is reached: the caller stops walking and sends the stop packet
        /// </summary>
        public bool IsArrived { get; }
    }

    /// <summary>
    ///     Movement of a monster towards a point, the arithmetic part of it only. The system knows
    ///     nothing about sessions, packets and the scheduler - it answers where the monster stands
    ///     after one tick of a walk, and the caller decides what to send and what to apply.
    ///     TODO: there are no paths and no obstacles here, every step goes straight to the target -
    ///     the collision volumes of the maps the original walks around are not in the databases the
    ///     server has. A monster walks through a wall until they are somewhere to be read from
    /// </summary>
    public class MonsterMoveSystem
    {
        /// <summary>
        ///     How many milliseconds one second of the move rate is spread over
        /// </summary>
        private const float MillisecondsPerSecond = 1000f;

        /// <summary>
        ///     Speed a monster walks with, in units per second. It is half of the move rate of the
        ///     parm row, and exactly this number goes into the speed field of 5190: the client
        ///     interpolates the walk with it, so the number the packet carries and the number the
        ///     steps are cut by have to be the very same one
        /// </summary>
        /// <param name="moveRateOrg">ParmMonster.MoveRateOrg of the monster</param>
        public static float GetMoveSpeed(short moveRateOrg)
        {
            return moveRateOrg / 2f;
        }

        /// <summary>
        ///     Walk the monster from its position towards the target by the length one tick covers,
        ///     that is speed multiplied by the interval
        /// </summary>
        /// <param name="from">Position the server holds for the monster, never null</param>
        /// <param name="to">Point the monster walks to</param>
        /// <param name="speed">Walking speed in units per second, <see cref="GetMoveSpeed"/></param>
        /// <param name="intervalMilliseconds">Length of the tick the step is cut for</param>
        /// <returns>The new position and whether the target is reached</returns>
        public MonsterMoveStep Step(Vector3 from, Vector3 to, float speed, int intervalMilliseconds)
        {
            float stepLength = speed * intervalMilliseconds / MillisecondsPerSecond;

            // A step that leads nowhere - a parm with a zero move rate, a tick of no length or a
            // target that carries NaN - leaves the monster where it stands and reports the walk as
            // over. Reporting it as still walking would make the caller repeat the same useless
            // step every tick, and a NaN target would settle in the position of a monster and leak
            // into every packet about it
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null || !IsFinite(to) || !IsFinite(from) || !float.IsFinite(stepLength) || stepLength <= 0f)
            {
                return new MonsterMoveStep(new Vector3(from), true);
            }

            float distanceSq = MoveSystem.GetDistance2DSq(from, to);

            // The target is closer than one step: the monster stands exactly on it and does not
            // overshoot. The height comes from the target as well - it is the only step that
            // takes the monster off the height it walked on
            if (distanceSq <= stepLength * stepLength)
            {
                return new MonsterMoveStep(new Vector3(to), true);
            }

            float ratio = stepLength / (float)Math.Sqrt(distanceSq);

            // Y is the height and is not interpolated: the walk is cut on the plane, and the height
            // of the way between two points is not something the server can tell
            Vector3 position = new Vector3(
                from.X + (to.X - from.X) * ratio,
                from.Y,
                from.Z + (to.Z - from.Z) * ratio);

            return new MonsterMoveStep(position, false);
        }

        /// <summary>
        ///     Whether every coordinate of the position is a real number
        /// </summary>
        /// <param name="position">Position to check</param>
        private static bool IsFinite(Vector3 position)
        {
            return float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);
        }
    }
}

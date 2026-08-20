using Microsoft.Extensions.Options;
using Packets.Server.Game.Structures;
using Server.Game.Models.Settings;

namespace Server.Game.Core.Systems
{
    /// <summary>
    ///     Outcome of a movement check. The values repeat the return codes of CPc::Ran so the
    ///     answer of the original and the answer of this system can be compared as they are
    /// </summary>
    public enum MoveResult
    {
        /// <summary>
        ///     The move is applied by the caller, nothing is sent back to the owner of the character
        /// </summary>
        Accepted = 0,

        /// <summary>
        ///     Moving to this position is not allowed at all: a position the server refuses to even
        ///     look at, or an impassable place. The original answers the same way when
        ///     CChar::CheckMove forbids the move or the height of the target had to be revised
        /// </summary>
        Forbidden = 1,

        /// <summary>
        ///     The distance between the current and the new position is too big for one packet:
        ///     a teleport, a speed hack or a client that lost the server position
        /// </summary>
        DistanceViolation = 3
    }

    /// <summary>
    ///     Validation of a character move, after CMap::MovedFps and CPc::Ran of the original.
    ///     The system knows nothing about sessions and packets: it answers whether a move from one
    ///     position to another is allowed, and the caller decides what to send and what to apply.
    ///     TODO: the sliding speed detector (CSpdHackChecker::AddPos/IsSpdHack, positions collected
    ///     over time and compared with the running speed, a teleport back to the last valid position
    ///     when it fires) is not ported yet; it reports its refusals as DistanceViolation.
    ///     The fields it needs are already on the character - GChar.SpdHackMaxDist, filled by
    ///     CalcSpeed out of the running speed, and GChar.SpdHackMovedDist, which nothing accumulates
    ///     yet - so the detector is built on them and not on new state.
    ///     TODO: the threshold of one tick has to be measured against a live client first: the
    ///     distance between two 5188 of a running character is what the default of 500 is guessed
    ///     against, and if a normal run goes over it every second move is refused (T9)
    /// </summary>
    public class MoveSystem
    {
        /// <summary>
        ///     Hard limit of a single move. CMap::MovedFps compares the squared 2D distance with
        ///     4 000 000, that is 2000 units, and this limit is not configurable in the original
        /// </summary>
        public const float MaxDistancePerMove = 2000f;

        /// <summary>
        ///     Threshold of one tick used when the configured value is out of range. In the original
        ///     the value comes from the content parameter of eContentsSpdHackChk
        /// </summary>
        public const int DefaultMaxDistancePerTick = 500;

        /// <summary>
        ///     Upper bound of the per tick threshold, the value itself is already out of range
        /// </summary>
        public const int LimitMaxDistancePerTick = 10000;

        private readonly float _maxDistancePerTickSq;

        public MoveSystem(IOptions<GameSetting> gameSetting)
        {
            MaxDistancePerTick = GetMaxDistancePerTick(gameSetting.Value.MoveMaxDistancePerTick);

            _maxDistancePerTickSq = (float)MaxDistancePerTick * MaxDistancePerTick;
        }

        /// <summary>
        ///     How far a character is allowed to move with one packet, in units
        /// </summary>
        public int MaxDistancePerTick { get; }

        /// <summary>
        ///     Check a move of a character from one position to another. The checks are ordered by
        ///     their price, the cheapest first, exactly as the original orders them
        /// </summary>
        /// <param name="mapNo">Map the character is on, TblPcState.mMapNo</param>
        /// <param name="from">Position the server holds for the character</param>
        /// <param name="to">Position the client asks to move to</param>
        /// <returns>Whether the caller may apply <paramref name="to"/> to the character</returns>
        public MoveResult CheckMove(int mapNo, Vector3 from, Vector3 to)
        {
            // A broken or forged packet: NaN and infinity pass every comparison below unnoticed and
            // would settle in the character state, from where they leak into the saved position and
            // into every packet about this character. The original has no counterpart of this check
            if (from == null || to == null || !IsFinite(from) || !IsFinite(to))
            {
                return MoveResult.Forbidden;
            }

            // CPc::Ran answers 0 at once when the position did not change: no side effects at all
            if (to.Equals(from))
            {
                return MoveResult.Accepted;
            }

            // CMap::MovedFps refuses a target outside the rectangle of the map
            if (!IsInsideMap(mapNo, to))
            {
                return MoveResult.DistanceViolation;
            }

            float distanceSq = GetDistance2DSq(from, to);

            // A jump across the map with one packet, the limit of CMap::MovedFps
            if (distanceSq >= MaxDistancePerMove * MaxDistancePerMove)
            {
                return MoveResult.DistanceViolation;
            }

            // The threshold of one tick, the limit of CPc::Ran. With the default of 500 it is the
            // stricter of the two and the limit above never fires on its own; with a configured
            // value above 2000 it is the other way round and this check becomes unreachable. Both
            // are kept to stay close to the original, where they live in different places and
            // answer with different codes. The comparisons differ the same way they do there: the
            // hard limit refuses the distance it is equal to, the threshold of a tick allows it
            if (distanceSq > _maxDistancePerTickSq)
            {
                return MoveResult.DistanceViolation;
            }

            if (IsMoveBlocked(mapNo, from, to))
            {
                return MoveResult.Forbidden;
            }

            return MoveResult.Accepted;
        }

        /// <summary>
        ///     Squared distance on the horizontal plane, GetDist2DSq of the original. The rectangle
        ///     of a map is given by mStxX/mStxZ/mEtxX/mEtxZ, so X and Z make the plane and Y is the
        ///     height: a character that falls down a cliff does not travel any 2D distance
        /// </summary>
        public static float GetDistance2DSq(Vector3 from, Vector3 to)
        {
            float distanceX = to.X - from.X;
            float distanceZ = to.Z - from.Z;

            return distanceX * distanceX + distanceZ * distanceZ;
        }

        /// <summary>
        ///     Whether the position is inside the rectangle of the map. Extension point: the sizes of
        ///     the maps (mStxX/mStxZ/mEtxX/mEtxZ of CMap) are read by the original from its own map
        ///     files and are not in the databases the server has, so every position is accepted until
        ///     the rectangles are somewhere to be read from
        /// </summary>
        /// <param name="mapNo">Map the character is on</param>
        /// <param name="position">Position the client asks to move to</param>
        protected virtual bool IsInsideMap(int mapNo, Vector3 position)
        {
            return true;
        }

        /// <summary>
        ///     Whether the way from one position to another is blocked. Extension point: the original
        ///     answers it with CMap::IsNotMoveablePc and with the height revision, both over the
        ///     collision volumes of the map, which the server does not have. Note that the original
        ///     splits the answer - impassability is refused by CMap::MovedFps with code 3, the height
        ///     revision by CPc::Ran with code 1 - while this single point stands for both and keeps
        ///     the code of a forbidden move
        /// </summary>
        /// <param name="mapNo">Map the character is on</param>
        /// <param name="from">Position the server holds for the character</param>
        /// <param name="to">Position the client asks to move to</param>
        protected virtual bool IsMoveBlocked(int mapNo, Vector3 from, Vector3 to)
        {
            return false;
        }

        /// <summary>
        ///     The per tick threshold as the original reads it: a value outside [500, 10000) is not
        ///     pulled to the nearest border but replaced by the default. An absent setting is a zero
        ///     and lands on the default as well
        /// </summary>
        /// <param name="configured">Value of GameSetting.MoveMaxDistancePerTick</param>
        private static int GetMaxDistancePerTick(int configured)
        {
            if (configured < DefaultMaxDistancePerTick || configured >= LimitMaxDistancePerTick)
            {
                return DefaultMaxDistancePerTick;
            }

            return configured;
        }

        /// <summary>
        ///     Whether every coordinate of the position is a real number
        /// </summary>
        /// <param name="position"></param>
        private static bool IsFinite(Vector3 position)
        {
            return float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);
        }
    }
}

using Packets.Core.Utilities;
using Packets.Server.Game.Enums;

namespace Packets.Server.Game.Structures
{
    /// <summary>
    ///     Unique identifier of an entity in the world. On the wire it is a single unsigned integer
    ///     packed as: low 20 bits are the number, next 5 bits are the class, next 3 bits are the
    ///     generation and the next one is the "number is taken" flag. The three generation bits are
    ///     the only thing by which the client tells an entity that has just respawned from the
    ///     corpse with the same number it has seen a moment ago, so the value has to change every
    ///     time the number is handed out again
    /// </summary>
    public class UniqueId
    {
        // Bit offsets of the fields inside the packed number
        private const int ClassShift = 20;
        private const int SeqShift = 25;
        private const int IsSeqShift = 28;

        // Masks of the fields, already shifted down to zero
        private const uint IdMask = 0xFFFFF;
        private const uint ClassMask = 0x1F;
        private const uint SeqMask = 0x7;
        private const uint IsSeqMask = 0x1;

        /// <summary>
        ///     How many generations fit in the field: the counter wraps around this value
        /// </summary>
        public const uint SeqCount = SeqMask + 1;

        /// <summary>
        ///     Generation of a number handed out for the first time
        /// </summary>
        public const uint FirstSeq = 1;

        public uint Id { get; set; }
        public uint Class { get; set; }
        public uint Seq { get; set; }
        public uint IsSeq { get; set; }

        public UniqueId(UniqueIdentifierType type)
        {
            Class = (uint)type;
            Seq = FirstSeq;
            IsSeq = 1;
        }

        /// <summary>
        ///     Whether two identifiers name one and the same entity. The number alone is not enough:
        ///     it is handed out again as soon as the one that held it is gone, so the class and the
        ///     generation of the handout are checked with it - that is what tells a newcomer from
        ///     the entity somebody still remembers by its number
        /// </summary>
        /// <param name="left">One identifier</param>
        /// <param name="right">The other identifier</param>
        public static bool IsSame(UniqueId left, UniqueId right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.Id == right.Id && left.Class == right.Class && left.Seq == right.Seq;
        }

        public void Read(FormationPackage formationPackage)
        {
            uint number = formationPackage.ReadUInteger();
            Id = number & IdMask;
            Class = (number >> ClassShift) & ClassMask;
            Seq = (number >> SeqShift) & SeqMask;
            IsSeq = (number >> IsSeqShift) & IsSeqMask;
        }

        public void Write(FormationPackage formationPackage)
        {
            uint number = (IsSeq & IsSeqMask) << IsSeqShift;
            number |= (Seq & SeqMask) << SeqShift;
            number |= (Class & ClassMask) << ClassShift;
            number |= Id & IdMask;
            formationPackage.AddUInteger(number);
        }
    }
}

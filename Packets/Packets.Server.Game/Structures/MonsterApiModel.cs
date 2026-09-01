using Packets.Core.Utilities;
using Packets.Server.Game.Enums;

namespace Packets.Server.Game.Structures
{
    public class MonsterApiModel
    {
        public MonsterApiModel()
        {
            UniqueIdentifier = new UniqueId(UniqueIdentifierType.Monster);
            Position = new Vector3();
            PointPosition = new Vector3();
        }

        /// <summary>
        ///     State of a monster that is down. Its point is empty - a corpse walks nowhere
        /// </summary>
        public const short StateDead = 0;

        /// <summary>
        ///     State of a monster that stands where the packet puts it
        /// </summary>
        public const short StateStanding = 1;

        /// <summary>
        ///     State of a monster that walks somewhere without a fight: around its own spot or back
        ///     to it. The point of the walk is <see cref="PointPosition"/>
        /// </summary>
        public const short StateWalking = 3;

        /// <summary>
        ///     State of a monster that fights: it either runs up to the one it fights - the point of
        ///     the run is <see cref="PointPosition"/> - or already stands in front of it and swings
        /// </summary>
        public const short StateAngry = 5;

        /// <summary>
        ///     What the monster is busy with at the moment it is drawn: <see cref="StateDead"/>,
        ///     <see cref="StateStanding"/>, <see cref="StateWalking"/> or <see cref="StateAngry"/>.
        ///     A monster that is drawn walking is walked by the client from <see cref="Position"/>
        ///     to <see cref="PointPosition"/> at once, without a walk packet of its own - that is
        ///     how somebody a monster has run up to sees it running and not standing. The name is
        ///     the one the field carried while only life and death were told apart
        /// </summary>
        public short AliveOrDead { get; set; }
        public short AttackRate { get; set; }
        public short MoveRate { get; set; }

        /// <summary>
        ///     Point the monster is walking to, zero for a monster that stands. It is read only for
        ///     the states that mean a walk, so a standing monster carries zeros here and not the
        ///     place it walked to last
        /// </summary>
        public Vector3 PointPosition { get; set; }

        public UniqueId UniqueIdentifier { get; set; }
        public uint ParmNo { get; set; }
        public Vector3 Position { get; set; }
        public float DirectionSight { get; set; }
        public int MonsterId { get; set; }
        public int TransformationId { get; set; }
        public short Reputation { get; set; }
        public ushort Level { get; set; }
        public short Hp { get; set; }
        public string OwnerName { get; set; }
        public byte SummonType { get; set; }
        public int OwnerPcNo { get; set; }
        public uint OwnerPcGuildNo { get; set; }

        public void Read(FormationPackage formationPackage)
        {
            AliveOrDead = formationPackage.ReadShort();
            AttackRate = formationPackage.ReadShort();
            MoveRate = formationPackage.ReadShort();
            formationPackage.ReadBytes(2);
            PointPosition.Read(formationPackage);
            UniqueIdentifier.Read(formationPackage);
            ParmNo = formationPackage.ReadUInteger();
            Position.Read(formationPackage);
            DirectionSight = formationPackage.ReadFloat();
            MonsterId = formationPackage.ReadInteger();
            TransformationId = formationPackage.ReadInteger();
            Reputation = formationPackage.ReadShort();
            Level = formationPackage.ReadUShort();
            Hp = formationPackage.ReadShort();
            OwnerName = FormationPackageUtility.GetText(formationPackage.ReadBytes(15), 0);
            SummonType = formationPackage.ReadByte();
            formationPackage.ReadBytes(6);
            OwnerPcNo = formationPackage.ReadInteger();
            OwnerPcGuildNo = formationPackage.ReadUInteger();
        }

        public void Write(FormationPackage formationPackage)
        {
            formationPackage.AddShort(AliveOrDead);
            formationPackage.AddShort(AttackRate);
            formationPackage.AddShort(MoveRate);
            formationPackage.AddZeroBytes(2);
            PointPosition.Write(formationPackage);
            UniqueIdentifier.Write(formationPackage);
            formationPackage.AddUInteger(ParmNo);
            Position.Write(formationPackage);
            formationPackage.AddFloat(DirectionSight);
            formationPackage.AddInteger(MonsterId);
            formationPackage.AddInteger(TransformationId);
            formationPackage.AddShort(Reputation);
            formationPackage.AddUShort(Level);
            formationPackage.AddShort(Hp);
            formationPackage.AddBytes(FormationPackageUtility.GetBytes(OwnerName, 15));
            formationPackage.AddByte(SummonType);
            formationPackage.AddZeroBytes(6);
            formationPackage.AddInteger(OwnerPcNo);
            formationPackage.AddUInteger(OwnerPcGuildNo);
        }
    }
}

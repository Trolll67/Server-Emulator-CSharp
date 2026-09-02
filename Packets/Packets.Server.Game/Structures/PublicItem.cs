using Packets.Core.Utilities;
using Packets.Server.Game.Enums;

namespace Packets.Server.Game.Structures
{
    public class PublicItem
    {
        /// <summary>
        ///     Identifier the original puts into the tail of the record when nobody holds the item:
        ///     the number is empty and the flag of the handout is off, so the client reads it as
        ///     "reserved for nobody". The original marks an item a player has thrown away with
        ///     exactly this value, and we mark every item on the ground with it - the reserve of
        ///     the loot for the one who killed the monster is not reproduced
        /// </summary>
        public const uint NoDropOwner = 0x1F00000;

        public PublicItem()
        {
            UniqueIdentifier = new UniqueId(UniqueIdentifierType.Item);
            Item = new ItemApiModel();
            Position = new Vector3();
        }

        public UniqueId UniqueIdentifier { get; set; }

        /// <summary>
        ///     Four bytes right after the identifier. What the field means is not established -
        ///     the question number 2 of the plan to the owner: the reference shows 250...450 in
        ///     steps of five there for an item that has just dropped, a different number for every
        ///     drop, and zero for an item that has been lying on the ground for a while. Nobody
        ///     fills it in until the meaning is known, we send a zero
        /// </summary>
        public uint DropInfo { get; set; }

        public ItemApiModel Item { get; set; }
        public Vector3 Position { get; set; }

        public void Read(FormationPackage formationPackage)
        {
            UniqueIdentifier.Read(formationPackage);
            DropInfo = formationPackage.ReadUInteger();

            Item.Read(formationPackage);

            Position.Read(formationPackage);
            formationPackage.ReadBytes(4);
        }

        /// <summary>
        ///     Write the record with <see cref="NoDropOwner"/> in the tail - the way the packet
        ///     about a single item has it
        /// </summary>
        /// <param name="formationPackage"></param>
        public void Write(FormationPackage formationPackage)
        {
            Write(formationPackage, NoDropOwner);
        }

        /// <summary>
        ///     Write the record with the tail the caller gives. The packet about a single item
        ///     puts the reserve of the item there, the packet with the batch of the items around
        ///     lays its tail two bytes off by the only sample of the reference there is, and a
        ///     constant written by a wrong offset would overwrite a field that is really there
        /// </summary>
        /// <param name="formationPackage"></param>
        /// <param name="dropOwner">Four bytes of the tail of the record</param>
        public void Write(FormationPackage formationPackage, uint dropOwner)
        {
            UniqueIdentifier.Write(formationPackage);
            formationPackage.AddUInteger(DropInfo);

            Item.Write(formationPackage);

            Position.Write(formationPackage);
            formationPackage.AddUInteger(dropOwner);
        }
    }
}

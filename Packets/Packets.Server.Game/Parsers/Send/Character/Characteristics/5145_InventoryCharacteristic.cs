using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Character.Characteristics;

namespace Packets.Server.Game.Parsers.Send.Character.Characteristics
{
    /// <summary>
    ///     Parser charactiristic in inventory
    /// </summary>
    [ParserSend]
    public class InventoryCharacteristic
    {
        [ParserAction(Core.Enums.PacketType.InventoryCharacteristic)]
        public byte[] Parsing(InventoryCharacteristicModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddShort(model.DDv);
            formationPackage.AddShort(model.MDv);
            formationPackage.AddShort(model.RDv);

            formationPackage.AddShort(model.DPv);
            formationPackage.AddShort(model.MPv);
            formationPackage.AddShort(model.RPv);

            formationPackage.AddShort(model.DDD);
            formationPackage.AddShort(model.DHit);

            formationPackage.AddShort(model.RDD);
            formationPackage.AddShort(model.RHit);

            formationPackage.AddShort(model.MDD);
            formationPackage.AddShort(model.MHit);

            formationPackage.AddShort(model.Str);
            formationPackage.AddShort(model.Dex);
            formationPackage.AddShort(model.Int);

            formationPackage.AddShort(model.CriticalHit);

            // AbInfo of the original is eighteen shorts and ends right here: mMaxHp and mMaxMp are
            // shorts, not ints, and nothing follows them. Writing them as ints made the client read
            // the low half of the health as the health - right by luck - and the high half of it,
            // two zero bytes, as the whole mana: the player saw the right HP and 0/0 MP
            formationPackage.AddShort(model.HpMax);
            formationPackage.AddShort(model.MpMax);

            return formationPackage.GetBytes();
        }
    }
}
using Database.DataModel.Enums;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Models.Send.Character;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Network;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Core.Factories
{
    public class CharacterFactory : ICharacterFactory
    {
        public void SendCompleteCreateCharacters(GameSession client, GPc pc)
        {
            CompleteCreateCharacterModel completeCreateCharactersModel = new CompleteCreateCharacterModel()
            {
                CharacterId = (int)pc.Simple.PcNo,
                Str = pc.Ability.Str,
                Dex = pc.Ability.Dex,
                Int = pc.Ability.Int
            };

            client.Send(completeCreateCharactersModel);
        }

        public void SendCompleteDeleteCharacters(GameSession client, GPc pc)
        {
            CompleteDeleteCharacterModel completeDeleteCharactersModel = new CompleteDeleteCharacterModel();

            // Send empty packet

            client.Send(completeDeleteCharactersModel);
        }

        public void SendInformationCharacters(GameSession client)
        {
            InformationCharacterModel informationCharactersModel = new InformationCharacterModel
            {
                // Клиент получает уровень доступа аккаунта из UspLoginUser
                Auth = client.Sessions?.UserAuth ?? 0
            };

            foreach (var pc in client.Pcs)
            {
                informationCharactersModel.Characters.Add(new Character
                {
                    Id = (int)pc.Simple.PcNo,
                    Class = (byte)pc.Simple.Class,
                    Gender = pc.Simple.Sex,
                    Head = pc.Simple.Head,
                    Face = pc.Simple.Face,
                    Level = (short)pc.Simple.Level,
                    Name = pc.Simple.NickName,
                    Str = pc.Ability.Str,
                    Dex = pc.Ability.Dex,
                    Int = pc.Ability.Int,
                    Chaotic = pc.Detail.Chaotic,
                    Position = pc.PositionCur
                });

                informationCharactersModel.Equipments.Add(CreateEquipment(pc));
            }

            client.Send(informationCharactersModel);
        }

        /// <summary>
        ///     Worn gear of a character as the selection screen has to draw it: every slot carries
        ///     the serial of the item in it, because the client tells one worn item from another by
        ///     the serial and by nothing else.
        ///     <para>
        ///     The list of the worn items is taken as one snapshot at the start of the build: an
        ///     equip operation publishes a new list instead of editing the one already published,
        ///     so a reference taken once stays still while every slot is read off it
        ///     </para>
        /// </summary>
        /// <param name="pc">Character the gear is collected of</param>
        private static Equipment CreateEquipment(GPc pc)
        {
            List<GPcEquip> worn = pc.Equip;

            return new Equipment
            {
                Weapon = ItemOfSlot(worn, ItemEquipTypeEnum.Weapon),
                Shield = ItemOfSlot(worn, ItemEquipTypeEnum.Shield),
                Armor = ItemOfSlot(worn, ItemEquipTypeEnum.Armor),
                FirstRing = ItemOfSlot(worn, ItemEquipTypeEnum.Ring1),
                SecondRing = ItemOfSlot(worn, ItemEquipTypeEnum.Ring2),
                Necklace = ItemOfSlot(worn, ItemEquipTypeEnum.Amulet),
                Boots = ItemOfSlot(worn, ItemEquipTypeEnum.Boot),
                Gloves = ItemOfSlot(worn, ItemEquipTypeEnum.Glove),
                Helmet = ItemOfSlot(worn, ItemEquipTypeEnum.Cap),
                Belt = ItemOfSlot(worn, ItemEquipTypeEnum.Belt),
                Cloak = ItemOfSlot(worn, ItemEquipTypeEnum.Cloak),
                SphereMastery = ItemOfSlot(worn, ItemEquipTypeEnum.ExpertnessMaterial),
                SphereSoul = ItemOfSlot(worn, ItemEquipTypeEnum.SoulMaterial),
                SphereDefense = ItemOfSlot(worn, ItemEquipTypeEnum.DefenseMaterial),
                SphereDestruction = ItemOfSlot(worn, ItemEquipTypeEnum.AttackMaterial),
                SphereLife = ItemOfSlot(worn, ItemEquipTypeEnum.LifeMaterial),
                SphereLuck = ItemOfSlot(worn, ItemEquipTypeEnum.EventAMaterial),
                SphereReincarnation = ItemOfSlot(worn, ItemEquipTypeEnum.EventBMaterial),
                SphereCharacteristics = ItemOfSlot(worn, ItemEquipTypeEnum.EventCMaterial)
            };
        }

        /// <summary>
        ///     Item worn in a slot of a snapshot, null for a slot nobody filled - the packet then
        ///     writes the slot as zeroes, the length of the block never changes. The slot is asked
        ///     of the equipment record and not of the item: the
        ///     item knows nothing about where it ended up, and two rings of one kind are told apart
        ///     by their records only. The serial comes off the record as well, whole and unchanged
        ///     since the record was built
        /// </summary>
        /// <param name="worn">Snapshot of the worn items</param>
        /// <param name="pos">Slot to look at</param>
        private static Item ItemOfSlot(List<GPcEquip> worn, ItemEquipTypeEnum pos)
        {
            GPcEquip equip = worn.FirstOrDefault(x => x.Pos == pos);
            if (equip == null)
            {
                return null;
            }

            return new Item
            {
                Id = equip.SerialNo,
                ItemId = equip.Item.Id
            };
        }
    }
}

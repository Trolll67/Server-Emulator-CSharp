using Database.DataModel.Enums;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Models.Send.Character;
using Packets.Server.Game.Models.Send.Inventory;
using Packets.Server.Game.Models.Send.MonsterNpc;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Network;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Core.Factories
{
    public class VisibleFactory : IVisibleFactory
    {
        /// <summary>
        ///     5107: everybody who is already on the screen of a session, drawn the way that session
        ///     has to see them
        /// </summary>
        /// <param name="clientsFrom">Sessions of the characters the packet tells about</param>
        /// <param name="clientTo">Session the packet is sent to</param>
        public void SendDisplayedCharacters(IEnumerable<GameSession> clientsFrom, GameSession clientTo)
        {
            ExistedPcAckModel existedPcAckModel = new ExistedPcAckModel();

            foreach (var clientFrom in clientsFrom)
            {
                existedPcAckModel.Character.Add(CreatePublicPc(clientFrom.Pc));
            }

            clientTo.Send(existedPcAckModel);
        }

        /// <summary>
        ///     5103: the character of one session as another session has to see it
        /// </summary>
        /// <param name="clientFrom">Session of the character the packet tells about</param>
        /// <param name="clientTo">Session the packet is sent to</param>
        /// <param name="isTeleport">
        ///     The character has to be put at the given position at once instead of being walked
        ///     there: a teleport, and the same for a client that is read back to the position of
        ///     the server after a refused move
        /// </param>
        public void SendDisplayedDetailsCharacter(GameSession clientFrom, GameSession clientTo, bool isTeleport = false)
        {
            DisplayedCharacterModel displayedCharactersModel = new DisplayedCharacterModel
            {
                IsTeleport = isTeleport,

                Character = CreatePublicPc(clientFrom.Pc)
            };

            clientTo.Send(displayedCharactersModel);
        }

        /// <summary>
        ///     A character as everybody around has to see it: who it is, where it stands and what it
        ///     wears. Both appearance packets draw one and the same character in one and the same
        ///     way, so both are built here.
        ///     <para>
        ///     The worn items are read off a single snapshot of the list taken at the start: an
        ///     equip operation publishes a new list instead of editing the one already published,
        ///     so a reference taken once stays still under the whole build - the character goes out
        ///     either in the gear it had before the change or in the gear it has after it, never in
        ///     a mix of both. The factories are called from the visibility pass, on a thread that
        ///     owns none of it
        ///     </para>
        /// </summary>
        /// <param name="pc">Character of the world the packet tells about</param>
        private static PublicPc CreatePublicPc(GPc pc)
        {
            List<GPcEquip> worn = pc.Equip;

            return new PublicPc
            {
                AliveOrDead = (byte)(pc.DeadTime == null ? 1 : 0),
                AttackRate = pc.Detail.AttackRate,
                MoveRate = pc.Detail.MoveRate,
                UniqueIdentifier = pc.UniqueId,
                Class = (byte)pc.Simple.Class,
                Gender = pc.Simple.Sex,
                Head = pc.Simple.Head,
                Face = pc.Simple.Face,
                Position = pc.PositionCur,
                Reputation = pc.Detail.Chaotic,
                Name = pc.Simple.NickName,
                Level = (short)pc.Simple.Level,
                ChaoticStatus = (int)pc.Detail.ChaoticStatus,
                PkCnt = 0,

                Weapon = ItemNoOfSlot(worn, ItemEquipTypeEnum.Weapon),
                Shield = ItemNoOfSlot(worn, ItemEquipTypeEnum.Shield),
                Armor = ItemNoOfSlot(worn, ItemEquipTypeEnum.Armor),
                Ring1 = ItemNoOfSlot(worn, ItemEquipTypeEnum.Ring1),
                Ring2 = ItemNoOfSlot(worn, ItemEquipTypeEnum.Ring2),
                Amulet = ItemNoOfSlot(worn, ItemEquipTypeEnum.Amulet),
                Boot = ItemNoOfSlot(worn, ItemEquipTypeEnum.Boot),
                Glove = ItemNoOfSlot(worn, ItemEquipTypeEnum.Glove),
                Cap = ItemNoOfSlot(worn, ItemEquipTypeEnum.Cap),
                Belt = ItemNoOfSlot(worn, ItemEquipTypeEnum.Belt),
                Cloak = ItemNoOfSlot(worn, ItemEquipTypeEnum.Cloak),
                ExpertnessMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.ExpertnessMaterial),
                SoulMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.SoulMaterial),
                DefenceMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.DefenseMaterial),
                AttackMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.AttackMaterial),
                LifeMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.LifeMaterial),
                EventAMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.EventAMaterial),
                EventBMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.EventBMaterial),
                EventCMaterial = ItemNoOfSlot(worn, ItemEquipTypeEnum.EventCMaterial)
            };
        }

        /// <summary>
        ///     Number of the item worn in a slot of a snapshot, zero for a slot nobody filled - the
        ///     client draws nothing on an empty slot. The slot is asked of the equipment record and
        ///     not of the item: the item knows nothing about where it ended up, and two rings of one
        ///     kind are told apart by their records only
        /// </summary>
        /// <param name="worn">Snapshot of the worn items</param>
        /// <param name="pos">Slot to look at</param>
        private static int ItemNoOfSlot(List<GPcEquip> worn, ItemEquipTypeEnum pos)
        {
            GPcEquip equip = worn.FirstOrDefault(x => x.Pos == pos);

            return equip?.Item.Id ?? 0;
        }

        public void SendDisplayedItems(GameSession client, IEnumerable<GPublicItem> itemGameModels)
        {
            ExistedItemAckModel displayedItemModel = new ExistedItemAckModel();

            foreach (var itemGameModel in itemGameModels)
            {
                displayedItemModel.Items.Add(CreatePublicItem(itemGameModel));
            }

            client.Send(displayedItemModel);
        }

        public void SendDisplayedDetailsItem(GameSession client, GPublicItem itemDroppedGame)
        {
            EnteredItemAckModel enteredItemModel = new EnteredItemAckModel()
            {
                Item = CreatePublicItem(itemDroppedGame)
            };

            client.Send(enteredItemModel);
        }

        /// <summary>
        ///     An item of the world as everybody around has to see it. Both packets about an item
        ///     on the ground draw one and the same item in one and the same way, so both are built
        ///     here.
        ///     <para>
        ///     A thing that is not identified goes out under the fake number of its parm row and
        ///     with the normal status: the original hides what has really dropped until the thing
        ///     is identified, and the real number would give it away in the tooltip of the client.
        ///     The serial number is the one of the thing itself - a thing that fell out of a
        ///     monster has none and goes out with a zero, a thing a player has thrown away carries
        ///     the number of its row
        ///     </para>
        /// </summary>
        /// <param name="itemGameModel">Item of the world the packet tells about</param>
        private static PublicItem CreatePublicItem(GPublicItem itemGameModel)
        {
            GItem item = itemGameModel.Item;

            return new PublicItem
            {
                Item = new ItemApiModel()
                {
                    Flag = (byte)(item.IsConfirm ? 1 : 0),
                    SerialNumber = item.SerialNumber,
                    ItemId = item.IsConfirm ? item.Id : item.FakeId,
                    Count = item.Count,
                    EndTick = item.EndTick,
                    ItemStatus = (byte)(item.IsConfirm ? item.Status : ItemStatusEnum.Normal),
                    UseCount = item.UseCount,
                    EatTime = item.EatTime,
                    // The original puts here the minutes left until the term of the thing
                    // ends, taken from its row in the DB of the player; we do not keep the
                    // minutes - zero, the way the original has it for a thing without a term.
                    // Filling it from the minutes of the procedure - together with the general
                    // repair of EndTick and of the reading of the bag
                    TermOfEffectivity = 0,
                    ItemBind = (byte)item.ItemBind,
                    Restore = item.Restore,
                    Hole = item.Hole
                },
                UniqueIdentifier = itemGameModel.UniqueId,
                Position = itemGameModel.Position
            };
        }

        public void SendDisplayedUnit(GameSession client, IEnumerable<GMonster> unitGameModels)
        {
            ExistedMonAckModel displayedNpcMonsterModel = new ExistedMonAckModel();

            foreach (var monster in unitGameModels)
            {
                MonsterApiModel npc = new MonsterApiModel
                {
                    AttackRate = monster.Detail.AttackRate,
                    Reputation = monster.Detail.Chaotic,
                    DirectionSight = monster.DirectionSight,
                    Hp = monster.GetHpDisplayed(monster.Simple.Hp),
                    Level = 0,
                    MonsterId = monster.ParmMon.ParmNo,
                    MoveRate = monster.Detail.MoveRate,
                    OwnerName = "",
                    OwnerPcGuildNo = 0,
                    OwnerPcNo = 0,
                    ParmNo = (uint)monster.ParmMon.ParmNo,
                    Position = monster.PositionCur,
                    SummonType = 0,
                    UniqueIdentifier = monster.UniqueId,
                    TransformationId = monster.ParmMon.ParmNo
                };

                SetAction(npc, monster);

                displayedNpcMonsterModel.NpcMonsters.Add(npc);
            }

            client.Send(displayedNpcMonsterModel);
        }

        public void SendDisplayedDetailsUnit(GameSession client, GMonster monster)
        {
            EnteredMonAckModel enteredMonAckModel = new EnteredMonAckModel()
            {
                Monster = new MonsterApiModel()
                {
                    AttackRate = monster.Detail.AttackRate,
                    Reputation = monster.Detail.Chaotic,
                    DirectionSight = monster.DirectionSight,
                    Hp = monster.GetHpDisplayed(monster.Simple.Hp),
                    Level = 0,
                    MonsterId = monster.ParmMon.ParmNo,
                    MoveRate = monster.Detail.MoveRate,
                    OwnerName = "",
                    OwnerPcGuildNo = 0,
                    OwnerPcNo = 0,
                    ParmNo = (uint)monster.ParmMon.ParmNo,
                    Position = monster.PositionCur,
                    SummonType = 0,
                    UniqueIdentifier = monster.UniqueId,
                    TransformationId = monster.ParmMon.ParmNo
                }
            };

            SetAction(enteredMonAckModel.Monster, monster);

            client.Send(enteredMonAckModel);
        }

        /// <summary>
        ///     Action block of an appearance packet: what the monster is doing and where it is going.
        ///     Everybody who is drawn a monster is drawn it the same way, so both packets about a
        ///     monster fill the block here.
        ///     <para>
        ///     The state of the AI is read under the lock of the model - the AI pass writes it on
        ///     its own thread. The point of the walk is NOT guarded by that lock: the AI writes it
        ///     outside of the hold, and the read is safe only because every walk gets a fresh point
        ///     of its own that is never changed afterwards - a reference comes out whole or null,
        ///     never half-written, though the pair of state and point may be one tick apart
        ///     </para>
        /// </summary>
        /// <param name="model">Monster of the packet, its action block still empty</param>
        /// <param name="monster">Monster of the world the block is filled from</param>
        private static void SetAction(MonsterApiModel model, GMonster monster)
        {
            MonsterAiState aiState;
            Vector3 pointPosition;

            lock (monster.Aggro.SyncRoot)
            {
                aiState = monster.AiState;
                pointPosition = monster._PosTo;
            }

            // A monster that is down is down, whatever it was busy with when it fell: the corpse
            // stands in the world until the garbage pass takes it, and it walks nowhere
            if (monster.DeadTime != null)
            {
                model.AliveOrDead = MonsterApiModel.StateDead;
                return;
            }

            if (aiState == MonsterAiState.Angry)
            {
                // Angry both while it runs after the one it fights and while it stands in front of
                // it and swings: the player a monster has run up to has to see it as it is at once,
                // without waiting for the next walk packet about it
                model.AliveOrDead = MonsterApiModel.StateAngry;
            }
            else if (pointPosition != null)
            {
                // Walking without a fight: the walk around the spot and the way home are one and the
                // same walk for the client, and it is the point that makes it a walk - a monster
                // without one stands, whatever the AI is going to do with it on its nearest tick
                model.AliveOrDead = MonsterApiModel.StateWalking;
            }
            else
            {
                model.AliveOrDead = MonsterApiModel.StateStanding;
            }

            if (pointPosition != null)
            {
                model.PointPosition = pointPosition;
            }
        }

        public void SendExitMap(GameSession client, UniqueId uniqueItemDrop, ExitMapWhy exitMapWhy)
        {
            ExitMapGbjAckModel exitMapModel = new ExitMapGbjAckModel()
            {
                UniqueItemDrop = uniqueItemDrop,
                Why = exitMapWhy
            };

            client.Send(exitMapModel);
        }
    }
}

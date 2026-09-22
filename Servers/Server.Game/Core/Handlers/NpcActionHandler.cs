using Database.DataModel.Enums;
using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Models.Receive.Npc;
using Packets.Server.Game.Models.Send;
using Packets.Server.Game.Models.Send.Npc;
using Packets.Server.Game.Models.Send.Settings;
using Packets.Server.Game.Structures;
using Packets.Core.Models.Common;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Core.Systems;
using Server.Game.Models.Game;
using Server.Game.Network;
using Server.Game.Services;
using Server.Game.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Core.Handlers
{
    [Handler]
    public class NpcActionHandler : INpcActionHandler
    {
        /// <summary>
        ///     How far a keeper is heard out, CPc::CheckTalkRange of the original: this much plus
        ///     the bodies of the two, measured on the plane of the world alone - the height is not
        ///     asked at all, so a keeper a floor below is still talked to
        /// </summary>
        private const int TalkRange = 400;

        /// <summary>
        ///     What the keeper charges for taking a thing into the warehouse, the number the
        ///     original sends with OPENSTORE_PUSH. It knows a second, higher price for the keepers
        ///     of one place of its own; that place is not reproduced here, so this is the only price
        /// </summary>
        private const int StorePushPrice = 30;

        /// <summary>
        ///     Values of the flag of CTrStoreSetPasswordAck that answer the two requests about the
        ///     password itself: the flag names what the window is for and never repeats the number
        ///     of the action it answers. The other two values of the flag belong to a warehouse that
        ///     is already locked and are not sent from here
        /// </summary>
        private const int StorePasswordSet = 2;
        private const int StorePasswordReset = 3;

        /// <summary>
        ///     What the client is told about a password of the warehouse. Passwords are not kept
        ///     anywhere yet, so every warehouse is an open one and the window opens without asking
        /// </summary>
        private const int StorePasswordNotSet = 0;

        private readonly INpcActionFactory _npcActionFactory;
        private readonly IErrorFactory _errorFactory;
        private readonly InventarSystem _inventarSystem;
        private readonly IInventoryFactory _inventoryFactory;
        private readonly IdentificationService _identificationService;
        private readonly ParmRepository _databaseBalanceService;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly SerialNumberService _serialNumberService;
        private readonly StoreService _storeService;
        private readonly ILogger<NpcActionHandler> _logger;

        public NpcActionHandler(ICharacteristicFactory characteristicFactory,
            IInventoryFactory inventarFactory,
            ParmRepository databaseBalanceService,
            INpcActionFactory npcActionFactory,
            IErrorFactory errorFactory,
            InventarSystem inventarSystem,
            IdentificationService identificationService,
            SerialNumberService serialNumberService,
            StoreService storeService,
            ILogger<NpcActionHandler> logger)
        {
            _characteristicFactory = characteristicFactory;
            _npcActionFactory = npcActionFactory;
            _errorFactory = errorFactory;
            _inventarSystem = inventarSystem;
            _inventoryFactory = inventarFactory;
            _identificationService = identificationService;
            _databaseBalanceService = databaseBalanceService;
            _serialNumberService = serialNumberService;
            _storeService = storeService;
            _logger = logger;
        }

        /// <summary>
        ///     The client clicked a keeper and asks for the window it opens. The original looks the
        ///     script of the keeper up by the number of its parm row and runs it; the script itself
        ///     is a compiled thing of its own and none of it is here yet, so the answer is the one
        ///     the original gives for a keeper it found no script for
        /// </summary>
        [HandlerAction(PacketType.ScriptReq)]
        public void Script(GameSession client, ScriptReqModel model)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            GMonster npc = StartTalking(client.Pc, model.UniqueIdentifier);

            if (npc == null)
            {
                return;
            }

            // Both numbers are zeros and neither of them comes from the request: the original fills
            // nothing but the keeper here, and the script is what would have filled the rest.
            // The request carries a number and a string of its own, and both are operands of that
            // script - with no script to read them they go nowhere.
            // TODO: the original sends this only for a keeper that has a line of dialog behind it
            // and answers nothing at all for the rest; the table of the dialogs is not read yet, so
            // every keeper opens the window here
            client.Send(new ScrDialogNoMsgAckModel
            {
                ScriptId = 0,
                SessionGameId = npc.UniqueId,
                Param = 0
            });
        }

        /// <summary>
        ///     The client asks the keeper it is talking to for one of the things a keeper does.
        ///     Everything but the first of them is answered out of the data of the server, and that
        ///     is what lets the warehouse work while the script engine is not here
        /// </summary>
        [HandlerAction(PacketType.ScriptProcReq)]
        public void ScriptProc(GameSession client, ScriptProcReqModel model)
        {
            if (!client.IsInWorld || client.Pc == null || client.Sessions == null)
            {
                return;
            }

            GMonster npc = StartTalking(client.Pc, model.UniqueIdentifier);

            if (npc == null)
            {
                return;
            }

            // The warehouse belongs to the account and not to the character: every character of
            // the account opens the same rows
            int userNo = client.Sessions.AccountId;

            switch (model.ScriptAction)
            {
                // The keeper is asked to take something in. The count goes first and the answer of
                // the keeper behind it - the client opens the window on that answer and reads the
                // price out of it
                case ScriptAction.OPENSTORE_PUSH:
                    _storeService.SendStoreCount(client, userNo);

                    client.Send(new ScriptProcAckModel
                    {
                        SessionGameId = npc.UniqueId,
                        Action = model.ScriptAction,
                        Param = StorePushPrice
                    });
                    break;

                // The keeper is asked to give something back: the count and the whole list behind
                // it, and the client opens the window on the list itself
                case ScriptAction.OPENSTORE_POP:
                    _storeService.SendStoreCount(client, userNo);
                    _storeService.SendStoreList(client, userNo);
                    break;

                // The player wants to put a password on the warehouse or to take it off. The
                // warehouse keeps none, so the answer says there is nothing to take off and the
                // client asks the player for a new one either way
                case ScriptAction.SET_PASSWORD_STORE:
                    client.Send(new StoreSetPasswordAckModel { Flag = StorePasswordSet, IsSet = StorePasswordNotSet });
                    break;

                case ScriptAction.RESET_PASSWORD_STORE:
                    client.Send(new StoreSetPasswordAckModel { Flag = StorePasswordReset, IsSet = StorePasswordNotSet });
                    break;

                // Everything else belongs to a mechanic that is not built yet - the shops, the
                // boards, the warehouse of a guild, the rankings - and the first of them, PROC,
                // waits for the script engine itself. A silent server looks to the player like a
                // keeper that is broken, so what it was asked for is written down instead
                default:
                    _logger.LogWarning("Character {Pc} asked a keeper for {Action}, and that is not carried yet",
                        client.Pc.Simple.NickName, model.ScriptAction);
                    break;
            }
        }

        /// <summary>
        ///     Take the keeper a request names and remember it on the character as the one it is
        ///     talking to. The original refuses silently on both ways out of here - a keeper that is
        ///     no longer in the world and one that stands too far away are answered with nothing at
        ///     all, and the window of the client simply does not open
        /// </summary>
        /// <param name="pc">Character that talks</param>
        /// <param name="uniqueId">Identifier of the keeper the request names</param>
        /// <returns>The keeper, none when there is nobody to talk to</returns>
        private GMonster StartTalking(GPc pc, UniqueId uniqueId)
        {
            GMonster npc = _identificationService.GetUnitByUniqueIdentifier(uniqueId);

            if (npc == null || !IsInTalkRange(pc, npc))
            {
                return null;
            }

            pc.TalkingNpc = npc;

            return npc;
        }

        /// <summary>
        ///     Whether the two stand close enough to talk, CPc::CheckTalkRange of the original: the
        ///     range is counted off the edges of the two bodies and not off their middles, and the
        ///     distance is the one of the plane - a keeper straight above or below is as near as one
        ///     standing beside the character
        /// </summary>
        /// <param name="pc">Character that talks</param>
        /// <param name="npc">Keeper it talks to</param>
        private static bool IsInTalkRange(GPc pc, GMonster npc)
        {
            float range = TalkRange + GetBodySize(pc) + GetBodySize(npc);

            return MoveSystem.GetDistance2DSq(pc.PositionCur, npc.PositionCur) <= range * range;
        }

        /// <summary>
        ///     Size of the body the range is widened by. It is taken off the shape the character
        ///     wears right now and not off the one it was born with - a transformed character is as
        ///     big as its shape - and a character whose parm row is not read yet has no body at all
        ///     rather than taking the check down with it
        /// </summary>
        /// <param name="character">Character the body belongs to</param>
        private static int GetBodySize(GChar character)
        {
            ParmMonster parm = character.ParmMonCur ?? character.ParmMon;

            return parm == null ? 0 : parm.BodySz;
        }

        [HandlerAction(PacketType.MerchantBuyReq)]
        public void MerchantBuy(GameSession client, MerchantBuyReqModel model)
        {
            //GItem itemSilver = client.Pc.Items.FirstOrDefault(i => i.Id == 409);

            //if (itemSilver == null)
            //{
                _errorFactory.SendServerError(client, PacketType.MerchantBuyReq, NakErrorType.UnknownError, false);
                return;
            //}

            //UnitPurchaseGameModel item = _databaseBalanceService.GetUnitPurchaseById(model.ItemId);
            //int price = item.Price * model.Count;

            //if (itemSilver.Count >= price)
            //{
            //    itemSilver.Count = itemSilver.Count - price;
            //    if (itemSilver.Count <= 0)
            //    {
            //        client.CharacterGame.Items.Remove(itemSilver);
            //    }

            //    ItemGameModel Item = new ItemGameModel()
            //    {
            //        SerialNumber = (ulong)itemSilver.Id,
            //        Count = price
            //    };

            //    _inventoryFactory.SendItemRemove(client, Item, Reason.RmBuy);
            //}
            //else
            //{
            //    _errorFactory.SendServerError(client, PacketType.MerchantBuyReq, NakErrorType.UnknownError, false);
            //    return;
            //}

            //ItemGameModel itemGame = _databaseBalanceService.GetItemById(item.ItemId);
            //ItemGameModel characterItemGame = client.CharacterGame.Items.FirstOrDefault(i => i.Id == model.ItemId);

            //if (itemGame.MaxStack)
            //{
            //    PublicItemGameModel gameItemModel = new PublicItemGameModel()
            //    {
            //        UniqueIdentifier = new Packets.Server.Game.Structures.UniqueIdentifier(Packets.Server.Game.Enums.UniqueIdentifierType.Item) { Id = _identificationService.GetUniqueIdentifier() },
            //        Item = new ItemGameModel(itemGame)
            //        {
            //            Buffs = _databaseBalanceService.GetBuffsById(itemGame.Id),
            //            IsConfirm = 1,
            //            Count = model.Count,
            //            Status = ItemStatusTypeEnum.Normal
            //        },
            //        DateCreate = DateTime.Now
            //    };

            //    if (characterItemGame != null && characterItemGame.Count + model.Count <= 2000000000)
            //    {
            //        characterItemGame.Count += model.Count;
            //        gameItemModel.Item.SerialNumber = (ulong)characterItemGame.Id;
            //        gameItemModel.Item.Id = (int)gameItemModel.Item.SerialNumber;

            //    }
            //    else
            //    {
            //        gameItemModel.Item.SerialNumber = _serialNumberService.GetSerialNumberIdentifier();
            //        gameItemModel.Item.Id = (int)gameItemModel.Item.SerialNumber;
            //        client.CharacterGame.Items.Add(gameItemModel.Item);
            //    }

            //    _inventoryFactory.SendItemAdd(client, gameItemModel, Reason.Buy);
            //    return;
            //}

            //for (int i = 0; i < model.Count; i++)
            //{
            //    ItemGameModel itemGameNew = new ItemGameModel(itemGame);

            //    itemGameNew.SerialNumber = _serialNumberService.GetSerialNumberIdentifier();
            //    itemGameNew.Id = (int)itemGameNew.SerialNumber;
            //    itemGameNew.IsConfirm = 1;
            //    itemGameNew.Count = 1;
            //    itemGameNew.Status = ItemStatusTypeEnum.Normal;
            //    itemGameNew.Buffs = _databaseBalanceService.GetBuffsById(itemGame.Id);

            //    PublicItemGameModel gameItemModel = new PublicItemGameModel()
            //    {
            //        UniqueIdentifier = new Packets.Server.Game.Structures.UniqueIdentifier(UniqueIdentifierType.Item) { Id = _identificationService.GetUniqueIdentifier() },
            //        Item = itemGameNew,
            //        DateCreate = DateTime.Now,
            //    };

            //    client.CharacterGame.Items.Add(gameItemModel.Item);
            //    _inventoryFactory.SendItemAdd(client, gameItemModel, Reason.Buy);
            //}

            ////TODO
            ////_databaseContext.Items.Add(newItem);
            ////_databaseContext.SaveChanges();
            //_characteristicFactory.SendInfoWeight(client);
        }
    }
}

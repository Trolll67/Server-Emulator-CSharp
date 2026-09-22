using System;
using System.Collections.Generic;
using System.Linq;
using Database.Fnl.Game;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Receive;
using Packets.Server.Game.Models.Send.Settings;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Network;

namespace Server.Game.Core.Handlers
{
    /// <inheritdoc />
    [Handler]
    public class WorldStateHandler : IWorldStateHandler
    {
        /// <summary>
        ///     EStoreType of the original: zero is the warehouse of the account, one and two are
        ///     the two levels of the guild warehouse
        /// </summary>
        private const int StoreTypeAccount = 0;

        private readonly IFnlGameRepository _gameRepository;
        private readonly ILogger<WorldStateHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        public WorldStateHandler(IFnlGameRepository gameRepository, ILogger<WorldStateHandler> logger)
        {
            _gameRepository = gameRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.StoreReq)]
        public void StoreHandle(GameSession client, StoreReqModel storeReqModel)
        {
            if (!client.IsInWorld || client.Pc == null || client.Sessions == null)
            {
                return;
            }

            // The warehouse belongs to the account and not to the character: every character of
            // the account opens the same rows
            int userNo = client.Sessions.AccountId;

            switch (storeReqModel.Action)
            {
                // The short list. The original answers it without asking for the keeper, the
                // distance or the password - this is the question the client puts the moment it
                // is in the world, only to learn whether anything lies in the warehouse at all
                case StoreActionType.CheckRequestList:
                    SendCheckStoreList(client, userNo);
                    break;

                // The whole list, the answer to opening the warehouse
                case StoreActionType.RequestList:
                    SendStoreList(client, userNo);
                    break;

                // The keeper is asked to take something in: the original names the count first
                // and then lets its script open the window
                case StoreActionType.PushRequest:
                    SendStoreCount(client, userNo);
                    break;

                // The keeper is asked to give something back: the count and the list behind it
                case StoreActionType.PopRequest:
                    SendStoreCount(client, userNo);
                    SendStoreList(client, userNo);
                    break;

                // Putting a thing in and taking it out are not carried yet: both move a row
                // between the bag and the warehouse and cost money, and none of that is written.
                // A silent server would look to the player like a thing that vanished, so the
                // attempt is written down instead
                case StoreActionType.PushItem:
                case StoreActionType.PullItem:
                    _logger.LogWarning("Character {Pc} asked the warehouse to {Action} {Count} thing(s), and moving things in and out is not carried yet",
                        client.Pc.Simple.NickName, storeReqModel.Action, storeReqModel.Count);
                    break;

                // The original does nothing for this one either
                case StoreActionType.Etc:
                    break;
            }
        }

        /// <summary>
        ///     The short list: what lies in the warehouse and how much of it, two numbers a row
        /// </summary>
        private void SendCheckStoreList(GameSession client, int userNo)
        {
            CheckStoreListAckModel model = new CheckStoreListAckModel();

            foreach (StoreItemRow row in ReadStore(userNo).Take(StoreListAckModel.MaxRows))
            {
                model.Rows.Add(new CheckStoreRowModel { ItemNo = row.ItemNo, Count = row.Cnt });
            }

            client.Send(model);
        }

        /// <summary>
        ///     The whole list of the warehouse of the account
        /// </summary>
        private void SendStoreList(GameSession client, int userNo)
        {
            StoreListAckModel model = new StoreListAckModel { StoreType = StoreTypeAccount };

            // The original cuts the list at three hundred rows: it is the room of the packet, and
            // the rows past it stay in the warehouse and are still counted by the count packet
            foreach (StoreItemRow row in ReadStore(userNo).Take(StoreListAckModel.MaxRows))
            {
                model.Rows.Add(new StoreRowModel
                {
                    SerialNo = row.SerialNo,
                    ItemNo = row.ItemNo,
                    IsConfirm = row.IsConfirm,
                    Status = row.Status,
                    Count = row.Cnt,
                    CountUse = row.CntUse,
                    Owner = row.Owner,
                    TermOfEffectivity = row.PracticalPeriod,
                    HoleCount = row.HoleCount
                });
            }

            client.Send(model);
        }

        /// <summary>
        ///     How many rows the warehouse really holds - the number of the database, which may be
        ///     larger than a list packet is able to carry
        /// </summary>
        private void SendStoreCount(GameSession client, int userNo)
        {
            client.Send(new StoreCountAckModel { Count = (uint)ReadStore(userNo).Count });
        }

        /// <summary>
        ///     Rows of the warehouse. A database that did not answer leaves the warehouse empty
        ///     rather than leaving the client without an answer at all
        /// </summary>
        private IReadOnlyList<StoreItemRow> ReadStore(int userNo)
        {
            try
            {
                return _gameRepository.GetStoreList(userNo);
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                _logger.LogError(e, "Can not read the warehouse of account {UserNo}", userNo);

                return new List<StoreItemRow>();
            }
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.LetterRefuseReq)]
        public void LetterRefuseHandle(GameSession client, LetterRefuseReqModel letterRefuseReqModel)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            // The original answers with the mark the character ends up with, and the client draws
            // its checkbox by that answer. The mark lives in TblPcState.mIsLetterLimit; it is kept
            // on the character here and reaches the row of the character once the letters are built
            client.Pc.Detail.LetterLimit = letterRefuseReqModel.IsOn;

            client.Send(new LetterRefuseAckModel { IsOn = client.Pc.Detail.LetterLimit });
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.GiftBoxExistReq)]
        public void GiftBoxExistHandle(GameSession client, GiftBoxExistReqModel giftBoxExistReqModel)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            // Gift boxes are a part of the billing base this server does not touch yet, so nothing
            // is ever waiting. The client takes the zero and leaves the button alone
            client.Send(new GiftBoxExistAckModel { IsExist = 0 });
        }
    }
}

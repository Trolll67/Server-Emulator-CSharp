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
        private readonly ILogger<WorldStateHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="logger"></param>
        public WorldStateHandler(ILogger<WorldStateHandler> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.StoreReq)]
        public void StoreHandle(GameSession client, StoreReqModel storeReqModel)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            // There is no warehouse on this server yet, so the only answer that is true is an empty
            // one: the client draws no rows and asks for nothing else. A move that would put things
            // in or take them out is written down instead of being quietly swallowed - a player who
            // tried it has to show up in the log, not lose a thing
            if (storeReqModel.Action != StoreActionType.RequestList &&
                storeReqModel.Action != StoreActionType.CheckRequestList)
            {
                _logger.LogWarning("Character {Pc} asked the warehouse to {Action} {Count} thing(s), and this server keeps no warehouse yet",
                    client.Pc.Simple.NickName, storeReqModel.Action, storeReqModel.Count);
            }

            client.Send(new CheckStoreListAckModel { Count = 0 });
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

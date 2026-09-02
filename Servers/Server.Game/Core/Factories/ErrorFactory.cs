using Packets.Core.Enums;
using Packets.Server.Game.Models.Send;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Network;

namespace Server.Game.Core.Factories
{
    public class ErrorFactory : IErrorFactory
    {
        public void SendServerError(GameSession client, PacketType packet, GameServerErrorType gameServerError, bool isMsgBox)
        {
            SendServerError(client, packet, gameServerError, 0, isMsgBox);
        }

        /// <summary>
        ///     1102: the request is refused and the client is told why. The eight bytes of the
        ///     request go back with the refusal - the original puts the identifier of the item
        ///     there when it refuses to put a thing on, so that the client knows which of the
        ///     things it asked about got the answer
        /// </summary>
        /// <param name="client">Session the refusal is sent to</param>
        /// <param name="packet">Opcode of the request that was refused</param>
        /// <param name="gameServerError">Reason of the refusal</param>
        /// <param name="etc">Eight bytes of the request, zero when it carries nothing to give back</param>
        /// <param name="isMsgBox">Whether the client shows the reason in a window of its own</param>
        public void SendServerError(GameSession client, PacketType packet, GameServerErrorType gameServerError, ulong etc, bool isMsgBox)
        {
            GameServerErrorModel gameServerErrorModel = new GameServerErrorModel
            {
                PacketType = packet,
                ErrorType = gameServerError,
                Etc = etc,
                IsMsgBox = isMsgBox
            };

            client.Send(gameServerErrorModel);
        }
    }
}

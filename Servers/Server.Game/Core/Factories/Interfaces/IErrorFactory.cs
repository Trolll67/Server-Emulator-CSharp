using Packets.Core.Enums;
using Packets.Server.Game.Models.Send;
using Packets.Core.Models.Common;
using Server.Game.Network;

namespace Server.Game.Core.Factories.Interfaces
{
    public interface IErrorFactory
    {
        void SendServerError(GameSession client, PacketType packet, NakErrorType gameServerError, bool isMsgBox);
        void SendServerError(GameSession client, PacketType packet, NakErrorType gameServerError, ulong etc, bool isMsgBox);
    }
}
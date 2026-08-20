using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Character
{
    /// <summary>
    ///     Model entered world acknowledgement. Уходит последним при входе персонажа в мир,
    ///     полезной нагрузки не несёт
    /// </summary>
    [Model(PacketType.EnteredWorldAck)]
    public class EnteredWorldAckModel
    {

    }
}

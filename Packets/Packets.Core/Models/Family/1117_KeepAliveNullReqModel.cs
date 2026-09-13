using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Core.Models.Family
{
    /// <summary>
    ///     Empty ping the servers of one world send each other so that a link which nobody uses
    ///     does not look the same as a broken one. CTrKeepAliveNullReq of the original, no body
    /// </summary>
    [Model(PacketType.KeepAliveNullReq)]
    public class KeepAliveNullReqModel
    {
    }
}

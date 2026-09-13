using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Login.Models.Receive
{
    /// <summary>
    ///     Asks the channel whether the server the player has chosen wants a phone confirmation
    ///     of the login. The client sends it after picking a server in the list and before it
    ///     connects there, which is why it looks like "server chosen" from the outside.
    ///     CTrARSAuthReq of the original: mUserNo, mUserID char(20), mSvrNo
    /// </summary>
    [Model(PacketType.ArsAuthReq)]
    public class ArsAuthReqModel
    {
        public int AccountId { get; set; }
        public string Login { get; set; }
        public short ServerId { get; set; }
    }
}
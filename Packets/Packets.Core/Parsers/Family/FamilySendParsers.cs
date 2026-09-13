using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Family;
using Packets.Core.Utilities;

namespace Packets.Core.Parsers.Family
{
    /// <summary>
    ///     Writes the packets the servers of one world send each other, see
    ///     <see cref="FamilyReceiveParsers"/>
    /// </summary>
    [ParserSend]
    public class FamilySendParsers
    {
        [ParserAction(PacketType.LoginFamilyReq)]
        public byte[] ParsingLoginFamilyReq(LoginFamilyReqModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddShort(model.SvrNo);
            formationPackage.AddInteger(model.Version);

            return formationPackage.GetBytes();
        }

        [ParserAction(PacketType.LoginFamilyAck)]
        public byte[] ParsingLoginFamilyAck(LoginFamilyAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddShort(model.SvrNo);

            return formationPackage.GetBytes();
        }

        [ParserAction(PacketType.LoginFamilyNak)]
        public byte[] ParsingLoginFamilyNak(LoginFamilyNakModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger((int)model.Error);
            formationPackage.AddShort(model.SvrNo);

            return formationPackage.GetBytes();
        }

        [ParserAction(PacketType.NotifySvrStateAck)]
        public byte[] ParsingNotifySvrStateAck(NotifySvrStateAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddShort(model.SvrNo);
            formationPackage.AddShort(model.MaxSesCnt);
            formationPackage.AddShort(model.BusySesCnt);

            return formationPackage.GetBytes();
        }

        [ParserAction(PacketType.KeepAliveNullReq)]
        public byte[] ParsingKeepAliveNullReq(KeepAliveNullReqModel model)
        {
            return new FormationPackage().GetBytes();
        }
    }
}

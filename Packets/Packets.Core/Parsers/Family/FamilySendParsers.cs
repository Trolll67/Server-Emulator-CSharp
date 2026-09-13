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
        /// <summary>
        ///     Room the kick keeps for whom it is about, see <see cref="FamilyReceiveParsers"/>
        /// </summary>
        private const int WhoSize = 15;

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

        [ParserAction(PacketType.KickPcReq)]
        public byte[] ParsingKickPcReq(KickPcReqModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddByte(model.IsPc ? (byte)1 : (byte)0);

            // One and the same room holds either the number of the account or the name
            if (model.IsPc)
            {
                formationPackage.AddBytes(FormationPackageUtility.GetBytes(model.PcName ?? string.Empty, WhoSize));
            }
            else
            {
                FormationPackage who = new FormationPackage();
                who.AddInteger(model.UserNo);
                who.AddZeroBytes(WhoSize - sizeof(int));

                formationPackage.AddBytes(who.GetBytes());
            }

            formationPackage.AddUInteger(model.Reason.Code);
            formationPackage.AddInteger(model.Reason.MsgGroup);
            formationPackage.AddLong(model.AddressNumber);

            return formationPackage.GetBytes();
        }

        [ParserAction(PacketType.KeepAliveNullReq)]
        public byte[] ParsingKeepAliveNullReq(KeepAliveNullReqModel model)
        {
            return new FormationPackage().GetBytes();
        }
    }
}

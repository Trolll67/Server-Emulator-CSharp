using System;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;
using Packets.Core.Models.Family;
using Packets.Core.Utilities;

namespace Packets.Core.Parsers.Family
{
    /// <summary>
    ///     Reads the packets the servers of one world send each other. Every server of the world
    ///     knows all of them: which ones it is going to meet depends on its kind, not on the build
    /// </summary>
    [ParserReceive]
    public class FamilyReceiveParsers
    {
        /// <summary>
        ///     Room the kick keeps for whom it is about: an account number or a character name
        /// </summary>
        private const int WhoSize = 15;

        [ParserAction(PacketType.LoginFamilyReq)]
        public LoginFamilyReqModel ParsingLoginFamilyReq(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            return new LoginFamilyReqModel
            {
                SvrNo = formationPackage.ReadShort(),
                Version = formationPackage.ReadInteger()
            };
        }

        [ParserAction(PacketType.LoginFamilyAck)]
        public LoginFamilyAckModel ParsingLoginFamilyAck(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            return new LoginFamilyAckModel
            {
                SvrNo = formationPackage.ReadShort()
            };
        }

        [ParserAction(PacketType.LoginFamilyNak)]
        public LoginFamilyNakModel ParsingLoginFamilyNak(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            return new LoginFamilyNakModel
            {
                Error = (FamilyErrorType)formationPackage.ReadInteger(),
                SvrNo = formationPackage.ReadShort()
            };
        }

        [ParserAction(PacketType.NotifySvrStateAck)]
        public NotifySvrStateAckModel ParsingNotifySvrStateAck(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            return new NotifySvrStateAckModel
            {
                SvrNo = formationPackage.ReadShort(),
                MaxSesCnt = formationPackage.ReadShort(),
                BusySesCnt = formationPackage.ReadShort()
            };
        }

        [ParserAction(PacketType.KickPcReq)]
        public KickPcReqModel ParsingKickPcReq(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            bool isPc = formationPackage.ReadByte() != 0;
            byte[] who = formationPackage.ReadBytes(WhoSize);

            return new KickPcReqModel
            {
                IsPc = isPc,
                UserNo = isPc ? 0 : BitConverter.ToInt32(who, 0),
                PcName = isPc ? FormationPackageUtility.GetText(who, 0) : null,
                Reason = NakErrorType.FromCode(formationPackage.ReadUInteger(), formationPackage.ReadInteger()),
                AddressNumber = formationPackage.ReadLong()
            };
        }

        [ParserAction(PacketType.KeepAliveNullReq)]
        public KeepAliveNullReqModel ParsingKeepAliveNullReq(byte[] data)
        {
            return new KeepAliveNullReqModel();
        }
    }
}

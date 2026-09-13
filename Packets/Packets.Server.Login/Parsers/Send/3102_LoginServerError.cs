using System;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Login.Models.Send;

namespace Packets.Server.Login.Parsers.Send
{
    /// <summary>
    ///     Parser server error
    /// </summary>
    [ParserSend]
    public class LoginServerError
    {
        /// <summary>
        ///     Length of the text of the reason, the original keeps char(201) for it
        /// </summary>
        private const int ReasonSize = 201;

        [ParserAction(PacketType.LoginServerError)]
        public byte[] Parsing(LoginServerErrorModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            // Why the login was refused
            formationPackage.AddUInteger(model.Error.Code);

            // Until when the account is certified, eight words of a system time
            AddSystemTime(formationPackage, model.EndCertify);

            // Text for the player, empty on every path but the ones of the publishers
            formationPackage.AddBytes(FormationPackageUtility.GetBytes(model.Reason ?? string.Empty, ReasonSize));

            // The ban has no end
            formationPackage.AddInteger(model.IsEternalSanction ? 1 : 0);

            return formationPackage.GetBytes();
        }

        /// <summary>
        ///     Writes a moment the way the original does: the eight words of a system time,
        ///     all zeroes when there is no moment to tell about
        /// </summary>
        private static void AddSystemTime(FormationPackage formationPackage, DateTime? moment)
        {
            if (moment == null)
            {
                formationPackage.AddZeroBytes(16);
                return;
            }

            DateTime value = moment.Value;

            formationPackage.AddUShort((ushort)value.Year);
            formationPackage.AddUShort((ushort)value.Month);
            formationPackage.AddUShort((ushort)value.DayOfWeek);
            formationPackage.AddUShort((ushort)value.Day);
            formationPackage.AddUShort((ushort)value.Hour);
            formationPackage.AddUShort((ushort)value.Minute);
            formationPackage.AddUShort((ushort)value.Second);
            formationPackage.AddUShort((ushort)value.Millisecond);
        }
    }
}

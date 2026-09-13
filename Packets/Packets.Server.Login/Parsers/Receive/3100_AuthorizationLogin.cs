using System;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Login.Models.Receive;

namespace Packets.Server.Login.Parsers.Receive
{
    /// <summary>
    ///     Parser authorization by login and password
    /// </summary>
    [ParserReceive]
    public class AuthorizationLogin
    {
        [ParserAction(PacketType.AuthorizationLogin)]
        public AuthorizationLoginModel Parsing(byte[] data)
        {
            AuthorizationLoginModel authorizationLoginModel = new AuthorizationLoginModel
            {
                Login = GetLogin(data),
                Password = GetPassword(data),
                IdIndex = data[IdIndexOffset],
                PswdIndex = data[PswdIndexOffset],
                Version = BitConverter.ToUInt32(data, VersionOffset),
                RscLength = BitConverter.ToUInt32(data, RscLengthOffset)
            };

            return authorizationLoginModel;
        }

        /// <summary>
        ///     Places of the fields that always stand where they stand. The rest of the packet is
        ///     noise with the login and the password hidden in it
        /// </summary>
        private const int PswdIndexOffset = 81;

        private const int IdIndexOffset = 256;
        private const int VersionOffset = 386;
        private const int RscLengthOffset = 416;

        private string GetLogin(byte[] data)
        {
            byte codeLogin = data[IdIndexOffset];
            codeLogin = (byte) (codeLogin / 8);

            int offsetLogin;
            switch (codeLogin)
            {
                case 0:
                    offsetLogin = 151;
                    break;
                case 1:
                    offsetLogin = 37;
                    break;
                case 2:
                    offsetLogin = 87;
                    break;
                case 3:
                    offsetLogin = 336;
                    break;
                case 4:
                    offsetLogin = 129;
                    break;
                case 5:
                    offsetLogin = 289;
                    break;
                case 6:
                    offsetLogin = 172;
                    break;
                case 7:
                    offsetLogin = 199;
                    break;
                default:
                    offsetLogin = 220;
                    break;
            }

            return FormationPackageUtility.GetText(data, offsetLogin);
        }

        private string GetPassword(byte[] data)
        {
            byte codePassword = data[PswdIndexOffset];
            codePassword = (byte) (codePassword / 2);

            int offsetPassword;
            switch (codePassword)
            {
                case 0:
                    offsetPassword = 260;
                    break;
                case 1:
                    offsetPassword = 60;
                    break;
                case 2:
                    offsetPassword = 108;
                    break;
                case 3:
                    offsetPassword = 4;
                    break;
                case 4:
                    offsetPassword = 314;
                    break;
                case 5:
                    offsetPassword = 357;
                    break;
                default:
                    offsetPassword = 390;
                    break;
            }

            return FormationPackageUtility.GetText(data, offsetPassword);
        }
    }
}
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;

namespace Packets.Server.Login.Models.Receive
{
    /// <summary>
    ///     Login of a player. The packet is three kilobytes of noise with the real fields hidden
    ///     in it: the login and the password sit in one of several places each, and two index
    ///     bytes say in which one. CTrCertifyUserReq of the original
    /// </summary>
    [Model(PacketType.AuthorizationLogin)]
    public class AuthorizationLoginModel
    {
        public string Login { get; set; }
        public string Password { get; set; }

        /// <summary>
        ///     Which of the nine places the login was put in, as it came in the packet: the place
        ///     is this divided by eight
        /// </summary>
        public byte IdIndex { get; set; }

        /// <summary>
        ///     Which of the seven places the password was put in, as it came in the packet:
        ///     the place is this divided by two
        /// </summary>
        public byte PswdIndex { get; set; }

        /// <summary>
        ///     Build of the client. The original refuses a login whose build is not the one the
        ///     server was told to expect
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        ///     Length of the key of the client resources. The original only checks that it is
        ///     within bounds, the key itself is compared where the resources are checked
        /// </summary>
        public uint RscLength { get; set; }

        /// <summary>
        ///     Checks that the packet is put together the way the client puts it together. The
        ///     original does it before it looks at anything else in the packet
        /// </summary>
        /// <param name="idSize">Longest login this server takes, it depends on the country</param>
        /// <param name="pswdSize">Longest password this server takes</param>
        /// <returns>Null when the packet is whole, otherwise why it is not</returns>
        public NakErrorType CheckIntegrity(int idSize, int pswdSize)
        {
            if (IdIndex / 8 >= 9 || PswdIndex / 2 >= 7)
            {
                return NakErrorType.TrBrokenIntegrity;
            }

            if (RscLength < MinRscLength || RscLength >= MaxRscLength)
            {
                return NakErrorType.TrBrokenIntegrity;
            }

            // Both an empty field and one longer than this server takes are the same reason in
            // the original, for the login and for the password alike
            if (string.IsNullOrEmpty(Login) || Login.Length > idSize)
            {
                return NakErrorType.UserInvalidId;
            }

            if (string.IsNullOrEmpty(Password) || Password.Length > pswdSize)
            {
                return NakErrorType.UserInvalidId;
            }

            return null;
        }

        /// <summary>
        ///     Bounds the original keeps the length of the resource key inside
        /// </summary>
        private const uint MinRscLength = 8;

        private const uint MaxRscLength = 500;
    }
}

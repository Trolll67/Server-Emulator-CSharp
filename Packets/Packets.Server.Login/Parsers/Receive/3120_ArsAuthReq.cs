using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Login.Models.Receive;

namespace Packets.Server.Login.Parsers.Receive
{
    /// <summary>
    ///     Parser of the phone confirmation request
    /// </summary>
    [ParserReceive]
    public class ArsAuthReq
    {
        [ParserAction(PacketType.ArsAuthReq)]
        public ArsAuthReqModel Parsing(byte[] data)
        {
            ArsAuthReqModel authorizationModel = new ArsAuthReqModel();

            FormationPackage formationPackage = new FormationPackage(data);
            authorizationModel.AccountId = formationPackage.ReadInteger();
            authorizationModel.Login = FormationPackageUtility.GetText(formationPackage.ReadBytes(20), 0);
            authorizationModel.ServerId = formationPackage.ReadShort();

            return authorizationModel;
        }
    }
}
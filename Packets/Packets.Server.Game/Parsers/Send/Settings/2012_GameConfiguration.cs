using System.Linq;
using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of eCTrContentsAck: a hundred entries of a byte and thirty floats each, and the
    ///     number of the page behind them. The length is fixed, so an option the server has no row
    ///     for still takes its place in the packet
    /// </summary>
    [ParserSend]
    public class GameConfiguration
    {
        [ParserAction(Core.Enums.PacketType.GameConfiguration)]
        public byte[] Parsing(GameConfigurationModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            for (int i = 0; i < GameConfigurationModel.ContentsPerPage; i++)
            {
                GameConfigurationContent content = model.Contents.ElementAtOrDefault(i);

                formationPackage.AddByte(content != null && content.IsSetup ? (byte)1 : (byte)0);

                for (int value = 0; value < GameConfigurationModel.ValuesPerContent; value++)
                {
                    // The table keeps the values as float, and the wire takes four bytes each:
                    // a row that is shorter than thirty values fills the rest with zeroes
                    double number = content?.Values != null && value < content.Values.Count
                        ? content.Values[value]
                        : 0d;

                    formationPackage.AddFloat((float)number);
                }
            }

            formationPackage.AddUInteger(model.ContentsSeq);

            return formationPackage.GetBytes();
        }
    }
}

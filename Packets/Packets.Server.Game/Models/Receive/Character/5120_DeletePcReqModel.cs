using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Receive.Character
{
    /// <summary>
    ///     Model for delete pc
    /// </summary>
    [Model(PacketType.DeletePcReq)]
    public class DeletePcReqModel
    {
        public uint PcNo { get; set; }

        /// <summary>
        ///     Слот на экране выбора персонажа. Клиент его присылает, но удаление идёт
        ///     по PcNo, поэтому сервером поле не используется
        /// </summary>
        public byte Slot { get; set; }
    }
}
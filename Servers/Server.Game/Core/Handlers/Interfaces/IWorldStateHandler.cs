using Packets.Server.Game.Models.Receive;
using Server.Game.Network;

namespace Server.Game.Core.Handlers.Interfaces
{
    /// <summary>
    ///     The three questions the client asks the moment it is in the world: what lies in the
    ///     warehouse, whether the character refuses letters and whether a gift box waits for it.
    ///     Each of them belongs to a subsystem of its own, and each moves to the handler of that
    ///     subsystem once it is built
    /// </summary>
    public interface IWorldStateHandler
    {
        /// <summary>
        ///     The client asks the warehouse to do something: to name its rows, to count them,
        ///     or to move a thing in or out
        /// </summary>
        void StoreHandle(GameSession client, StoreReqModel storeReqModel);

        /// <summary>
        ///     The client sets, or asks for, the "refuse letters" mark of the character
        /// </summary>
        void LetterRefuseHandle(GameSession client, LetterRefuseReqModel letterRefuseReqModel);

        /// <summary>
        ///     The client asks whether a gift box waits for the character
        /// </summary>
        void GiftBoxExistHandle(GameSession client, GiftBoxExistReqModel giftBoxExistReqModel);
    }
}

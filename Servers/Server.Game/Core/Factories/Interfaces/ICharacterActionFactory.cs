using Server.Game.Network;

namespace Server.Game.Core.Factories.Interfaces
{
    public interface ICharacterActionFactory
    {
        void SendMovedCharacters(GameSession clientTo, GameSession clientFrom);

        void SendStopMoveCharacter(GameSession clientTo, GameSession clientFrom, byte flag);

        void SendJumpCharacter(GameSession clientTo, GameSession clientFrom);

        void SendDirectionCharacter(GameSession clientTo, GameSession clientFrom);

        void SendRespawnCharacter(GameSession clientTo, GameSession clientFrom);
    }
}

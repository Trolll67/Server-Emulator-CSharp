using Packets.Server.Game.Models.Send.Character;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Network;

namespace Server.Game.Core.Factories
{
    public class CharacterActionFactory : ICharacterActionFactory
    {
        /// <summary>
        ///     5189: an accepted move of a character. Everything comes from the state of the mover
        ///     the server already applied, the position of the request itself is never repeated.
        ///     The packet is meant for the neighbours of the mover - the mover walks on its own side
        ///     and is only told when the server refuses the move - so the caller is the one who
        ///     leaves it out of the receivers
        /// </summary>
        /// <param name="clientTo">Session the packet is sent to</param>
        /// <param name="clientFrom">Session of the character that moved</param>
        public void SendMovedCharacters(GameSession clientTo, GameSession clientFrom)
        {
            MovedCharacterModel movedCharactersModel = new MovedCharacterModel
            {
                SessionGameId = clientFrom.Pc.UniqueId,
                // The running speed of the character: the same value the client already got in 5101,
                // 5103 and 5147, so the neighbours interpolate the move exactly as the mover does
                MoveRate = clientFrom.Pc.Detail.MoveRate,
                Position = clientFrom.Pc.PositionCur,
                DirectionSight = clientFrom.Pc.DirectionSight,
                Action = clientFrom.Pc.Action
            };

            // TODO: Flag stays at zero and the flag the client sends in 5188 is thrown away by the
            // handler. What the byte means is unknown - walking against running, a move of a mount,
            // a move the client already started - and until it is read off a live client both sides
            // of the pair are left alone: the neighbours currently see every move the same way

            clientTo.Send(movedCharactersModel);
        }

        /// <summary>
        ///     5326: a refused move, the character is put back to the position the server holds.
        ///     Unlike 5189 this packet is not hidden from the character that asked to move -
        ///     stopping it is the whole point - so the caller sends it to the neighbours and to
        ///     the mover itself
        /// </summary>
        /// <param name="clientTo">Session the packet is sent to</param>
        /// <param name="clientFrom">Session of the character that was stopped</param>
        /// <param name="flag">
        ///     Zero for a refusal the move check marked as a full rollback, one for every other
        ///     refusal - the one the initiator gets 5103 after
        /// </param>
        public void SendStopMoveCharacter(GameSession clientTo, GameSession clientFrom, byte flag)
        {
            StopMoveCharacterModel stopMoveCharacterModel = new StopMoveCharacterModel
            {
                SessionGameId = clientFrom.Pc.UniqueId,
                Position = clientFrom.Pc.PositionCur,
                Flag = flag
            };

            clientTo.Send(stopMoveCharacterModel);
        }

        public void SendJumpCharacter(GameSession clientTo, GameSession clientFrom)
        {
            JumpEndCharacterModel jumpEndCharactersModel = new JumpEndCharacterModel
            {
                SessionGameId = clientFrom.Pc.UniqueId,
                DirectionSight = clientFrom.Pc.DirectionSight,
                Action = clientFrom.Pc.Action
            };

            clientTo.Send(jumpEndCharactersModel);
        }

        public void SendDirectionCharacter(GameSession clientTo, GameSession clientFrom)
        {
            CharDirectionModel charDirectionModel = new CharDirectionModel
            {
                SessionGameId = clientFrom.Pc.UniqueId,
                DirectionSight = clientFrom.Pc.DirectionSight
            };

            clientTo.Send(charDirectionModel);
        }

        public void SendRespawnCharacter(GameSession clientTo, GameSession clientFrom)
        {
            RespawnAckModel respawnAckModel = new RespawnAckModel
            {
                SessionGameId = clientFrom.Pc.UniqueId,
                Position = clientFrom.Pc.PositionCur
            };

            clientTo.Send(respawnAckModel);
        }
    }
}

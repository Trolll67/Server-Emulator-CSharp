using Packets.Server.Game.Models.Send.Attack;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Network;
using System;

namespace Server.Game.Core.Systems
{
    /// <summary>
    ///     Death of a character killed by a monster. The death of a player goes out with the very same
    ///     packet the death of a monster does (5137), so the client draws both of them the same way -
    ///     only the roles in it are the other way round: the monster is the offense and the character
    ///     is the defense.
    ///     <para>
    ///     The system owns the death itself and nothing else: the caller is the one that took the hp
    ///     off the character and told it about the new hp, so the whole death of a character reads as
    ///     5132 (the swing) - 5146 (the hp that reached zero) - 5137 (this system). The hp is neither
    ///     read nor written here on purpose - it belongs to the caller alone, which is the single
    ///     thread that swings for the monsters.
    ///     </para>
    ///     <para>
    ///     The auto attack of the killed character is not stopped here either: the swing pass takes it
    ///     down itself as soon as it sees a DeadTime on the attacker. Raising the character back is the
    ///     business of its own request (5141), and the only thing it waits for is the DeadTime this
    ///     system writes
    ///     </para>
    /// </summary>
    public class PlayerDeathSystem
    {
        private readonly IAttackFactory _attackFactory;

        public PlayerDeathSystem(IAttackFactory attackFactory)
        {
            _attackFactory = attackFactory;
        }

        /// <summary>
        ///     Kill the character of the session: the death is fixed once and everybody who sees the
        ///     character is told about it
        /// </summary>
        /// <param name="victim">Session in the world whose character was brought to zero hp</param>
        /// <param name="killer">Monster whose swing has killed the character</param>
        public void KillPlayer(GameSession victim, GMonster killer)
        {
            // The guard goes before the write and before every send: a character that already carries
            // a death time was killed by somebody else, and a second death would send the client one
            // more 5137 for a character that is lying dead already - the client answers such a death
            // with its own death screen and would show it twice
            if (victim.Pc.DeadTime != null)
            {
                return;
            }

            // Being dead is what the death time means for a character: nothing counts the time down
            // to a resurrection (a character is raised by its own request and not by a pass), so the
            // moment itself is only written to make the flag readable
            victim.Pc.DeadTime = DateTime.Now;

            // The reputation fields of 5137 belong to the killer, and a monster has none of them: it
            // carries no kill counter (the zero the packet is built with) and no chaotic reputation,
            // so the status goes out as the plain one
            _attackFactory.SendDeadAttack(victim, killer.UniqueId, victim.Pc.UniqueId, 0, ChaoticStatusType.Normal);

            // The neighbours keep the character standing until they are told it died
            foreach (var visibleCharacterGame in victim.Pc.VisibleCharacterGames)
            {
                _attackFactory.SendDeadAttack(visibleCharacterGame, killer.UniqueId, victim.Pc.UniqueId, 0, ChaoticStatusType.Normal);
            }
        }
    }
}

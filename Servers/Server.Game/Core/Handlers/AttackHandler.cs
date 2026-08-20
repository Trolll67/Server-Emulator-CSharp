using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Receive.Attack;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Network;
using Server.Game.Services;
using System;

namespace Server.Game.Core.Handlers
{
    [Handler]
    public class AttackHandler : IAttackHandler
    {
        private readonly IdentificationService _identificationService;

        public AttackHandler(IdentificationService identificationService)
        {
            _identificationService = identificationService;
        }

        /// <summary>
        ///     Start of an attack: the client asks to hit the target it picked and keeps the request
        ///     to itself until the server answers, so all the packet does here is put the attack
        ///     state on the character. The swings themselves are played by the attack service on its
        ///     own tick, this handler sends nothing back
        /// </summary>
        /// <param name="client">Session that asked to attack</param>
        /// <param name="model">Target, kind of the attack and the position the client attacks from</param>
        [HandlerAction(PacketType.AttackReq)]
        public void BeginAttack(GameSession client, AttackReqModel model)
        {
            // An attack of a session that is not in the world: the character is either not chosen
            // yet or already on its way out, so there is nobody to attack with
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            // A dead character does not attack: the client of the killed one keeps sending its auto
            // attack until it gets the death of its own character drawn
            if (client.Pc.DeadTime != null)
            {
                return;
            }

            // The target is taken by the identifier the client sent: an identifier that belongs to
            // nobody is a target the client is late with - it disappeared on our side already
            if (!_identificationService.CheckConnectionOrUnitByUniqueIdentifier(model.TargetSessionGameId))
            {
                return;
            }

            // Only units are attacked in this phase. A request against another character means the
            // player attacks a player, and that is dropped without a word: PvP is not ported, and
            // an answer would make the client play an attack the server never carries on
            GMonster target = _identificationService.GetUnitByUniqueIdentifier(model.TargetSessionGameId);

            if (target == null)
            {
                return;
            }

            // A killed monster stays in the world as a corpse until the garbage pass takes it away,
            // so a client that was late with its request asks to hit a corpse
            if (target.DeadTime != null)
            {
                return;
            }

            // The distance to the target is not checked here on purpose: the client starts the
            // attack while it still walks up to the target, and the distance of every single swing
            // is checked by the attack service anyway

            // A request for the target that is being attacked already is a repeat of the auto
            // attack of the client and not a new attack: only the start of an attack and a real
            // change of the target move the timer of the swing
            bool isNewTarget = client.Pc.AttackedUniqueIdentifier == null
                || client.Pc.AttackedUniqueIdentifier.Id != model.TargetSessionGameId.Id;

            client.Pc.TargetUniqueId = model.TargetSessionGameId;
            client.Pc.AttackType = model.AttackType;
            client.Pc.AttackPosition = model.AttackPosition;
            client.Pc.AttackFlag = model.AttackFlag;

            if (isNewTarget)
            {
                // The first swing at a target is due at once - the attack service takes it on its
                // nearest tick and puts the next one AttackRate ahead. The timer of a running
                // attack is left where it stands on purpose: rewriting it on every request would
                // let a client that repeats 5133 in a loop get a swing on every tick of the
                // service and hit far faster than its attack rate allows
                client.Pc.AttackDateTime = DateTime.Now;
            }

            // Written last on purpose: this is the flag by which the attack tick and the move
            // handler tell an attacking character from an idle one. The swings tick in the thread
            // of the scheduler while the request comes from the network one, so everything the
            // swing reads has to be in place before the attack is marked as active - otherwise the
            // very first tick can meet the new flag with the target of the previous one.
            // A repeated request changes the target the same way, over the running attack
            client.Pc.AttackedUniqueIdentifier = model.TargetSessionGameId;
        }
    }
}

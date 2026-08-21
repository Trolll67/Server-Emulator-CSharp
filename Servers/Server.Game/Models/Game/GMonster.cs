using Database.DataModel.Enums;
using Database.DataModel.Models;
using Packets.Server.Game.Structures;
using Server.Game.Network;
using System;
using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     Monster of the world.
    ///     <para>
    ///     Current hp/mp are stored only in <see cref="GChar.Simple"/> (<c>Simple.Hp</c>, <c>Simple.Mp</c>),
    ///     their maximums - in <see cref="GChar.ParmMon"/> (<c>ParmMon.Hp</c>, <c>ParmMon.Mp</c>).
    ///     There is no separate hp/mp on the monster itself: everything that damages or heals
    ///     a monster works with <c>Simple</c>.
    ///     </para>
    ///     <para>
    ///     Death contract: a monster is dead while <see cref="GChar.DeadTime"/> is not null, alive while it is
    ///     null. Death is fixed once - by writing <c>DeadTime</c>, the corpse stays in the world and is removed
    ///     from <c>IdentificationService</c> by <c>GarbageGameService.GarbageUnits</c> (shortly before the
    ///     respawn or after <c>GameSetting.GarbageUnits</c>). Resurrection is done by
    ///     <c>UnitGameService.RespawnUnits</c>: <see cref="Respawn"/> milliseconds after <c>DeadTime</c> the
    ///     monster is removed from the identification service, restored by <see cref="_SetDefaultInfo"/>
    ///     (it resets <c>DeadTime</c>, hp and mp) and added back.
    ///     </para>
    /// </summary>
    public class GMonster : GChar
    {
        public GMonster()
        {
            VisibleCharacterGames = new List<GameSession>();
            VisibleItemGames = new List<GPublicItem>();
            VisibleUnitGames = new List<GMonster>();
        }

        /// <summary>
        ///     Hp in a packet for a monster whose bar the client must not draw
        /// </summary>
        public const short HpHidden = -1;

        public List<GDropGroup> DropGroup { get; set; }
        //FnlApi::CArrayEx<FnlApp::CGoods,10> __mStomach;
        //FnlApi::CArrayEx<CAgroHistory,4> __mAgroList;
        //FnlApi::CFlag<9, unsigned char> __mMonFlag;
        //unsigned int __mTickLastDie;

        public int Respawn { get; set; }
        public Vector3 PositionDefault { get; set; }
        public float DirectionSightDefault { get; set; }

        public List<GameSession> VisibleCharacterGames { get; set; }
        public List<GPublicItem> VisibleItemGames { get; set; }
        public List<GMonster> VisibleUnitGames { get; set; }

        /// <summary>
        ///     Hp of the monster the way the packets carry it: a percent of the full hp, not an absolute
        ///     value. The client draws the bar as fifteen segments of that percent, so an absolute hp would
        ///     keep the bar full until the monster drops under a hundred points - and a monster of the
        ///     first levels carries tens of thousands of them.
        ///     <para>
        ///     A monster whose parameter row keeps the bar off gets <see cref="HpHidden"/> instead of a
        ///     number - whatever its hp is, full or none. A monster that is already down and a monster with
        ///     a broken parm (the maximum is not positive) give 0.
        ///     </para>
        ///     <para>
        ///     The hp comes as an argument instead of being read from <c>Simple.Hp</c>: the swing pass
        ///     reports the value from before the hit, the display packets - the current one. The maximum is
        ///     always <c>ParmMon.Hp</c>, the same source <see cref="_SetDefaultInfo"/> fills the hp from
        ///     </para>
        /// </summary>
        /// <param name="hp">Hp to report</param>
        /// <returns>Percent of the full hp, 0 for a monster that is down, <see cref="HpHidden"/> for a hidden bar</returns>
        public short GetHpDisplayed(int hp)
        {
            if (!ParmMon.IsShowHp)
            {
                return HpHidden;
            }

            short maxHp = ParmMon.Hp;

            if (hp <= 0 || maxHp <= 0)
            {
                return 0;
            }

            // Hp above the maximum - an overheal or a parm changed under a living monster - draws the
            // same full bar, so it is held at a hundred and never leaves the range of a percent
            if (hp >= maxHp)
            {
                return 100;
            }

            return (short)(hp * 100 / maxHp);
        }

        /// <summary>
        ///     Build the monster ability from its Detail. Idempotent: the base call resets the ability
        ///     and everything here is an assignment, so a repeated call after a respawn does not accumulate
        /// </summary>
        public new void CalcAbility()
        {
            base.CalcAbility();

            Ability.DDv = Detail.DDv;
            Ability.MDv = Detail.MDv;
            Ability.DDD = 0;
            Ability.RDv = Detail.RDv;
            Ability.MPv = Detail.MPv;
            Ability.RPv = Detail.RPv;
            Ability.DHit = Detail.Hit;
            Ability.DPv = (short)(AddDPV + Detail.DPv);
        }

        /// <summary>
        ///     Bring the monster to its default state: alive (<c>DeadTime = null</c>), full hp/mp from the parm
        ///     and ability recalculated from the parm. Used both on world loading and on respawn,
        ///     so repeated calls must give the same result
        /// </summary>
        /// <param name="parmMon"></param>
        public new void _SetDefaultInfo(ParmMonster parmMon)
        {
            base._SetDefaultInfo(parmMon);

            DeadTime = null;

            Simple = new GPcSimple()
            {
                Class = ParmMon.Class,
                Hp = ParmMon.Hp,
                Mp = ParmMon.Mp,
                Exp = ParmMon.Exp,
                NickName = ParmMon.Nm
            };

            Simple.SetStomach(70);

            Detail.DDv = ParmMon.DDv;
            Detail.RDv = ParmMon.RDv;
            Detail.MDv = ParmMon.MDv;

            Detail.DPv = ParmMon.DPv;
            Detail.RPv = ParmMon.RPv;
            Detail.MPv = ParmMon.MPv;

            Detail.Hit = 0;
            Detail.MinD = ParmMon.MinD;
            Detail.MaxD = ParmMon.MaxD;

            Transformed(parmMon);

            // Detail is filled, so the ability can be built from it: defences, evasion, hit
            CalcAbility();
        }
    }
}

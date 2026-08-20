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

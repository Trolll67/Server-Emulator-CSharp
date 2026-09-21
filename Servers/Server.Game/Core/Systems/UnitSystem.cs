using Database.DataModel.Models;
using Packets.Server.Game.Structures;
using Server.Game.Models.Game;
using Server.Game.Services;
using Server.Game.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Core.Systems
{
    public class UnitSystem
    {
        private readonly ParmRepository _parmRepository;
        public UnitSystem(ParmRepository parmRepository)
        {
            _parmRepository = parmRepository;
        }

        /// <summary>
        ///     Get unit games
        /// </summary>
        /// <returns></returns>
        public List<GMonster> GetUnitGames()
        {
            Random random = new Random();
            var monsters = new List<GMonster>();
            var monsterSpots = _parmRepository.GetAllMonsterSpots();

            // Create all units
            foreach (var mSpot in monsterSpots)
            {
                // Get new unit, the mapping calls _SetDefaultInfo: full hp/mp from the parm
                var monster = _parmRepository.GetGMonsterById(mSpot.MonsterId);

                // Set general fields, the unit is born alive and waits for no respawn pass
                monster.IsVsibleFirst = true;
                monster.DeadTime = null;
                Vector3 pos;
                MonsterSpotGroup sGroup;
                if (mSpot.SpotGroup.Count > 0)
                {
                    var rndSpotGroup = random.Next(0, mSpot.SpotGroup.Count);
                    sGroup = mSpot.SpotGroup[rndSpotGroup];

                    // The columns of the spot are the axes as they are: mPosY is the height, the
                    // same way TblPcState keeps it and the same way it travels on the wire. The
                    // swap that used to be here put a horizontal into Y, and every piece of logic
                    // that holds the height steady - the walk of a monster, the scatter of the
                    // loot - was holding the wrong axis
                    pos = new Vector3((float)sGroup.PosX, (float)sGroup.PosY, (float)sGroup.PosZ);
                }
                else
                {
                    sGroup = mSpot.SpotGroup[0];
                    pos = new Vector3((float)sGroup.PosX, (float)sGroup.PosY, (float)sGroup.PosZ);
                }

                // Set default position for unit. The current position is a copy: the home point of
                // the spot must survive whatever moves the monster later
                monster.PositionDefault = pos;
                monster.PositionCur = new Vector3(pos);
                monster.DirectionSight = (float)mSpot.Dir;
                monster.DirectionSightDefault = (float)mSpot.Dir;
                monster.Respawn = mSpot.Tick * 1000;

                monsters.Add(monster);
            }

            return monsters;
        }

        //public UnitGameModel AddUnit(int unitId, UnitPositionModel unitPositionGameModel)
        //{
        //    //TODO Сделано для расстановки мобов в реал тайме
        //    // Get new unit
        //    UnitGameModel unitGame = _databaseBalanceService.GetUnitById(unitId);

        //    // Set general fields
        //    unitGame.IsVsibleFirst = true;
        //    unitGame.DeadTime = null;

        //    // Set drops for unit
        //    unitGame.Drops = _databaseBalanceService.GetUnitDropsById(unitId);

        //    // Set default position for unit
        //    unitGame.PositionDefault = new Vector3(unitPositionGameModel.X, unitPositionGameModel.Y, unitPositionGameModel.Z);
        //    unitGame.Position = unitGame.PositionDefault;
        //    unitGame.DirectionSightDefault = unitPositionGameModel.DirectionSight;
        //    unitGame.Respawn = unitPositionGameModel.Respawn;

        //    return unitGame;
        //}
    }
}

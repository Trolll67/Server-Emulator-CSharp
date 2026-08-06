using Database.DataModel.Enums;
using Database.Fnl.Account;
using Database.Fnl.Game;
using Packets.Server.Game.Structures;
using Server.Game.Models.Game;
using Server.Game.Services.Database;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Services
{
    /// <summary>
    ///     Database mapping service
    /// </summary>
    public class DBGameMappingService
    {
        private readonly ParmRepository _parmRepository;

        public DBGameMappingService(ParmRepository parmRepository)
        {
            _parmRepository = parmRepository;
        }

        #region Character mapping
        /// <summary>
        ///     Map character
        /// </summary>
        /// <param name="pc">Character game model to fill</param>
        /// <param name="pcNo">Character number, TblPc.mNo (UspListPc)</param>
        /// <param name="slot">Character slot, TblPc.mSlot (UspListPc)</param>
        /// <param name="detail">Character state (UspGetPcDetail)</param>
        /// <param name="items">Inventory rows (UspGetPcItem)</param>
        /// <param name="equips">Worn equipment rows (UspGetPcEquip)</param>
        /// <param name="parmMon">Base monster parm chosen by class</param>
        public void MapCharacter(GPc pc, int pcNo, byte slot, PcDetailRow detail,
            IReadOnlyList<PcItemRow> items, IReadOnlyList<PcEquipRow> equips, ParmMonster parmMon)
        {
            pc.Simple = new GPcSimple
            {
                PcNo = (uint)pcNo,
                NickName = detail.Nm,
                Slot = slot,
                Class = (PcClassEnum)detail.Class,
                Sex = detail.Sex,
                Head = detail.Head,
                Face = detail.Face,
                Body = detail.Body,
                Level = (ushort)detail.Level,
                Exp = (ulong)detail.Exp,
                Hp = detail.Hp,
                Mp = detail.Mp,
            };

            pc.BeginHp = pc.Simple.Hp;
            pc.BeginMp = pc.Simple.Mp;
            pc.AddHp = (short)detail.HpAdd;
            pc.AddMp = (short)detail.MpAdd;
            pc.PreventItemDrop = detail.IsPreventItemDrop;
            pc.Simple.SetStomach(detail.Stomach);

            pc.PositionCur = new Vector3(detail.PosX, detail.PosY, detail.PosZ);
            // TODO GUILD __int64 __fastcall CPc::__FetchPcBase(CPc *this, unsigned int pPcNo)

            pc.CalcAbility();
            pc.Detail = new GPcDetail();
            pc.Detail.SetChaotic(detail.Chaotic);
            pc._SetDefaultInfo(parmMon);

            foreach (var item in items)
            {
                var parmItem = _parmRepository.GetItemById(item.ItemNo);

                if (parmItem == null)
                    continue;

                var gItem = new GItem(parmItem);
                gItem.SerialNumber = (ulong)item.SerialNo;
                gItem.IsConfirm = item.IsConfirm;
                gItem.Status = (ItemStatusEnum)item.Status;
                gItem.Count = item.Cnt;
                gItem.UseCount = item.CntUse;
                gItem.ItemBind = (ItemBindTypeEnum)item.BindingType;
                gItem.Restore = item.RestoreCnt;
                gItem.Hole = item.HoleCount;

                pc.Inventory.Items.Add(gItem);
            }

            foreach (var itemEquip in equips)
            {
                // Worn items normally already live in the inventory result set; reuse that
                // instance so equip and inventory share it. Fall back to the parm template when
                // the inventory result set does not carry the worn item
                var item = pc.Inventory.Items.FirstOrDefault(x => x.SerialNumber == (ulong)itemEquip.SerialNo);
                if (item == null)
                {
                    var parmItem = _parmRepository.GetItemById(itemEquip.ItemNo);
                    if (parmItem == null)
                        continue;

                    item = new GItem(parmItem);
                    item.SerialNumber = (ulong)itemEquip.SerialNo;
                    item.ItemBind = (ItemBindTypeEnum)itemEquip.BindingType;
                }

                var equip = new GPcEquip()
                {
                    IsConfirm = item.IsConfirm ? 1 : 0,
                    IsEquip = true, // TODO хуйня какая-то, если мы берем экипировку, то почему есть bool IsEquip?
                    IsSeal = false,
                    SerialNo = item.SerialNumber,
                    Status = item.Status,
                    Item = item
                };

                pc.Equip.Add(equip);
            }

            //TODO Добавить в бд pc направление взгляда
        }
        #endregion

        #region Item mapping
        /// <summary>
        ///     Map item
        /// </summary>
        /// <param name="itemGame"></param>
        /// <param name="item"></param>
        //public void MapItem(GItem itemGame, ItemModel item)
        //{
        //    itemGame.Id = item.Id;
        //    itemGame.Id = item.ItemId;

        //    itemGame.EquipPos = item.Position;
        //    itemGame.Count = item.Count;

        //    itemGame.IsConfirm = item.Flag;
        //    itemGame.EndTick = item.EndTick;
        //    itemGame.Status = item.ItemStatus;
        //    itemGame.UseCount = item.UseCount;
        //    itemGame.EatTime = item.EatTime;
        //    itemGame.TermOfValidity = item.TermOfEffectivity;
        //    itemGame.ItemBind = item.ItemBind;
        //    itemGame.Restore = item.Restore;
        //    itemGame.Hole = item.Hole;
        //}
        #endregion

        #region Session mapping
        /// <summary>
        ///     Map session. The world server no longer reads a Sessions table; the session key is
        ///     verified through dbo.UspLoginUser, so the domain session carries its result
        /// </summary>
        /// <param name="sessionGame">Session game model to fill</param>
        /// <param name="loginResult">Result of dbo.UspLoginUser (T2/T8)</param>
        /// <param name="userNo">Account number the key was checked for, TblUser.mUserNo</param>
        /// <param name="serverId">World this game server owns, from GameSetting</param>
        public void MapSession(GSession sessionGame, LoginUserResult loginResult, int userNo, int? serverId)
        {
            // GSession currently exposes only AccountId/ServerId/InGame; UserAuth/UserId from
            // loginResult have no domain field yet (would need a GSession change, out of T7 scope)
            sessionGame.Id = userNo;
            sessionGame.AccountId = userNo;
            sessionGame.ServerId = serverId;
            sessionGame.InGame = loginResult.IsSuccess;
        }
        #endregion
    }
}

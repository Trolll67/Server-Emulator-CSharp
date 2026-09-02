using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DataModel.Enums
{
    public enum ErrorEnum
    {
        CharAlreadyDie,
        CharParalyzed,
        CharSwoon,
        ItemNotExist,
        NoSeizuredItem,
        PosInvalid,
        ItemNotUseStateOrPos,
        ItemCantEquipLimitClass,
        ItemCantEquipLimitLevel,
        ItemEquipped,
        ItemNotEquipSlot,
        ItemNotEquip,
        CantEquipSlot,
        CantEquipWhenSlotBreak,
        NoSealSlot,
        ItemCantFindBow,
        ItemRestrictArrow,
        ItemCantEquipSpear,
        ItemCantEquipShield,
        ItemNotEquipRideRing,
        ItemExpired,
        IsNotOwner,
        ServantCallInvalid,
        ServantCallNotEnoughEnergy,
        ItemIsCurse,
        NotImplemented,
    }
}

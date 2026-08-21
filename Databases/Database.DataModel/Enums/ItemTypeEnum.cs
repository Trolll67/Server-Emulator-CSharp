namespace Database.DataModel.Enums
{
    /// <summary>
    ///     Item type. The numbers are the ones the item table gives, so they are read off a parm
    ///     row as they are and must not be renumbered
    /// </summary>
    public enum ItemTypeEnum
    {
        /// <summary>
        ///     No type of its own. The balance site also asks with it for items of every type
        /// </summary>
        All = 0x0,
        /// <summary>
        ///     Melee weapon, the usual one
        /// </summary>
        Weapon = 0x1,
        Shield = 0x2,
        Armor = 0x3,
        Ring = 0x4,
        Amulet = 0x5,
        Boot = 0x6,
        Glove = 0x7,
        Cap = 0x8,
        Belt = 0x9,
        Potion = 0xA,
        Gold = 0xB,
        Book = 0xC,
        Stick = 0xD,
        Food = 0xE,
        Event = 0xF,
        Etc = 0x10,
        Cloak = 0x11,
        /// <summary>
        ///     Range weapon, a bow. The only type that puts a swing on the range way
        /// </summary>
        Bow = 0x12,
        /// <summary>
        ///     Ammunition of a bow. Goes to the hand a shield goes to, not to the hand of the
        ///     weapon
        /// </summary>
        Arrow = 0x13,
        /// <summary>
        ///     Spear or halberd. A melee weapon with a reach of its own, worn in the weapon slot
        /// </summary>
        Spear = 0x14,
        Package = 0x15,
        ExpertnessMaterial = 0x16,
        SoulMaterial = 0x17,
        DefenseMaterial = 0x18,
        AttackMaterial = 0x19,
        LifeMaterial = 0x1A,
        EventAMaterial = 0x1B,
        EventBMaterial = 0x1C,
        EventCMaterial = 0x1D,
        AchieveCoin = 0x1E,
        AchieveTrophy = 0x1F,
        AchieveCoinPocket = 0x20,
        Servant = 0x21,
        ServantScroll = 0x22,
        ServantEtc = 0x23,
        ServantPotion = 0x24,

        // 0x25 closes the list of types an item can carry. What follows is ours: the balance site
        // groups items by these, no item row ever holds such a number
        Scrolls = 0x26,
        Premium = 0x27,
        AllEquip = 0x28,
        AllUsable = 0x29
    }
}

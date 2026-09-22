namespace Packets.Server.Game.Enums
{
    /// <summary>
    ///     ESCRIPT_ACTION of the original: what the client asks of the keeper it is talking to.
    ///     Everything but <see cref="PROC"/> is answered straight from the data of the server -
    ///     only PROC runs the script of the NPC
    /// </summary>
    public enum ScriptAction
    {
        PROC = 0,
        OPENSHOP_BUY = 1,
        OPENSHOP_SELL = 2,
        OPENSHOP_CHARGE = 3,
        OPENSTORE_PUSH = 4,
        OPENSTORE_POP = 5,
        OPENBOARD = 6,
        CASTLE_MENU = 7,
        OPENGUILDSTORE = 8,
        OPEN_CHAOSBATTLE_RANKING = 9,
        OPEN_CHAOSBATTLE_SVRINFO = 10,
        OPEN_UTGW_HEROES_BATTLE_INF = 11,
        OPEN_CONSIGNMENT_SHOP = 12,
        OPEN_RUNE_SYSTEM_UI = 13,
        OPEN_TEAM_RANK_INF = 14,
        SET_PASSWORD_STORE = 15,
        RESET_PASSWORD_STORE = 16
    }
}

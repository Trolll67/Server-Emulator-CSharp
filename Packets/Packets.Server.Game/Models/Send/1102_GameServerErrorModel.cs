using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send
{
    /// <summary>
    ///     Model serevr error
    /// </summary>
    [Model(PacketType.GameServerError)]
    public class GameServerErrorModel
    {
        /// <summary>
        ///     Opcode of the request that was refused, echoed back: the client tells which of the
        ///     requests it has sent got the refusal by this and by nothing else
        /// </summary>
        public PacketType PacketType { get; set; }

        /// <summary>
        ///     Reason of the refusal
        /// </summary>
        public GameServerErrorType ErrorType { get; set; }

        /// <summary>
        ///     Eight bytes of the request the refusal is about: the original puts the identifier of
        ///     the item there when it refuses to put a thing on. A request that carries nothing to
        ///     give back leaves the field at zero
        /// </summary>
        public ulong Etc { get; set; }

        /// <summary>
        ///     Whether the client shows the reason in a window of its own instead of a line in the
        ///     system chat
        /// </summary>
        public bool IsMsgBox { get; set; }
    }

    /// <summary>
    ///     Reason a request was refused. The original names its reasons ("eErrNoItemTooHeavy" and
    ///     the like) and sends the CRC-32 of the name; the client counts the very same number over
    ///     the names it knows and looks the text up by it. That is why the name is what is kept
    ///     here and the number is counted off it: a number nobody has a name for shows the player
    ///     nothing at all, and a new name works the moment it is written down
    /// </summary>
    public sealed class GameServerErrorType
    {
        /// <summary>
        ///     Table of the CRC-32 polynomial, built once for the whole run. It stands before the
        ///     reasons on purpose: static fields are built in the order they are written, and every
        ///     reason below counts its number while it is being built
        /// </summary>
        private static readonly uint[] CrcTable = BuildCrcTable();

        // Login, the character screen and the server list
        public static readonly GameServerErrorType NoUserNotLogin = new GameServerErrorType("eErrNoUserNotLogin");
        public static readonly GameServerErrorType NoUserChkAlreadyLogined = new GameServerErrorType("eErrNoUserChkAlreadyLogined");
        public static readonly GameServerErrorType NoCharInvalidSlot = new GameServerErrorType("eErrNoCharInvalidSlot");
        public static readonly GameServerErrorType NoUserCharSlotBusy = new GameServerErrorType("eErrNoUserCharSlotBusy");
        public static readonly GameServerErrorType NoCharAlreadyExistNm = new GameServerErrorType("eErrNoCharAlreadyExistNm");
        public static readonly GameServerErrorType NoCharCannotDel = new GameServerErrorType("eErrNoCharCannotDel");
        public static readonly GameServerErrorType NoCharInvalidNo = new GameServerErrorType("eErrNoCharInvalidNo");
        public static readonly GameServerErrorType NoSvrInvalidNo = new GameServerErrorType("eErrNoSvrInvalidNo");
        public static readonly GameServerErrorType HackerDetected = new GameServerErrorType("eErrNoHackerDetected");

        // The character is in no state to do anything with a thing
        public static readonly GameServerErrorType CharAlreadyDie = new GameServerErrorType("eErrNoCharAlreadyDie");
        public static readonly GameServerErrorType DistIsOut = new GameServerErrorType("eErrNoDistIsOut");

        // Picking a thing up and taking it into the bag
        public static readonly GameServerErrorType ItemInvalid = new GameServerErrorType("eErrNoItemInvalid");
        public static readonly GameServerErrorType ItemInvalidCnt = new GameServerErrorType("eErrNoItemInvalidCnt");
        public static readonly GameServerErrorType InvFull = new GameServerErrorType("eErrNoInvFull");
        public static readonly GameServerErrorType NoItemTooHeavy = new GameServerErrorType("eErrNoItemTooHeavy");
        public static readonly GameServerErrorType ItemTooMany = new GameServerErrorType("eErrNoItemTooMany");
        public static readonly GameServerErrorType ItemTooManyStackCnt = new GameServerErrorType("eErrNoItemTooManyStackCnt");
        public static readonly GameServerErrorType BindItemCantMove = new GameServerErrorType("eErrNoBindItemCantMove");
        public static readonly GameServerErrorType SqlInternalError = new GameServerErrorType("eErrNoSqlInternalError");

        // Taking a thing out of the bag and throwing it away
        public static readonly GameServerErrorType ItemNotExist = new GameServerErrorType("eErrNoItemNotExist");
        public static readonly GameServerErrorType ItemLack = new GameServerErrorType("eErrNoItemLack");
        public static readonly GameServerErrorType ItemEquipped = new GameServerErrorType("eErrNoItemEquipped");

        // Putting a thing on and taking it off
        public static readonly GameServerErrorType ItemCantEquipLimitClass = new GameServerErrorType("eErrNoItemCantEquipLimitClass");
        public static readonly GameServerErrorType ItemCantEquipLimitLevel = new GameServerErrorType("eErrNoItemCantEquipLimitLevel");
        public static readonly GameServerErrorType ItemNotEquipSlot = new GameServerErrorType("eErrNoItemNotEquipSlot");
        public static readonly GameServerErrorType ItemCantFindBow = new GameServerErrorType("eErrNoItemCantFindBow");
        public static readonly GameServerErrorType ItemCantEquipSpear = new GameServerErrorType("eErrNoItemCantEquipSpear");
        public static readonly GameServerErrorType ItemCantEquipShield = new GameServerErrorType("eErrNoItemCantEquipShield");
        public static readonly GameServerErrorType ItemNotEquip = new GameServerErrorType("eErrNoItemNotEquip");

        // Beads and servants
        public static readonly GameServerErrorType NoBeadHoleFull = new GameServerErrorType("eErrNoBeadHoleFull");
        public static readonly GameServerErrorType ServantEvolutionInvalid = new GameServerErrorType("eErrNoServantEvolutionInvalid");

        /// <summary>
        ///     Unknown error of the consignment subsystem. It is not a catch-all: everything sent
        ///     under it is read by the player as a trouble of the trade, so a place that refuses
        ///     for a reason of its own has to name that reason instead
        /// </summary>
        public static readonly GameServerErrorType CnsmUnknownError = new GameServerErrorType("eErrNoCnsmUnknownError");

        /// <summary>
        ///     The same reason under the name it was known by here before it was read back out of
        ///     its number. Kept only so that the places that still write it keep building - they
        ///     belong to phases of their own and have to move to the name above
        /// </summary>
        public static readonly GameServerErrorType UnknownError = CnsmUnknownError;

        private GameServerErrorType(string name)
        {
            Name = name;
            Code = MakeHashKey(name);
        }

        /// <summary>
        ///     Name of the reason, spelled the way the original spells it
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Number of the name, the one that goes out in the packet
        /// </summary>
        public uint Code { get; }

        /// <summary>
        ///     Number of a name: the standard CRC-32 over its ASCII bytes - the polynomial
        ///     0xEDB88320, all ones to start with and the inversion at the end. Both sides count it
        ///     the same way, so no agreed table of numbers is needed anywhere. Control vector of
        ///     the count: "123456789" gives 0xCBF43926
        /// </summary>
        /// <param name="name">Name of the reason</param>
        public static uint MakeHashKey(string name)
        {
            uint crc = 0xFFFFFFFFu;

            foreach (char symbol in name)
            {
                crc = CrcTable[(crc ^ (byte)symbol) & 0xFF] ^ (crc >> 8);
            }

            return ~crc;
        }

        public override string ToString()
        {
            return Name;
        }

        private static uint[] BuildCrcTable()
        {
            uint[] table = new uint[256];

            for (uint i = 0; i < table.Length; i++)
            {
                uint value = i;

                for (int bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
                }

                table[i] = value;
            }

            return table;
        }
    }
}

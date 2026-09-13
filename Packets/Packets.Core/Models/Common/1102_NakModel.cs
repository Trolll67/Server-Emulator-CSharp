using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Core.Models.Common
{
    /// <summary>
    ///     Model serevr error
    /// </summary>
    [Model(PacketType.Nak)]
    public class NakModel
    {
        /// <summary>
        ///     Opcode of the request that was refused, echoed back: the client tells which of the
        ///     requests it has sent got the refusal by this and by nothing else
        /// </summary>
        public PacketType PacketType { get; set; }

        /// <summary>
        ///     Reason of the refusal
        /// </summary>
        public NakErrorType ErrorType { get; set; }

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
    public sealed class NakErrorType
    {
        /// <summary>
        ///     Table of the CRC-32 polynomial, built once for the whole run. It stands before the
        ///     reasons on purpose: static fields are built in the order they are written, and every
        ///     reason below counts its number while it is being built
        /// </summary>
        private static readonly uint[] CrcTable = BuildCrcTable();

        /// <summary>
        ///     Group the client keeps the texts of the reasons of the server in. It only matters
        ///     where a packet carries the group next to the reason, which the kick does
        /// </summary>
        private const int ServerMessageGroup = 10004;

        // Login, the character screen and the server list
        public static readonly NakErrorType NoUserNotLogin = new NakErrorType("eErrNoUserNotLogin");
        public static readonly NakErrorType NoUserAlreadyLogined = new NakErrorType("eErrNoUserAlreadyLogined");
        public static readonly NakErrorType NoUserNotExistId = new NakErrorType("eErrNoUserNotExistId3");
        public static readonly NakErrorType NoUserDiffPswd = new NakErrorType("eErrNoUserDiffPswd");
        public static readonly NakErrorType NoUserLoginAnother = new NakErrorType("eErrNoUserLoginAnother", ServerMessageGroup);
        public static readonly NakErrorType VerInvalid = new NakErrorType("eErrNoVerInvalid");
        public static readonly NakErrorType IpBlocked = new NakErrorType("eErrNoIpBlocked");
        public static readonly NakErrorType FamilyNot = new NakErrorType("eErrNoFamilyNot");
        public static readonly NakErrorType TrBrokenIntegrity = new NakErrorType("eErrNoTrBrokenIntegrity");
        public static readonly NakErrorType UserInvalidId = new NakErrorType("eErrNoUserInvalidId");
        public static readonly NakErrorType NoUserChkAlreadyLogined = new NakErrorType("eErrNoUserChkAlreadyLogined", ServerMessageGroup);
        public static readonly NakErrorType NoCharInvalidSlot = new NakErrorType("eErrNoCharInvalidSlot");
        public static readonly NakErrorType NoUserCharSlotBusy = new NakErrorType("eErrNoUserCharSlotBusy");
        public static readonly NakErrorType NoCharAlreadyExistNm = new NakErrorType("eErrNoCharAlreadyExistNm");
        public static readonly NakErrorType NoCharCannotDel = new NakErrorType("eErrNoCharCannotDel");
        public static readonly NakErrorType NoCharInvalidNo = new NakErrorType("eErrNoCharInvalidNo");
        public static readonly NakErrorType NoSvrInvalidNo = new NakErrorType("eErrNoSvrInvalidNo");
        public static readonly NakErrorType HackerDetected = new NakErrorType("eErrNoHackerDetected");

        // The character is in no state to do anything with a thing
        public static readonly NakErrorType CharAlreadyDie = new NakErrorType("eErrNoCharAlreadyDie");
        public static readonly NakErrorType DistIsOut = new NakErrorType("eErrNoDistIsOut");

        // Picking a thing up and taking it into the bag
        public static readonly NakErrorType ItemInvalid = new NakErrorType("eErrNoItemInvalid");
        public static readonly NakErrorType ItemInvalidCnt = new NakErrorType("eErrNoItemInvalidCnt");
        public static readonly NakErrorType InvFull = new NakErrorType("eErrNoInvFull");
        public static readonly NakErrorType NoItemTooHeavy = new NakErrorType("eErrNoItemTooHeavy");
        public static readonly NakErrorType ItemTooMany = new NakErrorType("eErrNoItemTooMany");
        public static readonly NakErrorType ItemTooManyStackCnt = new NakErrorType("eErrNoItemTooManyStackCnt");
        public static readonly NakErrorType BindItemCantMove = new NakErrorType("eErrNoBindItemCantMove");
        public static readonly NakErrorType SqlInternalError = new NakErrorType("eErrNoSqlInternalError");

        // Taking a thing out of the bag and throwing it away
        public static readonly NakErrorType ItemNotExist = new NakErrorType("eErrNoItemNotExist");
        public static readonly NakErrorType ItemLack = new NakErrorType("eErrNoItemLack");
        public static readonly NakErrorType ItemEquipped = new NakErrorType("eErrNoItemEquipped");

        // Putting a thing on and taking it off
        public static readonly NakErrorType ItemCantEquipLimitClass = new NakErrorType("eErrNoItemCantEquipLimitClass");
        public static readonly NakErrorType ItemCantEquipLimitLevel = new NakErrorType("eErrNoItemCantEquipLimitLevel");
        public static readonly NakErrorType ItemNotEquipSlot = new NakErrorType("eErrNoItemNotEquipSlot");
        public static readonly NakErrorType ItemCantFindBow = new NakErrorType("eErrNoItemCantFindBow");
        public static readonly NakErrorType ItemCantEquipSpear = new NakErrorType("eErrNoItemCantEquipSpear");
        public static readonly NakErrorType ItemCantEquipShield = new NakErrorType("eErrNoItemCantEquipShield");
        public static readonly NakErrorType ItemNotEquip = new NakErrorType("eErrNoItemNotEquip");

        // Beads and servants
        public static readonly NakErrorType NoBeadHoleFull = new NakErrorType("eErrNoBeadHoleFull");
        public static readonly NakErrorType ServantEvolutionInvalid = new NakErrorType("eErrNoServantEvolutionInvalid");

        /// <summary>
        ///     Unknown error of the consignment subsystem. It is not a catch-all: everything sent
        ///     under it is read by the player as a trouble of the trade, so a place that refuses
        ///     for a reason of its own has to name that reason instead
        /// </summary>
        public static readonly NakErrorType CnsmUnknownError = new NakErrorType("eErrNoCnsmUnknownError");

        /// <summary>
        ///     The same reason under the name it was known by here before it was read back out of
        ///     its number. Kept only so that the places that still write it keep building - they
        ///     belong to phases of their own and have to move to the name above
        /// </summary>
        public static readonly NakErrorType UnknownError = CnsmUnknownError;

        private NakErrorType(string name, int msgGroup = 0)
        {
            Name = name;
            Code = MakeHashKey(name);
            MsgGroup = msgGroup;
        }

        /// <summary>
        ///     Reason under a name that is not written down here. The stored procedures of the
        ///     original name their own reasons and the channel sends the number of whatever name
        ///     came back, without knowing the name itself - so a reason read out of the database
        ///     travels the same way and the client finds the text for it
        /// </summary>
        /// <param name="name">Name of the reason, spelled the way the original spells it</param>
        public static NakErrorType FromName(string name)
        {
            return new NakErrorType(name);
        }

        /// <summary>
        ///     Reason that came in a packet, where the number travels instead of the name. The
        ///     name is not in the packet and is not needed: everything that is done with it here
        ///     is sending it on and writing it down
        /// </summary>
        /// <param name="code">Number of the reason</param>
        /// <param name="msgGroup">Group of texts the client looks it up in</param>
        public static NakErrorType FromCode(uint code, int msgGroup)
        {
            return new NakErrorType(code, msgGroup);
        }

        private NakErrorType(uint code, int msgGroup)
        {
            Name = code.ToString();
            Code = code;
            MsgGroup = msgGroup;
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
        ///     Group of texts the client looks the reason up in. Zero for every reason that
        ///     travels without a group, which is all of them but the ones of the kick
        /// </summary>
        public int MsgGroup { get; }

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

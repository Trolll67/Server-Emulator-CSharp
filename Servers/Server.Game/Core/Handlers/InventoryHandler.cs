using Database.Fnl.Game;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Models.Receive.Inventory;
using Packets.Server.Game.Models.Send;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Core.Systems;
using Server.Game.Models.Game;
using Server.Game.Models.Settings;
using Server.Game.Network;
using Server.Game.Services;
using System;
using System.Runtime.CompilerServices;

namespace Server.Game.Core.Handlers
{
    /// <summary>
    ///     Things that lie in the world and things that lie in the bag: 5177 takes one off the
    ///     ground, 5159 throws one away. Both requests walk the three steps the equipment walks
    ///     (see EquipHandler) and in the order of the original - the character checks the request
    ///     and builds a plan of the change, the row of the item is written by a stored procedure,
    ///     and only a stored change is applied to the model and told to the clients. A refusal at
    ///     any of the steps leaves the bag and the database exactly as they were.
    ///     A request comes in on the network thread of the session, and it is not the only writer
    ///     of the bag - the loot of a killed monster goes into the same bag off the thread of the
    ///     swings (UnitDropSystem.PutIntoBag). The three steps are therefore walked under the lock
    ///     of the bag of the character, the call of the database included, so that no second writer
    ///     builds its plan over a bag that is about to change under it; the lists the model
    ///     publishes are replaced by a single write of a reference, so the threads that only read
    ///     the bag take no lock at all and meet either the bag of before the change or the one
    ///     after it. The things of the world belong to nobody in particular and are taken under the
    ///     lock of the identification service - see IdentificationService.TryTakeItem
    /// </summary>
    [Handler]
    public class InventarHandler : IInventarHandler
    {
        /// <summary>
        ///     How often one session may pick a thing up and how often it may throw one away, in
        ///     milliseconds; values of the original. A request that comes earlier is dropped without
        ///     a word - the original answers nothing either, and a client that sends two requests on
        ///     one click has to see one answer and not two
        /// </summary>
        private const int PickUpIntervalMilliseconds = 500;
        private const int DropIntervalMilliseconds = 300;

        /// <summary>
        ///     Height between the character and the thing no reach covers, value of the original: a
        ///     thing at the foot of a cliff is not taken from the top of it. Counted apart from the
        ///     distance of the plane, the way the original counts it
        /// </summary>
        private const float CriticalHeight = 1150f;

        /// <summary>
        ///     Spread of a thing that is thrown away, values of the original: the offset of either
        ///     horizontal axis is drawn out of this range around the character, and a draw that
        ///     lands on the very spot of the character is pushed off it by the least offset. The
        ///     loot of a monster is scattered by the same values
        /// </summary>
        private const int DropItemRange = 50;
        private const int DropItemMinOffset = 20;

        /// <summary>
        ///     Code dbo.UspPushItem answers with when the binding of the item forbids the move, and
        ///     the one dbo.UspPopItem answers with for the same reason. Everything else the two
        ///     report is a failure of the write itself and goes back as an internal error of the
        ///     database - the original reads exactly these two codes and nothing more
        /// </summary>
        private const int PushItemBindErrNo = 11;
        private const int PopItemBindErrNo = 12;

        /// <summary>
        ///     Ticks of the last pick-up and of the last drop of every session that is being served.
        ///     The handler is built anew for every request, so the two counters cannot live in a
        ///     field of it; the table holds the sessions weakly, so a session that is gone takes its
        ///     counters with it and nothing has to clean up after it
        /// </summary>
        private static readonly ConditionalWeakTable<GameSession, RequestTicks> Requests = new ConditionalWeakTable<GameSession, RequestTicks>();

        /// <summary>
        ///     Draw of the spot a thrown away thing lands on. Random is not thread safe and the
        ///     sessions are served by the socket threads, so the generator is used under a lock
        /// </summary>
        private static readonly Random DropRandom = new Random();

        private readonly ILogger<InventarHandler> _logger;
        private readonly IFnlGameRepository _gameRepository;
        private readonly IdentificationService _identificationService;
        private readonly IOptions<GameSetting> _settings;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IInventoryFactory _inventoryFactory;
        private readonly IErrorFactory _errorFactory;

        public InventarHandler(ILogger<InventarHandler> logger, IFnlGameRepository gameRepository, IdentificationService identificationService,
                                IOptions<GameSetting> settings, ICharacteristicFactory characteristicFactory, IInventoryFactory inventoryFactory,
                                IErrorFactory errorFactory)
        {
            _logger = logger;
            _gameRepository = gameRepository;
            _identificationService = identificationService;
            _settings = settings;
            _characteristicFactory = characteristicFactory;
            _inventoryFactory = inventoryFactory;
            _errorFactory = errorFactory;
        }

        /// <summary>
        ///     5177: take a thing that lies in the world into the bag. The order of the steps is the
        ///     one of the original: the thing has to lie there, it has to be within reach, the
        ///     character has to have room for it, and only then the row of the item is created or
        ///     moved to the character. The thing is taken out of the world before the row is
        ///     written and put back when the write fails, so nobody ever ends up with a thing the
        ///     database knows nothing about
        /// </summary>
        /// <param name="client">Session that asked for the thing</param>
        /// <param name="itemPickUpModel">Identifier of the thing that lies in the world</param>
        [HandlerAction(PacketType.ItemPickupReq)]
        public void ItemPickUp(GameSession client, ItemPickupReqModel itemPickUpModel)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                _errorFactory.SendServerError(client, PacketType.ItemPickupReq, GameServerErrorType.NoUserNotLogin, false);
                return;
            }

            if (!AcceptPickUp(client))
            {
                return;
            }

            // The thing is only looked at here: it is taken out of the world further down, when
            // everything that may refuse the request has already had its say
            GPublicItem ground = _identificationService.GetItemByUniqueIdentifier(itemPickUpModel.UniqueIdentifierItem);

            if (ground == null)
            {
                _errorFactory.SendServerError(client, PacketType.ItemPickupReq, GameServerErrorType.ItemNotExist, false);
                return;
            }

            if (!IsInReach(client.Pc, ground.Position))
            {
                _errorFactory.SendServerError(client, PacketType.ItemPickupReq, GameServerErrorType.DistIsOut, false);
                return;
            }

            GInventoryChange change;
            GItem stored;

            // The plan of the bag, the row of the database and the change itself are one sequence
            // for one writer at a time: the loot of a killed monster reaches the same bag off the
            // thread of the swings, and a plan built over the bag of before that loot would publish
            // its list over the applied one and lose the loot. The lock of the identification
            // service is taken inside this one and never the other way round
            lock (client.Pc.InventoryLock)
            {
                // The whole pile goes into the bag: the original takes a thing of the ground whole
                // and has no request that takes a part of it
                change = client.Pc.PickUpItem(ground.Item, ground.Item.Count);

                if (!change.IsSuccess)
                {
                    _errorFactory.SendServerError(client, PacketType.ItemPickupReq, ToErrorType(change.Error), false);
                    return;
                }

                // Of two players that reached for the thing at the same moment only one gets here:
                // the other one finds nothing to take and is told the thing is not there any more
                if (!_identificationService.TryTakeItem(itemPickUpModel.UniqueIdentifierItem, out GPublicItem taken))
                {
                    _errorFactory.SendServerError(client, PacketType.ItemPickupReq, GameServerErrorType.ItemNotExist, false);
                    return;
                }

                // The plan was built over the thing that was looked at, and only that thing may be
                // taken by it: an identifier is handed out again once the thing that held it is
                // gone, and a plan carried over to another thing would put into the bag something
                // else than it was built for
                if (taken != ground)
                {
                    _identificationService.ReturnItem(taken);
                    _errorFactory.SendServerError(client, PacketType.ItemPickupReq, GameServerErrorType.ItemNotExist, false);
                    return;
                }

                if (!Store(client, taken.Item, change, out ulong serialNo))
                {
                    // The row of the item is not written: the thing goes back on the ground and
                    // the bag is left as it was. It goes back under the identifier it already has,
                    // so for everybody around nothing has happened at all - the original does the
                    // same by clearing the flag it set on the thing itself
                    _identificationService.ReturnItem(taken);
                    return;
                }

                // The procedure is the arbiter of the merge: it merges the stacks of the database
                // by conditions of its own and answers with the serial of the row the thing ended
                // up in, and the bag is brought over to that row whether it merged the thing or
                // took a row of its own for it - see GPc.ApplyInventoryChange
                // Страховка: перегрузка сейчас не отказывает; если откажет - база уже записана,
                // вещь не возвращаем. Вещь уже изъята из мира - освобождаем номер и завершаем без пакетов
                if (!client.Pc.ApplyInventoryChange(change, serialNo))
                {
                    _logger.LogError("Character {PcNo} could not apply inventory change for row {SerialNo} of item {ItemNo}, the row is already written", client.Pc.Simple.PcNo, serialNo, ground.Item.Id);
                    _identificationService.ReleaseItem(taken);
                    return;
                }

                // The thing is in the bag and is never going back on the ground: the number it lay
                // in the world under is free for the next thing that falls there
                _identificationService.ReleaseItem(taken);

                // The row of the bag as it is now, read by the serial of the procedure: whichever
                // way the two settled the merge, that serial names the row the thing went into
                stored = client.Pc.FindItem(serialNo) ?? change.Item;
            }

            // The packets go out with the lock let go: the bag already holds the change, and
            // nothing of what is sent reads the list any more.
            // The client draws what the row holds after the change - the whole stack when the thing
            // went into a row the bag already had
            _inventoryFactory.SendItemAdd(client, stored, client.Pc.UniqueId, Reason.Pickup);

            SendWeight(client, stored);

            // Nothing is sent about the thing leaving the world: the pass of the visibility tells
            // everybody around, the owner of the bag included
        }

        /// <summary>
        ///     5159: throw a part of a row of the bag away. The row is moved to the owner the
        ///     dropped things are parked under before the bag is touched at all, so a write that
        ///     fails leaves the character with everything it had - the original goes the other way
        ///     round and rolls its bag back by the opposite call
        /// </summary>
        /// <param name="client">Session that asked to throw the thing away</param>
        /// <param name="itemDropModel">Serial of the row of the bag and the count that leaves it</param>
        [HandlerAction(PacketType.ItemDropReq)]
        public void ItemDrop(GameSession client, ItemDropReqModel itemDropModel)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                _errorFactory.SendServerError(client, PacketType.ItemDropReq, GameServerErrorType.NoUserNotLogin, itemDropModel.SerialNumber, false);
                return;
            }

            if (!AcceptDrop(client))
            {
                return;
            }

            GInventoryChange change;
            ulong serialNo;

            // One writer of the bag at a time, the row of the database included - see the lock in
            // ItemPickUp
            lock (client.Pc.InventoryLock)
            {
                change = client.Pc.DropItem(itemDropModel.SerialNumber, (int)itemDropModel.Stack);

                if (!change.IsSuccess)
                {
                    _errorFactory.SendServerError(client, PacketType.ItemDropReq, ToErrorType(change.Error), itemDropModel.SerialNumber, false);
                    return;
                }

                if (!Store(client, change, out serialNo))
                {
                    return;
                }

                if (!client.Pc.ApplyInventoryChange(change))
                {
                    return;
                }
            }

            // The packet names the row the things left and the count that left it, so it is built
            // before the thing on the ground takes the serial the procedure issued to it: a stack
            // that was split lies on the ground under a serial of its own
            GItem removed = new GItem(change.Ground);
            removed.SerialNumber = change.Item.SerialNumber;

            GItem ground = change.Ground;
            ground.SerialNumber = serialNo;

            GPublicItem publicItem = new GPublicItem()
            {
                Item = ground,
                Position = GetDropPosition(client.Pc.PositionCur),
                IsVsibleFirst = true,
                DateCreate = DateTime.Now
            };

            _identificationService.AddItem(publicItem);

            _inventoryFactory.SendItemRemove(client, removed, Reason.Drop);

            SendWeight(client, ground);

            // The thing appearing in the world is told by the pass of the visibility
        }

        /// <summary>
        ///     5158: use a thing of the bag. Potions, food and everything else a thing does when it
        ///     is used is a mechanic of its own and is not written yet, so every request is refused
        ///     with the reason the original gives for a thing that is not there - a refusal the
        ///     client shows as a line of its own instead of leaving the player waiting
        /// </summary>
        /// <param name="client">Session that asked to use the thing</param>
        /// <param name="itemUseModel">Serial of the row of the bag</param>
        [HandlerAction(PacketType.ItemUseReq)]
        public void ItemUse(GameSession client, ItemUseReqModel itemUseModel)
        {
            // TODO: using a thing - the buffs it hands out, the cooldown, the stomach of the
            // character - is a phase of its own
            _errorFactory.SendServerError(client, PacketType.ItemUseReq, GameServerErrorType.ItemNotExist, itemUseModel.SerialNumber, false);
        }

        /// <summary>
        ///     Writes a thing that was taken out of the world into the bag of a character. A thing
        ///     that came off a monster carries no serial at all and the procedure creates a row for
        ///     it; a thing another player threw away carries the serial of the row that already
        ///     exists and the procedure moves that row to the new owner. Merging into a stack the
        ///     character already carries is the business of the procedure as well - it answers with
        ///     the serial of the row the thing ended up in, and that is the serial the bag has to
        ///     carry
        /// </summary>
        /// <param name="client">Session the thing goes to</param>
        /// <param name="ground">Thing as it lay in the world</param>
        /// <param name="change">Plan built by the character</param>
        /// <param name="serialNo">Serial of the row the thing ended up in</param>
        /// <returns>True when the row is written and the plan may be applied</returns>
        private bool Store(GameSession client, GItem ground, GInventoryChange change, out ulong serialNo)
        {
            serialNo = 0;

            PushItemRow row;

            try
            {
                row = _gameRepository.PushItem((int)client.Pc.Simple.PcNo, (long)ground.SerialNumber, ground.Id, ground.TermOfValidity,
                    change.Count, ground.UseCount, ground.IsConfirm, (byte)ground.Status, GPcInventory.IsStackable(ground),
                    ground.IsCharge, 0, (byte)ground.ItemBind, ground.Restore);
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                // FNLGame is down or the procedure failed: the bag is not touched and the thing goes
                // back to the world by the caller
                _logger.LogError(e, "Can not store item {ItemNo} picked up by character {PcNo}, the pick-up is dropped", ground.Id, client.Pc.Simple.PcNo);
                _errorFactory.SendServerError(client, PacketType.ItemPickupReq, GameServerErrorType.SqlInternalError, false);
                return false;
            }

            if (!row.IsSuccess)
            {
                _logger.LogError("Character {PcNo} could not be given item {ItemNo}, the procedure answered {ErrNo}", client.Pc.Simple.PcNo, ground.Id, row.ErrorCode);
                _errorFactory.SendServerError(client, PacketType.ItemPickupReq, ToErrorType(row.ErrorCode, PushItemBindErrNo), false);
                return false;
            }

            serialNo = (ulong)row.SerialNo;

            // TODO: the tick the item runs out at comes back with the row as the minutes left of
            // its lifetime, and the original writes it into the item. What our EndTick counts from
            // is not established, so the item keeps the tick it lay in the world with
            return true;
        }

        /// <summary>
        ///     Moves a row of the bag to the owner the things that lie in the world are parked
        ///     under. The whole row goes over when the whole stack is thrown away, and a part of it
        ///     is split off into a row of its own otherwise - either way the serial of the row that
        ///     ends up on the ground comes back from the procedure.
        ///     Written before the bag is touched: a write that fails leaves the character with the
        ///     row it had and nothing has to be rolled back
        /// </summary>
        /// <param name="client">Session the row belongs to</param>
        /// <param name="change">Plan built by the character</param>
        /// <param name="serialNo">Serial the thing lies in the world under</param>
        /// <returns>True when the row is moved and the plan may be applied</returns>
        private bool Store(GameSession client, GInventoryChange change, out ulong serialNo)
        {
            serialNo = 0;

            PopItemResult result;

            try
            {
                // The cached change of the count is the count that leaves the row, with a minus
                // sign; the taken part is not destroyed, it goes to the new owner
                result = _gameRepository.PopItem(FnlGameRepository.DroppedItemOwner, (long)change.Item.SerialNumber, change.Count,
                    -change.Count, false, GPcInventory.IsStackable(change.Item));
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                _logger.LogError(e, "Can not take item {SerialNo} out of the bag of character {PcNo}, the drop is dropped", change.Item.SerialNumber, client.Pc.Simple.PcNo);
                _errorFactory.SendServerError(client, PacketType.ItemDropReq, GameServerErrorType.SqlInternalError, change.Item.SerialNumber, false);
                return false;
            }

            if (!result.IsSuccess)
            {
                _logger.LogError("Character {PcNo} could not throw item {SerialNo} away, the procedure answered {ErrNo}", client.Pc.Simple.PcNo, change.Item.SerialNumber, result.ReturnCode);
                _errorFactory.SendServerError(client, PacketType.ItemDropReq, ToErrorType(result.ReturnCode, PopItemBindErrNo), change.Item.SerialNumber, false);
                return false;
            }

            serialNo = (ulong)result.SerialNoNew;

            return true;
        }

        /// <summary>
        ///     Tells the owner of the bag what it carries now. The original sends the weight of a
        ///     pick-up and of a drop only when the thing itself weighs something: money weighs
        ///     nothing and the packet stays out
        /// </summary>
        /// <param name="client">Session the bag belongs to</param>
        /// <param name="item">Thing that moved</param>
        private void SendWeight(GameSession client, GItem item)
        {
            if (item.Weight != 0)
            {
                _characteristicFactory.SendInfoWeight(client);
            }
        }

        /// <summary>
        ///     Whether a thing lies close enough to be taken. The reach is counted off the edge of
        ///     the body of the character and not off its middle, the way the reach of a swing is
        ///     counted (GChar.CalcDistAttack): the size of the body plus the reach of the setting,
        ///     which carries the sum of the reach of the original and the allowance it makes for
        ///     the difference between what the client draws and what the server holds.
        ///     The height is asked apart from the distance of the plane, exactly as the original
        ///     asks it - a thing straight below the character is out of reach without being far
        /// </summary>
        /// <param name="pc">Character that reaches for the thing</param>
        /// <param name="position">Spot the thing lies at</param>
        /// <returns>True when the thing may be taken</returns>
        private bool IsInReach(GPc pc, Vector3 position)
        {
            float reach = (pc.ParmMonCur == null ? 0 : pc.ParmMonCur.BodySz) + _settings.Value.ItemPickUpDistance;

            if (MoveSystem.GetDistance2DSq(pc.PositionCur, position) > reach * reach)
            {
                return false;
            }

            return Math.Abs(pc.PositionCur.Y - position.Y) < CriticalHeight;
        }

        /// <summary>
        ///     Spot a thing lands on beside the character that throws it away. The offset of either
        ///     horizontal axis is drawn out of one range around the character, and a draw that lands
        ///     on the very spot of the character is pushed off it by the least offset - all of it
        ///     the way the original draws it. The height is the one of the character: the server has
        ///     no ground under the thing to take another one from, and no check of the spot either
        /// </summary>
        /// <param name="position">Spot the character stands at</param>
        /// <returns>Spot the thing lies down at</returns>
        private static Vector3 GetDropPosition(Vector3 position)
        {
            int offsetX;
            int offsetZ;

            lock (DropRandom)
            {
                offsetX = DropItemRange / 2 - DropRandom.Next(DropItemRange);
                offsetZ = DropItemRange / 2 - DropRandom.Next(DropItemRange);
            }

            if (offsetX == 0 && offsetZ == 0)
            {
                offsetX = DropItemMinOffset;
            }

            return new Vector3(position.X + offsetX, position.Y, position.Z + offsetZ);
        }

        /// <summary>
        ///     Whether a pick-up of this session is served at all: the requests that come closer to
        ///     each other than the interval of the original are dropped silently
        /// </summary>
        /// <param name="client">Session that asked for a thing</param>
        /// <returns>True when the request is served</returns>
        private static bool AcceptPickUp(GameSession client)
        {
            RequestTicks ticks = Requests.GetOrCreateValue(client);
            int tick = Environment.TickCount;

            // Counted as an unsigned difference: the counter of the ticks runs over the width of
            // its field about once in seven weeks, and a difference read as a signed number would
            // let everything through for a moment there
            if ((uint)(tick - ticks.PickUpTick) < PickUpIntervalMilliseconds)
            {
                return false;
            }

            ticks.PickUpTick = tick;

            return true;
        }

        /// <summary>
        ///     Whether a drop of this session is served, see AcceptPickUp
        /// </summary>
        /// <param name="client">Session that asked to throw a thing away</param>
        /// <returns>True when the request is served</returns>
        private static bool AcceptDrop(GameSession client)
        {
            RequestTicks ticks = Requests.GetOrCreateValue(client);
            int tick = Environment.TickCount;

            if ((uint)(tick - ticks.DropTick) < DropIntervalMilliseconds)
            {
                return false;
            }

            ticks.DropTick = tick;

            return true;
        }

        /// <summary>
        ///     Reason of a refusal of the bag under the name the client knows it by. The names of
        ///     the model are the names of the original with the prefix of its constants dropped, so
        ///     the two lists meet one to one
        /// </summary>
        /// <param name="error">Reason the character refused the request for</param>
        /// <returns>Reason as it goes out in 1102</returns>
        private static GameServerErrorType ToErrorType(InventoryErrorEnum error)
        {
            switch (error)
            {
                case InventoryErrorEnum.CharAlreadyDie:
                    return GameServerErrorType.CharAlreadyDie;
                case InventoryErrorEnum.ItemInvalidCnt:
                    return GameServerErrorType.ItemInvalidCnt;
                case InventoryErrorEnum.InvFull:
                    return GameServerErrorType.InvFull;
                case InventoryErrorEnum.ItemTooManyStackCnt:
                    return GameServerErrorType.ItemTooManyStackCnt;
                case InventoryErrorEnum.ItemTooHeavy:
                    return GameServerErrorType.NoItemTooHeavy;
                case InventoryErrorEnum.ItemLack:
                    return GameServerErrorType.ItemLack;
                case InventoryErrorEnum.ItemEquipped:
                    return GameServerErrorType.ItemEquipped;
                default:
                    return GameServerErrorType.ItemNotExist;
            }
        }

        /// <summary>
        ///     Reason of a refusal of a stored procedure. The binding of an item that forbids the
        ///     move has a reason of its own, everything else the procedures report is a failure of
        ///     the write and is told as an internal error of the database - the original reads the
        ///     one code and nothing else
        /// </summary>
        /// <param name="errNo">Code the procedure answered with</param>
        /// <param name="bindErrNo">Code of that procedure for the binding of the item</param>
        /// <returns>Reason as it goes out in 1102</returns>
        private static GameServerErrorType ToErrorType(int errNo, int bindErrNo)
        {
            return errNo == bindErrNo ? GameServerErrorType.BindItemCantMove : GameServerErrorType.SqlInternalError;
        }

        /// <summary>
        ///     Ticks of the last pick-up and of the last drop of one session. Both start a whole
        ///     interval in the past, so the first request of a session is served whatever the
        ///     counter of the ticks stands at
        /// </summary>
        private sealed class RequestTicks
        {
            public int PickUpTick = Environment.TickCount - PickUpIntervalMilliseconds;
            public int DropTick = Environment.TickCount - DropIntervalMilliseconds;
        }
    }
}

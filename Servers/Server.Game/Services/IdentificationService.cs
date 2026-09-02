using Packets.Server.Game.Enums;
using Packets.Server.Game.Structures;
using Server.Game.Models.Game;
using Server.Game.Network;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Services
{
    /// <summary>
    ///     Identification service for working loading objects
    /// </summary>
    public class IdentificationService
    {
        /// <summary>
        ///     Lock object
        /// </summary>
        private object _lockObject = new object();

        /// <summary>
        ///     Unique identifiers unit
        /// </summary>
        private Queue<uint> _uniqueIdentifiers;

        /// <summary>
        ///     Unique identifiers counter
        /// </summary>
        private uint _uniqueIdentifiersCounter;

        /// <summary>
        ///     Generation of the last handout of every number that has ever been handed out. The
        ///     number itself is reused after the entity is gone, and the generation is the only
        ///     thing by which the client tells the new entity from the one it has seen under the
        ///     same number a moment ago, so it lives next to the handout and not in the entity.
        ///     A record outlives the entity on purpose: clearing it when the number is released
        ///     would make the next handout of that number look like the first one and undo the
        ///     whole point of the counter. The set of keys is bounded by the peak number of live
        ///     entities, because the numbers themselves are reused
        /// </summary>
        private Dictionary<uint, uint> _uniqueIdentifierGenerations;

        /// <summary>
        ///     Connections in the game
        /// </summary>
        private Dictionary<UniqueId, GameSession> _connections;

        /// <summary>
        ///     Items in the game
        /// </summary>
        private Dictionary<UniqueId, GPublicItem> _items;

        /// <summary>
        ///     Units in the game
        /// </summary>
        private Dictionary<UniqueId, GMonster> _units;

        /// <summary>
        ///     Method for loading
        /// </summary>
        public IdentificationService()
        {
            _uniqueIdentifiers = new Queue<uint>();
            _uniqueIdentifiersCounter = 1;
            _uniqueIdentifierGenerations = new Dictionary<uint, uint>();

            _connections = new Dictionary<UniqueId, GameSession>();
            _items = new Dictionary<UniqueId, GPublicItem>();
            _units = new Dictionary<UniqueId, GMonster>();
        }

        #region Connections
        /// <summary>
        ///     Add connection
        /// </summary>
        /// <param name="gameSession"></param>
        public void AddConnection(GameSession gameSession)
        {
            lock (_lockObject)
            {
                gameSession.Pc.UniqueId = CreateUniqueIdentifier(UniqueIdentifierType.Player);

                _connections.Add(gameSession.Pc.UniqueId, gameSession);
            }
        }

        /// <summary>
        ///     Remove connection
        /// </summary>
        /// <param name="gameSession"></param>
        public void RemoveConnection(GameSession gameSession)
        {
            lock (_lockObject)
            {
                var uniqueIdentifier = _connections.FirstOrDefault(c => c.Value == gameSession).Key;

                if (uniqueIdentifier != null)
                {
                    RemoveUniqueIdentifier(uniqueIdentifier.Id);
                    _connections.Remove(uniqueIdentifier);
                }
            }
        }

        /// <summary>
        ///     Get all connections
        /// </summary>
        /// <returns></returns>
        public List<GameSession> GetAllConnections()
        {
            lock (_lockObject)
            {
                return _connections.Values.ToList();
            }
        }

        /// <summary>
        ///     Get connection by unique identifier
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <returns></returns>
        public GameSession GetConnectionByUniqueIdentifier(UniqueId uniqueIdentifier)
        {
            lock (_lockObject)
            {
                return _connections.FirstOrDefault(c => c.Key.Id == uniqueIdentifier.Id).Value;
            }
        }

        /// <summary>
        ///     Get connection by character name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameSession GetConnectionByCharacterName(string name)
        {
            lock (_lockObject)
            {
                return _connections.Values.FirstOrDefault(c => c.Pc?.Simple.NickName == name);
            }
        }
        #endregion

        #region Items
        /// <summary>
        ///     Add item
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(GPublicItem item)
        {
            lock (_lockObject)
            {
                item.UniqueId = CreateUniqueIdentifier(UniqueIdentifierType.Item);

                _items.Add(item.UniqueId, item);
            }
        }

        /// <summary>
        ///     Take an item of the world for the one that asks for it. The lookup and the removal
        ///     are one operation under the lock of the service: two players that reach for one thing
        ///     at the same moment come here one after the other, so exactly one of them gets it and
        ///     the other one is told there is nothing lying there any more. A lookup and a removal
        ///     as two calls would hand a copy of the thing to both.
        ///     The original works the same way - it sets a flag "the item is taken" atomically and
        ///     clears it when the pick-up fails; a caller of ours whose pick-up fails puts the item
        ///     back with <see cref="ReturnItem"/>, which leaves it the identifier it already has.
        ///     The number of the item is not released here: the item is out of the world but the
        ///     caller still holds it under that number and may put it back at any moment, and a
        ///     number handed to somebody else in that window would name two things at once. It
        ///     stays with the item until the caller says the item is gone for good and lets it go
        ///     with <see cref="ReleaseItem"/> - every caller of this one ends in exactly one of the
        ///     two
        /// </summary>
        /// <param name="uniqueIdentifier">Identifier of the item the request names</param>
        /// <param name="item">Item that has been taken, none when the caller did not get it</param>
        /// <returns>True when the item is gone out of the world and belongs to the caller</returns>
        public bool TryTakeItem(UniqueId uniqueIdentifier, out GPublicItem item)
        {
            lock (_lockObject)
            {
                var uniqueIdentifierItem = _items.Keys.FirstOrDefault(c => UniqueId.IsSame(c, uniqueIdentifier));

                if (uniqueIdentifierItem == null)
                {
                    item = null;
                    return false;
                }

                item = _items[uniqueIdentifierItem];

                _items.Remove(uniqueIdentifierItem);

                return true;
            }
        }

        /// <summary>
        ///     Put an item back into the world under the identifier it already carries - the way
        ///     back of <see cref="TryTakeItem"/> for a pick-up that did not go through. The world
        ///     has to be left exactly as it was: everybody around has already been told about the
        ///     item under that identifier and names it by it, while the pass of the visibility
        ///     compares the lists of what is seen and would not see anything change here, so an
        ///     item handed a new number would stay unreachable for the clients until it leaves and
        ///     enters the range of sight again.
        ///     The free numbers are not touched at all: the number never left the item - the take
        ///     does not release it - and the generation is left alone as well, it belongs to the
        ///     handout the item still lives under
        /// </summary>
        /// <param name="item">Item that has been taken and is going back on the ground</param>
        public void ReturnItem(GPublicItem item)
        {
            lock (_lockObject)
            {
                _items.Add(item.UniqueId, item);
            }
        }

        /// <summary>
        ///     Let the number of an item that has left the world for good go back to the free ones -
        ///     the other end of <see cref="TryTakeItem"/>, the one <see cref="ReturnItem"/> is not.
        ///     It is the caller that knows when the item is gone, and it says so by this call: the
        ///     pick-up once the row of the item is written and the bag holds it, the garbage pass
        ///     once the item is taken out of the world - whether the row of a rotten item could be
        ///     deleted or not, the item itself is out of the world either way.
        ///     The generation of the number is left alone: it belongs to the handouts of the number
        ///     and outlives every one of them, so that the next entity under this number is told
        ///     from the item that has just been carried off
        /// </summary>
        /// <param name="item">Item that has been taken and is never going back</param>
        public void ReleaseItem(GPublicItem item)
        {
            lock (_lockObject)
            {
                RemoveUniqueIdentifier(item.UniqueId.Id);
            }
        }

        /// <summary>
        ///     Get all items
        /// </summary>
        /// <returns></returns>
        public List<GPublicItem> GetAllItems()
        {
            lock (_lockObject)
            {
                return _items.Values.ToList();
            }
        }

        /// <summary>
        ///     Get item by unique identifier. The number alone does not name an item: it is handed
        ///     out again as soon as the item that held it is gone, so the whole identifier is
        ///     compared - the same rule <see cref="TryTakeItem"/> takes the item by, because a
        ///     caller that looks an item up and then takes it must not get two different things
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <returns></returns>
        public GPublicItem GetItemByUniqueIdentifier(UniqueId uniqueIdentifier)
        {
            lock (_lockObject)
            {
                return _items.FirstOrDefault(c => UniqueId.IsSame(c.Key, uniqueIdentifier)).Value;
            }
        }
        #endregion

        #region Units
        /// <summary>
        ///     Add unit
        /// </summary>
        /// <param name="unit"></param>
        public void AddUnit(GMonster unit)
        {
            lock (_lockObject)
            {
                unit.UniqueId = CreateUniqueIdentifier(UniqueIdentifierType.Monster);

                _units.Add(unit.UniqueId, unit);
            }
        }

        /// <summary>
        ///     Remove unit
        /// </summary>
        /// <param name="unit"></param>
        public void RemoveUnit(GMonster unit)
        {
            lock (_lockObject)
            {
                var uniqueIdentifier = _units.FirstOrDefault(c => c.Value == unit).Key;

                if (uniqueIdentifier != null)
                {
                    RemoveUniqueIdentifier(uniqueIdentifier.Id);
                    _units.Remove(uniqueIdentifier);
                }
            }
        }

        /// <summary>
        ///     Get all units
        /// </summary>
        /// <returns></returns>
        public List<GMonster> GetAllUnits()
        {
            lock (_lockObject)
            {
                return _units.Values.ToList();
            }
        }

        /// <summary>
        ///     Get unit by unique identifier
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <returns></returns>
        public GMonster GetUnitByUniqueIdentifier(UniqueId uniqueIdentifier)
        {
            lock (_lockObject)
            {
                return _units.FirstOrDefault(c => c.Key.Id == uniqueIdentifier.Id).Value;
            }
        }
        #endregion

        #region Other methods
        /// <summary>
        ///     Check connection or unit by unique identifier
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <returns></returns>
        public bool CheckConnectionOrUnitByUniqueIdentifier(UniqueId uniqueIdentifier)
        {
            lock (_lockObject)
            {
                var connection = GetConnectionByUniqueIdentifier(uniqueIdentifier);

                if (connection != null)
                {
                    return true;
                }

                var unit = GetUnitByUniqueIdentifier(uniqueIdentifier);

                if (unit != null)
                {
                    return true;
                }

                return false;
            }
        }
        #endregion

        /// <summary>
        ///     Create unique identifier of the given class. The only place where a number is handed
        ///     out, so that the generation of the number is bumped exactly once per handout
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private UniqueId CreateUniqueIdentifier(UniqueIdentifierType type)
        {
            lock (_lockObject)
            {
                var number = GetUniqueIdentifier();

                return new UniqueId(type)
                {
                    Id = number,
                    Seq = GetNextGeneration(number)
                };
            }
        }

        /// <summary>
        ///     Get unique identifier
        /// </summary>
        /// <returns></returns>
        private uint GetUniqueIdentifier()
        {
            if (_uniqueIdentifiers.Count == 0)
            {
                _uniqueIdentifiers.Enqueue(_uniqueIdentifiersCounter);
                _uniqueIdentifiersCounter = _uniqueIdentifiersCounter + 1;
            }

            return _uniqueIdentifiers.Dequeue();
        }

        /// <summary>
        ///     Get the generation of the next handout of the number: one for the first handout, one
        ///     more than the previous one for every next, wrapping around the width of the field
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <returns></returns>
        private uint GetNextGeneration(uint uniqueIdentifier)
        {
            uint generation;

            if (_uniqueIdentifierGenerations.TryGetValue(uniqueIdentifier, out var previousGeneration))
            {
                generation = (previousGeneration + 1) % UniqueId.SeqCount;
            }
            else
            {
                generation = UniqueId.FirstSeq;
            }

            _uniqueIdentifierGenerations[uniqueIdentifier] = generation;

            return generation;
        }

        /// <summary>
        ///     Remove unique identifier
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        private void RemoveUniqueIdentifier(uint uniqueIdentifier)
        {
            _uniqueIdentifiers.Enqueue(uniqueIdentifier);
        }
    }
}

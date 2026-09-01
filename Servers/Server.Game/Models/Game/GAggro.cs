using Packets.Server.Game.Structures;
using System;
using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     One record of the aggro history: who has hit the monster and how much damage that one has
    ///     dealt to it altogether. The record carries the identifier and not the attacker itself -
    ///     a session may leave the world while the record still lies in the history, and looking the
    ///     identifier up is the business of whoever needs the attacker
    /// </summary>
    public class GAggro
    {
        /// <param name="uniqueId">Identifier of the attacker</param>
        /// <param name="point">Damage this record starts with</param>
        public GAggro(UniqueId uniqueId, int point)
        {
            UniqueId = uniqueId;
            Point = point;
        }

        /// <summary>
        ///     Identifier of the attacker. Never replaced: a record is about one attacker only
        /// </summary>
        public UniqueId UniqueId { get; }

        /// <summary>
        ///     Damage this attacker has dealt to the monster since the record was opened
        /// </summary>
        public int Point { get; set; }
    }

    /// <summary>
    ///     Aggro history of a monster: the attackers it remembers and the damage each of them has
    ///     dealt. The history is what the monster picks its next target out of when the one it
    ///     fights is gone, so it outlives a single swing and is cleared only with the whole combat
    ///     state.
    ///     <para>
    ///     Only <see cref="Capacity"/> attackers fit in. An attacker that is already remembered
    ///     accumulates its damage in its own record, a new one takes a free place, and a new one
    ///     that comes when there is no free place is not remembered at all - the history does not
    ///     push anybody out. That is how the original keeps it: the fifth attacker of a monster is
    ///     simply not written down, however hard it hits.
    ///     </para>
    ///     <para>
    ///     The history is written from the swing pass of the players and read and cleared from the
    ///     AI pass, so everything it holds is kept behind <see cref="SyncRoot"/>. Every method takes
    ///     that lock itself; a caller that has to keep a pair of changes together - the history and
    ///     the target of the monster are changed as one - takes the very same lock around them, and
    ///     the methods called inside simply take it again (a lock of one thread is reentrant)
    ///     </para>
    /// </summary>
    public class GAggroHistory
    {
        /// <summary>
        ///     How many attackers the history remembers at once
        /// </summary>
        public const int Capacity = 4;

        /// <summary>
        ///     Lock object
        /// </summary>
        private readonly object _lockObject = new object();

        private readonly List<GAggro> _records = new List<GAggro>(Capacity);

        /// <summary>
        ///     The lock the whole state of the history lives behind. Given out so that a caller can
        ///     hold the history still while it changes something of its own next to it
        /// </summary>
        public object SyncRoot => _lockObject;

        /// <summary>
        ///     How many attackers are remembered right now
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _records.Count;
                }
            }
        }

        /// <summary>
        ///     Remember the damage of one hit: the record of an attacker that is already known grows
        ///     by it, an unknown attacker opens a record of its own while there is a free place
        /// </summary>
        /// <param name="attacker">Identifier of the one who has hit</param>
        /// <param name="damage">Damage of the hit</param>
        /// <returns>Whether the attacker is remembered now - false when the history was full</returns>
        public bool Register(UniqueId attacker, int damage)
        {
            if (attacker == null)
            {
                return false;
            }

            lock (_lockObject)
            {
                GAggro record = Find(attacker);

                if (record != null)
                {
                    record.Point += damage;
                    return true;
                }

                if (_records.Count >= Capacity)
                {
                    return false;
                }

                _records.Add(new GAggro(attacker, damage));

                return true;
            }
        }

        /// <summary>
        ///     Forget one attacker: the target that has died, left the world or was given up on is
        ///     taken out of the history so that it is not picked again
        /// </summary>
        /// <param name="attacker">Identifier of the attacker to forget</param>
        /// <returns>Whether there was a record to take out</returns>
        public bool Remove(UniqueId attacker)
        {
            if (attacker == null)
            {
                return false;
            }

            lock (_lockObject)
            {
                GAggro record = Find(attacker);

                if (record == null)
                {
                    return false;
                }

                return _records.Remove(record);
            }
        }

        /// <summary>
        ///     One of the remembered attackers, drawn at random. The one that has hit hardest is not
        ///     preferred in any way - the original draws a record and so do we
        /// </summary>
        /// <param name="random">Source of the draw</param>
        /// <returns>A record of the history, null when nobody is remembered</returns>
        public GAggro GetRandom(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            lock (_lockObject)
            {
                if (_records.Count == 0)
                {
                    return null;
                }

                return _records[random.Next(_records.Count)];
            }
        }

        /// <summary>
        ///     Forget everybody. The list itself is kept and only emptied: it is the state behind the
        ///     lock, and replacing it would leave a reader of another thread on the old one
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _records.Clear();
            }
        }

        /// <summary>
        ///     Record of one attacker. The caller holds the lock
        /// </summary>
        /// <param name="attacker">Identifier of the attacker</param>
        private GAggro Find(UniqueId attacker)
        {
            for (int index = 0; index < _records.Count; index++)
            {
                if (IsSame(_records[index].UniqueId, attacker))
                {
                    return _records[index];
                }
            }

            return null;
        }

        /// <summary>
        ///     Whether two identifiers name one and the same entity. The number alone is not enough
        ///     here: a number is handed out again once the entity that held it is gone, and the
        ///     generation is what tells the newcomer from the one the monster used to fight - a
        ///     record may well outlive the entity it was opened for
        /// </summary>
        /// <param name="left">One identifier</param>
        /// <param name="right">The other identifier</param>
        private static bool IsSame(UniqueId left, UniqueId right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.Id == right.Id && left.Seq == right.Seq;
        }
    }
}

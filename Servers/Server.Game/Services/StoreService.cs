using System;
using System.Collections.Generic;
using System.Linq;
using Database.Fnl.Game;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Packets.Server.Game.Models.Send.Settings;
using Server.Game.Network;

namespace Server.Game.Services
{
    /// <summary>
    ///     The personal warehouse of an account as the client is told about it. Two ways lead here
    ///     and both send the same packets: the client asks on its own the moment it is in the world,
    ///     and the keeper of the warehouse opens the window when a character talks to it - so the
    ///     reading lives beside neither handler and is shared by both.
    ///     Nothing is held between the calls: every one of them asks the database again, the way the
    ///     original asks its own procedure on every request
    /// </summary>
    public class StoreService
    {
        /// <summary>
        ///     EStoreType of the original: zero is the warehouse of the account, one and two are
        ///     the two levels of the guild warehouse
        /// </summary>
        private const int StoreTypeAccount = 0;

        private readonly IFnlGameRepository _gameRepository;
        private readonly ILogger<StoreService> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        public StoreService(IFnlGameRepository gameRepository, ILogger<StoreService> logger)
        {
            _gameRepository = gameRepository;
            _logger = logger;
        }

        /// <summary>
        ///     The short list: what lies in the warehouse and how much of it, two numbers a row
        /// </summary>
        /// <param name="client">Session the answer goes to</param>
        /// <param name="userNo">Account the warehouse belongs to</param>
        public void SendCheckStoreList(GameSession client, int userNo)
        {
            CheckStoreListAckModel model = new CheckStoreListAckModel();

            foreach (StoreItemRow row in ReadStore(userNo).Take(StoreListAckModel.MaxRows))
            {
                model.Rows.Add(new CheckStoreRowModel { ItemNo = row.ItemNo, Count = row.Cnt });
            }

            client.Send(model);
        }

        /// <summary>
        ///     The whole list of the warehouse of the account
        /// </summary>
        /// <param name="client">Session the answer goes to</param>
        /// <param name="userNo">Account the warehouse belongs to</param>
        public void SendStoreList(GameSession client, int userNo)
        {
            StoreListAckModel model = new StoreListAckModel { StoreType = StoreTypeAccount };

            // The original cuts the list at three hundred rows: it is the room of the packet, and
            // the rows past it stay in the warehouse and are still counted by the count packet
            foreach (StoreItemRow row in ReadStore(userNo).Take(StoreListAckModel.MaxRows))
            {
                model.Rows.Add(new StoreRowModel
                {
                    SerialNo = row.SerialNo,
                    ItemNo = row.ItemNo,
                    IsConfirm = row.IsConfirm,
                    Status = row.Status,
                    Count = row.Cnt,
                    CountUse = row.CntUse,
                    Owner = row.Owner,
                    TermOfEffectivity = row.PracticalPeriod,
                    HoleCount = row.HoleCount
                });
            }

            client.Send(model);
        }

        /// <summary>
        ///     How many rows the warehouse really holds - the number of the database, which may be
        ///     larger than a list packet is able to carry
        /// </summary>
        /// <param name="client">Session the answer goes to</param>
        /// <param name="userNo">Account the warehouse belongs to</param>
        public void SendStoreCount(GameSession client, int userNo)
        {
            client.Send(new StoreCountAckModel { Count = (uint)ReadStore(userNo).Count });
        }

        /// <summary>
        ///     Rows of the warehouse. A database that did not answer leaves the warehouse empty
        ///     rather than leaving the client without an answer at all
        /// </summary>
        /// <param name="userNo">Account the warehouse belongs to</param>
        private IReadOnlyList<StoreItemRow> ReadStore(int userNo)
        {
            try
            {
                return _gameRepository.GetStoreList(userNo);
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                _logger.LogError(e, "Can not read the warehouse of account {UserNo}", userNo);

                return new List<StoreItemRow>();
            }
        }
    }
}

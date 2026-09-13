using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Server.Login.Models.Family;
using Server.Login.Network;

namespace Server.Login.Services.Family
{
    /// <summary>
    ///     Accounts this channel has certified and has not let go of yet. An account lands here
    ///     the moment its login goes through and leaves when the player takes the key to a game
    ///     server, closes the session or simply stops answering.
    ///
    ///     The original keeps the same list in CCertificationMgr and gives every entry of it a
    ///     deadline of its own: a key nobody ever came for has to stop working. Here the deadline
    ///     is one sweep over the list instead of a timer per entry, which comes to the same thing
    /// </summary>
    public class CertificationRegistry
    {
        private readonly ILogger<CertificationRegistry> _logger;

        private readonly object _lock = new object();
        private readonly Dictionary<int, CertifiedAccount> _accounts = new Dictionary<int, CertifiedAccount>();

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="logger"></param>
        public CertificationRegistry(ILogger<CertificationRegistry> logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     Takes an account that has just logged in
        /// </summary>
        /// <returns>
        ///     Session that held the account before this one, when there was one. The caller is
        ///     the one to decide what to do with it: the original simply drops its own record of
        ///     the older login
        /// </returns>
        public LoginSession Certified(int userNo, string userId, int certifiedKey, string ip, LoginSession session)
        {
            lock (_lock)
            {
                LoginSession previous = null;

                if (_accounts.TryGetValue(userNo, out CertifiedAccount existing) && existing.Session != session)
                {
                    previous = existing.Session;
                }

                _accounts[userNo] = new CertifiedAccount
                {
                    UserNo = userNo,
                    UserId = userId,
                    CertifiedKey = certifiedKey,
                    Ip = ip,
                    Session = session,
                    CertifiedAt = DateTime.UtcNow
                };

                return previous;
            }
        }

        /// <summary>
        ///     Lets an account go: its session is gone, whatever the reason
        /// </summary>
        /// <param name="userNo">Account number</param>
        /// <param name="session">
        ///     Session that is gone. Only it is allowed to clear the record: a session that lost
        ///     the race to a fresh login of the same account must not take the fresh one with it
        /// </param>
        public void Released(int userNo, LoginSession session)
        {
            lock (_lock)
            {
                if (_accounts.TryGetValue(userNo, out CertifiedAccount account) && account.Session == session)
                {
                    _accounts.Remove(userNo);
                }
            }
        }

        /// <summary>
        ///     Is the account certified on this channel right now?
        /// </summary>
        public bool IsCertified(int userNo)
        {
            lock (_lock)
            {
                return _accounts.ContainsKey(userNo);
            }
        }

        /// <summary>
        ///     How many accounts are certified right now
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _accounts.Count;
                }
            }
        }

        /// <summary>
        ///     Drops the records nobody is behind any more: the session is closed, or the key has
        ///     been lying here longer than a player needs to pick a server
        /// </summary>
        /// <param name="lifetime">How long a key is allowed to wait for its player</param>
        public void Expire(TimeSpan lifetime)
        {
            DateTime deadline = DateTime.UtcNow - lifetime;
            List<CertifiedAccount> expired = new List<CertifiedAccount>();

            lock (_lock)
            {
                foreach (KeyValuePair<int, CertifiedAccount> pair in _accounts)
                {
                    CertifiedAccount account = pair.Value;

                    if (account.Session == null || !account.Session.IsConnected || account.CertifiedAt < deadline)
                    {
                        expired.Add(account);
                    }
                }

                foreach (CertifiedAccount account in expired)
                {
                    _accounts.Remove(account.UserNo);
                }
            }

            foreach (CertifiedAccount account in expired)
            {
                _logger.LogInformation("Key of account {Login} is not valid any more: nobody came for it", account.UserId);
            }
        }
    }
}

using System;
using Database.Fnl.Account;
using Microsoft.Extensions.Logging;
using Server.Game.Models.Game;
using Server.Game.Network;
using Server.Game.Services.Database;

namespace Server.Game.Services
{
    /// <summary>
    ///     Marks the end of a game session in the databases the same way the original does:
    ///     UspLogoutPc for the character and UspLogoutUser for the account. Without these calls
    ///     TblUser.mWorldNo stays positive after a disconnect and the next login into another
    ///     world is rejected with eErrNoUserLoginAnother
    /// </summary>
    public class LogoutService
    {
        private readonly IFnlAccountRepository _accountRepository;
        private readonly GameRepository _gameRepository;
        private readonly ILogger<LogoutService> _logger;

        public LogoutService(IFnlAccountRepository accountRepository, GameRepository gameRepository,
            ILogger<LogoutService> logger)
        {
            _accountRepository = accountRepository;
            _gameRepository = gameRepository;
            _logger = logger;
        }

        /// <summary>
        ///     Logs the session out of the world: saves the character and clears the login marks
        ///     of the character and the account. Never throws - the disconnect path must survive
        ///     a database failure
        /// </summary>
        /// <param name="client">Session leaving the world, authorized or not</param>
        public void Logout(GameSession client)
        {
            GSession session = client.Sessions;

            // The session never passed LoginUserReq, the databases know nothing about it
            if (session == null)
            {
                return;
            }

            // The LogoutPcReq handler and the disconnect can both end up here, possibly from
            // two threads at once; only the first caller does the database work
            if (!client.TryBeginLogout())
            {
                return;
            }

            GPc pc = client.Pc;

            if (pc != null)
            {
                try
                {
                    // The final save: the autosave of GameSaveService may be a whole period behind
                    _gameRepository.SavePosition(pc);
                    _gameRepository.LogoutPc(session.AccountId, (int)pc.Simple.PcNo);
                }
                catch (Exception e)
                {
                    // The account logout below matters more than the character save: without it
                    // the next login is rejected, so a failure here must not skip it
                    _logger.LogError(e, $"Can not save character {pc.Simple.PcNo} of account {session.AccountId} on logout");
                }
            }

            try
            {
                LogoutUserResult result = _accountRepository.LogoutUser(session.AccountId, 0, session.UseMacro);

                if (!result.IsSuccess && !result.WasNotInWorld)
                {
                    _logger.LogWarning($"UspLogoutUser answered {result.ReturnCode} for account {session.AccountId}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Can not log out account {session.AccountId} in the database");
            }
        }
    }
}

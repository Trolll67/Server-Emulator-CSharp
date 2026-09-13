using Server.Login.Network;

namespace Server.Login.Core.Factories.Interfaces
{
    /// <summary>
    ///     Factory of servers packets
    /// </summary>
    public interface IServersFactory
    {
        /// <summary>
        ///     Sends packet with servers
        /// </summary>
        /// <param name="loginSession"></param>
        void SendServers(LoginSession loginSession);

        /// <summary>
        ///     Sends refreshed packet with servers
        /// </summary>
        /// <param name="loginSession"></param>
        void SendRefreshedServers(LoginSession loginSession);

        /// <summary>
        ///     Answers that the chosen server wants no phone confirmation, which lets
        ///     the client go on to the game server
        /// </summary>
        /// <param name="loginSession"></param>
        void SendArsAuth(LoginSession loginSession);

        /// <summary>
        ///     Checks that the chosen server is one of those the client was given in the list
        /// </summary>
        /// <param name="serverId">TblParmSvr.mSvrNo the client picked in the list</param>
        /// <returns>True when the server is in the list, otherwise false</returns>
        bool IsKnownServer(short serverId);

        /// <summary>
        ///     Whether this channel has to compare the password against TblUser.mUserPswd:
        ///     option <see cref="ParmServerOption.CertifyToPasswordInDb"/> of TblParmSvrOp.
        ///     When it is off, the original does not check the password at all and the client
        ///     does not even send it in a readable form
        /// </summary>
        /// <returns>True when the password has to be checked in the database</returns>
        bool IsPasswordCheckedInDatabase();

        /// <summary>
        ///     Whether an unknown login creates the account instead of being refused: the negation of
        ///     option <see cref="ParmServerOption.DoNotAccountAutomaticCreation"/> of TblParmSvrOp,
        ///     exactly as the original channel builds the @pIsAddUser flag of the certify procedure
        /// </summary>
        /// <returns>True when a missing account has to be created on login</returns>
        bool IsAccountCreatedOnLogin();
    }
}
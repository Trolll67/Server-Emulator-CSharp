namespace Server.Login.Models.Settings
{
    /// <summary>
    ///     Config for settings login server
    /// </summary>
    public class LoginSetting
    {
        public short Id { get; set; }

        /// <summary>
        ///     Address the server listens on. Doubles as the key this channel is looked up by
        ///     in FNLParm.TblParmSvr (mMajorIp), which is where the listen port comes from
        /// </summary>
        public string ServerIp { get; set; }

        /// <summary>
        ///     Build of the client this channel takes. Zero turns the check off, and it is off by
        ///     default: the number lives in the files of the client, and a wrong one here refuses
        ///     every login with "wrong version"
        /// </summary>
        public uint ClientVersion { get; set; }

        /// <summary>
        ///     Longest login this channel takes. The original picks the length by the country of
        ///     the server; twenty is what it uses everywhere but Korea and China
        /// </summary>
        public int MaxLoginLength { get; set; } = 20;

        /// <summary>
        ///     Longest password this channel takes, see <see cref="MaxLoginLength"/>
        /// </summary>
        public int MaxPasswordLength { get; set; } = 20;

        /// <summary>
        ///     How many players this channel is able to hold. Goes to the servers of the world in
        ///     the packet of the state, and the original takes it from its own session pool
        /// </summary>
        public short MaxSessions { get; set; } = 1000;

        /// <summary>
        ///     How often, in seconds, the channel pings the links of the servers of its world and
        ///     tells them how loaded it is
        /// </summary>
        public int FamilyKeepAliveSeconds { get; set; } = 30;

        /// <summary>
        ///     How many players a field server has to be holding before the client is told the
        ///     server is not empty any more. Below this the server shows as the least loaded one.
        ///     The original keeps the three steps of the scale per country, this is the first
        /// </summary>
        public int ServerLowLoadSessions { get; set; } = 500;

        /// <summary>
        ///     How many players a field server has to be holding before it shows as loaded.
        ///     The second step of the scale, gNrmCnt of the original
        /// </summary>
        public int ServerNormalLoadSessions { get; set; } = 1500;

        /// <summary>
        ///     How many free places are left before the server shows as full. The last step of
        ///     the scale, counted from the session count the server itself reports
        /// </summary>
        public int ServerFullReserveSessions { get; set; } = 550;

        /// <summary>
        ///     Фича-флаг: шифровать исходящие пакеты. Выключен - кадр уходит как сейчас, с
        ///     crypt-байтом 0x00 и телом открытым текстом; включён - crypt-байт 0x01, а тело
        ///     кадра проходит через тот же поточный шифр, которым разбирается входящий поток.
        ///     Welcome-пакет уходит открытым при любом значении флага, иначе клиенту нечем
        ///     расшифровать кадр, который и приносит ему ключевой блок. Включение требует проверки
        ///     на живом клиенте: не исключено, что для направления сервер -> клиент нужен ключ из
        ///     welcome-блока, а не статический ключ BlowfishCrypt
        /// </summary>
        public bool EncryptOutgoingPackets { get; set; }

        /// <summary>
        ///     Фича-флаг: генерировать ключевой блок welcome-пакета. Выключен - клиенту уходит
        ///     заготовленный статический блок, как и раньше; включён - блок той же длины и той же
        ///     раскладки, но с новой ключевой частью на каждое подключение. Ключ шифра сессии при
        ///     этом не меняется, см. <see cref="Server.Login.Network.LoginSession.CipherKey"/>
        /// </summary>
        public bool GenerateSessionKey { get; set; }
    }
}

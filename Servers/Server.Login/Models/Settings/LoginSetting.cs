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
        ///     Own number of the channel server in FNLParm.TblParmSvr. Used when the server can not be
        ///     found by <see cref="ServerIp"/>: the address the emulator listens on and TblParmSvr.mMajorIp
        ///     of the live database are not necessarily the same
        /// </summary>
        public short ChannelSvrNo { get; set; }

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

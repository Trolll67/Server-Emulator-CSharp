using Database.DataModel.Enums;
using Database.Fnl.Parm;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Packets.Server.Game.Models.Send.Settings;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Settings;
using Server.Game.Network;
using Server.Game.Services;
using Packets.Server.Game.Models.Send;
using Packets.Server.Game.Structures;

namespace Server.Game.Core.Factories
{
    public class AuthorizationFactory : IAuthorizationFactory
    {
        /// <summary>
        ///     How many bytes at the end of the welcome block are not key material but fields the
        ///     client expects at their places. Generation rewrites only what stands before them,
        ///     so the length and the layout of the block stay exactly as they were
        /// </summary>
        private const int WelcomeKeyTailLength = 6;

        /// <summary>
        ///     How many pages of contents the client is told about: the options of a server run
        ///     past a hundred, and one packet carries a hundred of them
        /// </summary>
        private const int ContentPages = 2;

        private readonly GameSetting _gameSetting;
        private readonly IFnlParmRepository _parmRepository;
        private readonly OwnServerInfo _ownServerInfo;
        private readonly ILogger<AuthorizationFactory> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        public AuthorizationFactory(IOptions<GameSetting> gameSetting, IFnlParmRepository parmRepository, OwnServerInfo ownServerInfo, ILogger<AuthorizationFactory> logger)
        {
            _gameSetting = gameSetting.Value;
            _parmRepository = parmRepository;
            _ownServerInfo = ownServerInfo;
            _logger = logger;
        }

        public void SendWelcome(GameSession loginSession)
        {
            byte[] packetData = new byte[] {
                0xc6, 0x33, 0xae, 0x4b, 0xa1, 0x13, 0x79, 0x44, 0xcc, 0x30, 0x6f, 0x64, 0x1e, 0x40, 0xfa, 0x67, 0x9d,
                0x23, 0x90, 0x5b, 0x3d, 0x6b, 0x30, 0x28, 0x92, 0x6b, 0xab, 0x51, 0xd3, 0x71, 0x72, 0x30, 0xff, 0x13,
                0x0b, 0x55, 0xfe, 0x30, 0xb5, 0x4f, 0x59, 0x91, 0x1f, 0x1e, 0x74, 0xae, 0x11, 0xc4, 0x73, 0xdd, 0x37,
                0x70, 0x1c, 0x4d, 0x4c, 0x67, 0x13, 0xb6, 0x2b, 0xe0, 0x04, 0x4f, 0x12, 0xa9, 0x61, 0x59, 0x05, 0x02,
                0x3b, 0x0b, 0x76, 0x58, 0x36, 0xe8, 0x50, 0x5c, 0xc8, 0x07, 0x11, 0x4e, 0xa9, 0x35, 0xa9, 0x02, 0xe3,
                0x19, 0xb1, 0x7e, 0xb3, 0x59, 0xeb, 0x53, 0xfe, 0x76, 0x9b, 0x3c, 0x72, 0x1b, 0x7d, 0x66, 0x2b, 0x13,
                0xa2, 0x45, 0xe9, 0x1e, 0x7d, 0x32, 0x3c, 0x05, 0x7f, 0x61, 0x64, 0x18, 0x19, 0x75, 0x7c, 0x6d, 0xc0,
                0x60, 0xdf, 0x19, 0x31, 0x7d, 0xdd, 0x10, 0xde, 0x1f, 0xff, 0x4b, 0xa0, 0x56, 0xaf, 0x63, 0x60, 0x7a,
                0x26, 0x32, 0xed, 0x17, 0xbe, 0x1c, 0x89, 0x13, 0x60, 0x68, 0x65, 0x23, 0x29, 0x56, 0x11, 0x21, 0xdc,
                0xaf, 0x2a, 0x0a, 0x53, 0xc6, 0x3d, 0xda, 0x77, 0xb8, 0x29, 0xfd, 0x46, 0x02, 0x1d, 0x28, 0x7e, 0x06,
                0x2b, 0xad, 0x05, 0x5e, 0x1b, 0x0d, 0x4d, 0xad, 0x74, 0xf1, 0x48, 0x2a, 0x27, 0x25, 0x06, 0x2e, 0x1c,
                0x54, 0x21, 0xed, 0x37, 0x54, 0xa7, 0x00, 0xb3, 0x05, 0xf0, 0x1d
            };

            // Feature flag GameSetting.GenerateSessionKey: the key part of the block becomes a fresh
            // random one per connection, the tail fields and the size of the block are left alone, so
            // the layout of the packet does not move. Nothing else changes yet - the traffic cipher
            // still runs on the static key of BlowfishCrypt, and generating a key without rekeying the
            // cipher only makes sense once the live client is proven to read the block we send
            if (_gameSetting.GenerateSessionKey)
            {
                int keyLength = packetData.Length - WelcomeKeyTailLength;

                RandomNumberGenerator.Fill(new Span<byte>(packetData, 0, keyLength));

                byte[] cipherKey = new byte[keyLength];
                Array.Copy(packetData, cipherKey, keyLength);

                loginSession.CipherKey = cipherKey;
            }

            ConnectionClientModel connectionClientModel = new ConnectionClientModel
            {
                DecryptKey = packetData
            };

            loginSession.Send(connectionClientModel);
        }

        public void SendCertifiedKey(GameSession client, int certifiedKey)
        {
            client.Send(new CertifiedKeyAckModel { CertifiedKey = certifiedKey });
        }

        public void SendServerTime(GameSession client)
        {
            DateTime dateTime = DateTime.Now;

            ServerTimeModel serverTimeModel = new ServerTimeModel
            {
                ServerTick = (int)Environment.TickCount,
                Year = (short)dateTime.Year,
                Month = (short)dateTime.Month,
                DayOfWeek = (short)dateTime.DayOfWeek,
                Day = (short)dateTime.Day,
                Hour = (short)dateTime.Hour,
                Minute = (short)dateTime.Minute,
                Second = (short)dateTime.Second,
                Millisecond = (short)dateTime.Millisecond
            };

            client.Send(serverTimeModel);
        }

        public void SendCompleteEnterWorld(GameSession client)
        {
            CompleteEnterWorldModel completeEnterWorldModel = new CompleteEnterWorldModel
            {
                SessionGameId = client.Pc.UniqueId,
                MapNo = client.Pc.MapNo,
                Position = client.Pc.PositionCur,
                Reputation = client.Pc.Detail.Chaotic,
                AttackRate = client.Pc.Detail.AttackRate,
                MoveRate = client.Pc.Detail.MoveRate
            };

            foreach (var item in client.Pc.Inventory.Items)
            {
                // An item that is not identified goes out under the fake number of its parm row
                // and with the normal status, the same way the packets of the bag send it: what
                // has really dropped stays hidden until the item is identified
                var newItem = new ItemApiModel
                {
                    SerialNumber = (ulong)item.SerialNumber,
                    ItemId = item.IsConfirm ? item.Id : item.FakeId,

                    Count = item.Count,

                    Flag = (byte)(item.IsConfirm ? 1 : 0),
                    EndTick = item.EndTick,
                    ItemStatus = (byte)(item.IsConfirm ? item.Status : ItemStatusEnum.Normal),
                    UseCount = item.UseCount,
                    EatTime = item.EatTime,
                    // The original puts here the minutes left until the term of the thing
                    // ends, taken from its row in the DB of the player; we do not keep the
                    // minutes - zero, the way the original has it for a thing without a term.
                    // Filling it from the minutes of the procedure - together with the general
                    // repair of EndTick and of the reading of the bag
                    TermOfEffectivity = 0,
                    ItemBind = (byte)item.ItemBind,
                    Restore = item.Restore,
                    Hole = item.Hole
                };

                completeEnterWorldModel.Items.Add(newItem);
            }

            client.Send(completeEnterWorldModel);
        }

        /// <summary>
        ///     Contents of this server, eCTrContentsAck of the original: the rows of
        ///     FNLParm.TblParmSvrOp, a hundred options to a page. Every page is sent even when no
        ///     option of this server falls into it - the client expects the whole set.
        ///
        ///     This used to be two recorded packets replayed byte for byte, which told every
        ///     client the contents of the machine the recording was taken on instead of the
        ///     contents of this one
        /// </summary>
        public void SendGameConfiguration(GameSession client)
        {
            IReadOnlyList<ParmServerOptionRow> options;

            try
            {
                options = _parmRepository.GetServerOptions(_ownServerInfo.SvrNo);
            }
            catch (SqlException e)
            {
                // A world that lets nobody in because FNLParm blinked is worse than one running
                // with the contents off, and the client reads an empty set as "nothing is on"
                _logger.LogError(e, "Can not read the contents of server {SvrNo} from FNLParm, the client is told they are all off", _ownServerInfo.SvrNo);

                options = new List<ParmServerOptionRow>();
            }

            for (uint page = 0; page < ContentPages; page++)
            {
                GameConfigurationModel gameConfigurationModel = new GameConfigurationModel { ContentsSeq = page };

                for (int i = 0; i < GameConfigurationModel.ContentsPerPage; i++)
                {
                    gameConfigurationModel.Contents.Add(new GameConfigurationContent());
                }

                // A content stands in the slot of its own number: page zero holds the contents
                // 0..99 and page one the contents 100..199. There is no content number zero, so
                // the very first slot of the first page stays empty - the recorded answer of the
                // original has it empty as well. Shifting the contents by one lands every one of
                // them in the slot of its neighbour, and the client then reads, say, the paid time
                // of eContentsFlatCharge out of eContentsItemCharge
                int first = (int)(page * GameConfigurationModel.ContentsPerPage);

                foreach (ParmServerOptionRow option in options)
                {
                    int index = option.OpNo - first;

                    if (index < 0 || index >= GameConfigurationModel.ContentsPerPage)
                    {
                        continue;
                    }

                    gameConfigurationModel.Contents[index] = new GameConfigurationContent
                    {
                        IsSetup = option.IsSetup,
                        Values = option.Values
                    };
                }

                client.Send(gameConfigurationModel);
            }
        }
    }
}
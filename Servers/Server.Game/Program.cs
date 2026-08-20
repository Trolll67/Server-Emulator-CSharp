using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Database.Fnl.Account;
using Database.Fnl.DependencyInjection;
using Database.Fnl.Game;
using Database.Fnl.Parm;
using Database.Fnl.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Packets.Core.Interfaces;
using Packets.Core.Services;
using Serilog;
using Server.Game.Core.Factories;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Core.Systems;
using Server.Game.Models.Settings;
using Server.Game.Network;
using Server.Game.Services;
using Server.Game.Services.Database;
using Server.Game.Services.Game;
using Server.Game.Services.GameServices;
using Server.Game.Services.Hosted;
using Server.Game.Services.Mapping;

namespace Server.Game
{
    public class Program
    {
        private static ServiceProvider ServiceProvider { get; set; }

        private static async Task Main(string[] args)
        {
            IHost hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, configurationBilder) =>
                {
                    string environment = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT");

                    if (!string.IsNullOrWhiteSpace(environment))
                    {
                        hostingContext.HostingEnvironment.EnvironmentName = environment;
                    }

                    // Register server's settings
                    configurationBilder.SetBasePath(AppContext.BaseDirectory);
                    configurationBilder.AddJsonFile("appsettings.json", optional: false);
                    configurationBilder.AddJsonFile("gamesettings.json", optional: false);
                    configurationBilder.AddJsonFile($"appsettings.{environment}.json", optional: true);

                    // Secrets and real infrastructure addresses are never stored in the tracked appsettings
                    configurationBilder.AddJsonFile("appsettings.Local.json", optional: true);
                    configurationBilder.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    // For view russian symbols
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    // Adding serilog
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .CreateLogger();

                    // Without the databases the server is useless: better to fall at start than on the first packet
                    EnsureFnlDatabaseAccess(hostContext.Configuration);

                    // Access to the original R2 databases (FNLAccount, FNLGame, FNLParm) through the server's DSN files
                    services.AddFnlDatabase(hostContext.Configuration);

                    // Register repositories over the original stored procedures
                    services.AddSingleton<IFnlAccountRepository, FnlAccountRepository>();
                    services.AddSingleton<IFnlGameRepository, FnlGameRepository>();
                    services.AddSingleton<IFnlParmReferenceRepository, FnlParmReferenceRepository>();
                    services.AddSingleton<IFnlParmRepository, FnlParmRepository>();

                    // This server's own identity (svr no, world no, port) resolved once from TblParmSvr
                    services.AddSingleton<OwnServerInfo>();

                    // Register database services
                    services.AddTransient<GameRepository>();
                    services.AddSingleton<ParmRepository>();
                    services.AddSingleton<DatabaseQueueService>();

                    // Register mapping services
                    services.AddSingleton<Services.Mapping.GameMappingService>();
                    services.AddSingleton<Services.DBGameMappingService>();
                    services.AddSingleton<DBParmMappingService>();

                    // Loading configure
                    var gameSettings = hostContext.Configuration.GetSection("GameSetting");
                    services.Configure<GameSetting>(gameSettings);

                    // Register handlers
                    services.AddTransient<ISkillHandler, SkillHandler>();
                    services.AddTransient<IAbnormalHandler, AbnormalHandler>();
                    services.AddTransient<IAuthorizationHandler, AuthorizationHandler>();
                    services.AddTransient<ICharacterHandler, CharacterHandler>();
                    services.AddTransient<ICharacterActionHandler, CharacterActionHandler>();
                    services.AddTransient<IChatHandler, ChatHandler>();
                    services.AddTransient<IInventarHandler, InventarHandler>();
                    services.AddTransient<IAttackHandler, AttackHandler>();
                    services.AddTransient<IEquipHandler, EquipHandler>();
                    services.AddTransient<IReinforceHandler, ReinforceHandler>();
                    services.AddTransient<INpcActionHandler, NpcActionHandler>();

                    // Register factories
                    services.AddTransient<ISkillFactory, SkillFactory>();
                    services.AddTransient<IAttackFactory, AttackFactory>();
                    services.AddTransient<IAuthorizationFactory, AuthorizationFactory>();
                    services.AddTransient<ICharacterActionFactory, CharacterActionFactory>();
                    services.AddTransient<ICharacterFactory, CharacterFactory>();
                    services.AddTransient<ICharacteristicFactory, CharacteristicFactory>();
                    services.AddTransient<IInfoStomachFactory, InfoStomachFactory>();
                    services.AddTransient<IChatFactory, ChatFactory>();
                    services.AddTransient<IErrorFactory, ErrorFactory>();
                    services.AddTransient<IEquipFactory, EquipFactory>();
                    services.AddTransient<IInventoryFactory, InventoryFactory>();
                    services.AddTransient<IMonsterActionFactory, MonsterActionFactory>();
                    services.AddTransient<IVisibleFactory, VisibleFactory>();
                    services.AddTransient<IReinforceFactory, ReinforceFactory>();
                    services.AddTransient<INpcActionFactory, NpcActionFactory>();

                    // Register systems
                    services.AddTransient<AttackSystem>();
                    services.AddTransient<CharacterSystem>();
                    services.AddTransient<EquipSystem>();
                    services.AddTransient<AbnormalSystem>();
                    services.AddTransient<ExpSystem>();
                    services.AddTransient<InventarSystem>();
                    services.AddTransient<ItemUseSystem>();
                    services.AddTransient<MoveSystem>();
                    services.AddTransient<ReinforceSystem>();
                    services.AddTransient<UnitDropSystem>();
                    services.AddTransient<UnitSystem>();

                    // Register all handlers packet
                    services.AddSingleton<IRegisterHandlerService, RegisterHandlerService>();

                    // Register services
                    services.AddSingleton<IdentificationService>();
                    services.AddSingleton<SerialNumberService>();
                    services.AddSingleton<LogoutService>();
                    services.AddSingleton<GameServer>();

                    // Register hosted services
                    services.AddHostedService<NetworkHostedService>();

                    // Register game hosted services
                    services.AddHostedService<AttackGameService>();
                    services.AddHostedService<BuffGameService>();
                    services.AddHostedService<GarbageGameService>();
                    services.AddHostedService<RecoveryGameService>();
                    services.AddHostedService<UnitGameService>();
                    services.AddHostedService<VisibleGameService>();
                    services.AddHostedService<GameSaveService>();

                    // Building service provider
                    ServiceProvider = services.BuildServiceProvider();

                    // Register all handlers packet assembly
                    ServiceProvider.GetService<IRegisterHandlerService>().RegistrationModels(Assembly.Load("Packets.Server.Game"));
                    ServiceProvider.GetService<IRegisterHandlerService>().RegistrationParsers(Assembly.Load("Packets.Server.Game"));
                    ServiceProvider.GetService<IRegisterHandlerService>().RegistrationHandlers(Assembly.Load("Server.Game"));

                })
                .UseSerilog()
                .Build();

            await hostBuilder.RunAsync();
        }

        /// <summary>
        ///     Checks that the access to the original R2 databases can be resolved before the server starts
        ///     listening: a missing DSN file would only show up as a swallowed exception on the first
        ///     packet that touches the database
        /// </summary>
        /// <param name="configuration"></param>
        private static void EnsureFnlDatabaseAccess(IConfiguration configuration)
        {
            string dsnDirectory = configuration
                .GetSection(FnlDatabaseOptions.SectionName)[nameof(FnlDatabaseOptions.DsnDirectory)];

            List<string> problems = new List<string>();

            foreach (string name in new[] { FnlConnectionNames.FnlAccount, FnlConnectionNames.FnlGame, FnlConnectionNames.FnlParm })
            {
                // An explicit connection string replaces the DSN file, then there is nothing to check
                if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString(name)))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dsnDirectory))
                {
                    problems.Add($"  \"{FnlDatabaseOptions.SectionName}:{nameof(FnlDatabaseOptions.DsnDirectory)}\" " +
                                 $"is empty, so '{name}' can not be resolved");

                    continue;
                }

                string path = SqlConnectionFactory.GetDsnPath(dsnDirectory, name);

                if (!File.Exists(path))
                {
                    problems.Add($"  DSN file \"{path}\" for '{name}' is not found");
                }
            }

            if (problems.Count == 0)
            {
                return;
            }

            // Paths and key names only: the DSN files themselves carry the database password
            StringBuilder message =
                new StringBuilder("Game server cannot start: access to the R2 databases is not configured.");

            foreach (string problem in problems)
            {
                message.AppendLine();
                message.Append(problem);
            }

            message.AppendLine();
            message.Append($"  Point \"{FnlDatabaseOptions.SectionName}:{nameof(FnlDatabaseOptions.DsnDirectory)}\" ");
            message.Append("at the Data directory of the original server (the one holding Account.dsn, Game.dsn and Parm.dsn), ");
            message.Append($"or set the environment variable ");
            message.Append($"{FnlDatabaseOptions.SectionName}__{nameof(FnlDatabaseOptions.DsnDirectory)}");

            Log.Fatal(message.ToString());

            throw new InvalidOperationException(message.ToString());
        }
    }
}
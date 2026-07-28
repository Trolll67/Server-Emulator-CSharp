using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Database.Fnl.Account;
using Database.Fnl.DependencyInjection;
using Database.Fnl.Parm;
using Database.Fnl.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Packets.Core.Interfaces;
using Packets.Core.Services;
using Serilog;
using Server.Login.Core.Factories;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Core.Handlers;
using Server.Login.Core.Handlers.Interfaces;
using Server.Login.Models.Settings;
using Server.Login.Network;
using Server.Login.Services.Hosted;

namespace Server.Login
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
                    configurationBilder.AddJsonFile("loginsettings.json", optional: false);
                    configurationBilder.AddJsonFile($"appsettings.{environment}.json", optional: true);

                    // Secrets: connection strings with passwords are never stored in the tracked appsettings
                    configurationBilder.AddJsonFile("appsettings.Local.json", optional: true);
                    configurationBilder.AddUserSecrets<Program>(optional: true);
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

                    // Access to the original R2 databases (FNLAccount, FNLParm) through the server's DSN files
                    services.AddFnlDatabase(hostContext.Configuration);

                    // Register repositories over the original stored procedures
                    services.AddSingleton<IFnlAccountRepository, FnlAccountRepository>();
                    services.AddSingleton<IFnlParmRepository, FnlParmRepository>();

                    // Loading configure
                    services.Configure<LoginSetting>(hostContext.Configuration.GetSection("LoginSetting"));

                    // Register handlers
                    services.AddSingleton<IAuthorizationHandler, AuthorizationHandler>();
                    services.AddSingleton<IServersHandler, ServersHandler>();

                    // Register factories
                    services.AddSingleton<IAuthorizationFactory, AuthorizationFactory>();
                    services.AddSingleton<IServersFactory, ServersFactory>();

                    // Register all handlers packet
                    services.AddSingleton<IRegisterHandlerService, RegisterHandlerService>();

                    // Register services
                    services.AddSingleton<LoginServer>();

                    // Register hosted services
                    services.AddHostedService<NetworkHostedService>();

                    // Building service provider
                    ServiceProvider = services.BuildServiceProvider();

                    // Register all handlers packet assembly
                    ServiceProvider.GetService<IRegisterHandlerService>().RegistrationModels(Assembly.Load("Packets.Server.Login"));
                    ServiceProvider.GetService<IRegisterHandlerService>().RegistrationParsers(Assembly.Load("Packets.Server.Login"));
                    ServiceProvider.GetService<IRegisterHandlerService>().RegistrationHandlers(Assembly.Load("Server.Login"));
                })
                .UseSerilog()
                .Build();

            await hostBuilder.RunAsync();
        }

        /// <summary>
        ///     Checks that the access to the original R2 databases can be resolved before the server starts
        ///     listening: a missing DSN file would only show up as a swallowed exception on the first
        ///     authorization packet
        /// </summary>
        /// <param name="configuration"></param>
        private static void EnsureFnlDatabaseAccess(IConfiguration configuration)
        {
            string dsnDirectory = configuration
                .GetSection(FnlDatabaseOptions.SectionName)[nameof(FnlDatabaseOptions.DsnDirectory)];

            List<string> problems = new List<string>();

            foreach (string name in new[] { FnlConnectionNames.FnlAccount, FnlConnectionNames.FnlParm })
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
                new StringBuilder("Login server cannot start: access to the R2 databases is not configured.");

            foreach (string problem in problems)
            {
                message.AppendLine();
                message.Append(problem);
            }

            message.AppendLine();
            message.Append($"  Point \"{FnlDatabaseOptions.SectionName}:{nameof(FnlDatabaseOptions.DsnDirectory)}\" ");
            message.Append("at the Data directory of the original server (the one holding Account.dsn and Parm.dsn), ");
            message.Append($"or set the environment variable ");
            message.Append($"{FnlDatabaseOptions.SectionName}__{nameof(FnlDatabaseOptions.DsnDirectory)}");

            Log.Fatal(message.ToString());

            throw new InvalidOperationException(message.ToString());
        }
    }
}
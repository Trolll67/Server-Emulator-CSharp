using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Database.Fnl.Sql
{
    /// <inheritdoc/>
    /// <remarks>
    ///     Credentials are taken from the DSN files of the original server, the same ones it reads itself.
    ///     Nothing has to be duplicated in the emulator configuration, and no password ends up in a file
    ///     next to the sources. An explicit connection string in the "ConnectionStrings" section wins over
    ///     the DSN file: that is the way out for a deployment where the DSN files are not available
    /// </remarks>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly FnlDatabaseOptions _options;

        // A DSN file is read once: it does not change while the server is running
        private readonly ConcurrentDictionary<string, string> _connectionStrings =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        public SqlConnectionFactory(IConfiguration configuration, IOptions<FnlDatabaseOptions> options)
        {
            _configuration = configuration;
            _options = options.Value;
        }

        /// <inheritdoc/>
        /// <remarks>
        ///     A new connection per query is the normal way of working with ADO.NET: the pool lives
        ///     inside Microsoft.Data.SqlClient and is shared by everyone who asks for the same
        ///     connection string, so what is created here is a handle over a pooled connection
        /// </remarks>
        public SqlConnection Create(string name)
        {
            return new SqlConnection(_connectionStrings.GetOrAdd(name, Build));
        }

        /// <summary>
        ///     Builds the path to the DSN file of a database
        /// </summary>
        /// <param name="directory">Directory with the DSN files</param>
        /// <param name="name">Database name, see <see cref="FnlConnectionNames"/></param>
        public static string GetDsnPath(string directory, string name)
        {
            return Path.Combine(directory, name + ".dsn");
        }

        private string Build(string name)
        {
            string explicitConnectionString = _configuration.GetConnectionString(name);

            if (!string.IsNullOrWhiteSpace(explicitConnectionString))
            {
                return explicitConnectionString;
            }

            if (string.IsNullOrWhiteSpace(_options.DsnDirectory))
            {
                throw new InvalidOperationException(
                    $"Can not connect to '{name}': the directory with the DSN files is not set. " +
                    $"Set \"{FnlDatabaseOptions.SectionName}:{nameof(FnlDatabaseOptions.DsnDirectory)}\" " +
                    $"to the Data directory of the original server, or the environment variable " +
                    $"{FnlDatabaseOptions.SectionName}__{nameof(FnlDatabaseOptions.DsnDirectory)}");
            }

            return FromDsn(GetDsnPath(_options.DsnDirectory, name));
        }

        private string FromDsn(string path)
        {
            DsnFile dsn = DsnFile.Load(path);

            if (string.IsNullOrWhiteSpace(dsn.Server) || string.IsNullOrWhiteSpace(dsn.Database))
            {
                throw new InvalidOperationException(
                    $"DSN file '{path}' has no server address or database name: " +
                    "the Address (or SERVER) and DATABASE keys are expected");
            }

            // Normally the server has already fallen at start on such a configuration, this is the
            // last line of defence for a factory built by hand
            if (!_options.TryValidatePoolSize(out string poolProblem))
            {
                throw new InvalidOperationException(
                    $"Can not connect to '{dsn.Database}': " + poolProblem);
            }

            // The DRIVER key of the DSN file is deliberately ignored: the original goes through ODBC,
            // the emulator talks to SQL Server over TDS with Microsoft.Data.SqlClient
            // The pool keys are written out explicitly, with the ADO.NET defaults as the defaults:
            // the connection string then tells the whole truth about the pool of this database
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = "tcp:" + dsn.Server,
                InitialCatalog = dsn.Database,
                Encrypt = _options.Encrypt,
                TrustServerCertificate = _options.TrustServerCertificate,
                ConnectTimeout = _options.ConnectTimeout,
                Pooling = _options.Pooling,
                MinPoolSize = _options.MinPoolSize,
                MaxPoolSize = _options.MaxPoolSize
            };

            if (string.IsNullOrWhiteSpace(dsn.UserId))
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = dsn.UserId;
                builder.Password = dsn.Password ?? string.Empty;
            }

            return builder.ConnectionString;
        }
    }
}

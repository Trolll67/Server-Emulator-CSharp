namespace Database.Fnl.Sql
{
    /// <summary>
    ///     Settings of the access to the original R2 databases, the "FnlDatabase" configuration section
    /// </summary>
    /// <remarks>
    ///     Everything below is applied to the connection string built from a DSN file. An explicit
    ///     connection string in the "ConnectionStrings" section is used as is: if the operator wrote
    ///     the whole string himself, he sets the pool there as well
    /// </remarks>
    public class FnlDatabaseOptions
    {
        /// <summary>
        ///     Name of the configuration section
        /// </summary>
        public const string SectionName = "FnlDatabase";

        /// <summary>
        ///     Directory with the DSN files of the original server, for example E:\R2\CleanServer\Data.
        ///     Credentials are read from there and are never stored in the emulator configuration
        /// </summary>
        public string DsnDirectory { get; set; }

        /// <summary>
        ///     Encrypt the connection to SQL Server. The DSN files say nothing about it, and
        ///     Microsoft.Data.SqlClient turns encryption on by default since version 4 —
        ///     which the original server, living in a trusted network, does not expect
        /// </summary>
        public bool Encrypt { get; set; }

        /// <summary>
        ///     Accept the server certificate without validating it. Needed while the SQL Server
        ///     in Docker works with a self-signed certificate
        /// </summary>
        public bool TrustServerCertificate { get; set; } = true;

        /// <summary>
        ///     Connection timeout in seconds
        /// </summary>
        public int ConnectTimeout { get; set; } = 15;

        /// <summary>
        ///     Keep the connections in the pool. Microsoft.Data.SqlClient pools them by the connection
        ///     string on its own, so a new <see cref="Microsoft.Data.SqlClient.SqlConnection"/> per query
        ///     costs nothing but a rented socket. Turning the pooling off is a debugging measure only:
        ///     every query then opens a real TDS connection
        /// </summary>
        public bool Pooling { get; set; } = true;

        /// <summary>
        ///     How many connections the pool keeps open even while idle. The ADO.NET default, zero,
        ///     means the pool empties itself between the waves of players
        /// </summary>
        public int MinPoolSize { get; set; }

        /// <summary>
        ///     Upper bound of the connections in the pool, per connection string. When it is reached
        ///     the next query waits for a free connection up to <see cref="ConnectTimeout"/> seconds.
        ///     The ADO.NET default is 100
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        ///     Checks the pool size. SqlConnectionStringBuilder rejects a wrong one itself, but with a
        ///     message about an argument out of range: it says nothing about the configuration key to fix.
        ///     The server calls this at start, so a typo stops it before it begins listening
        /// </summary>
        /// <param name="problem">What is wrong with the size, empty when the size is valid</param>
        /// <returns>True when the size can be used</returns>
        public bool TryValidatePoolSize(out string problem)
        {
            if (MinPoolSize >= 0 && MaxPoolSize >= 1 && MinPoolSize <= MaxPoolSize)
            {
                problem = string.Empty;

                return true;
            }

            problem =
                $"wrong connection pool size: \"{SectionName}:{nameof(MinPoolSize)}\" is {MinPoolSize}, " +
                $"\"{SectionName}:{nameof(MaxPoolSize)}\" is {MaxPoolSize}. " +
                "The minimum is not negative, the maximum is at least one and not less than the minimum";

            return false;
        }
    }
}

namespace Database.Fnl.Sql
{
    /// <summary>
    ///     Settings of the access to the original R2 databases, the "FnlDatabase" configuration section
    /// </summary>
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
    }
}

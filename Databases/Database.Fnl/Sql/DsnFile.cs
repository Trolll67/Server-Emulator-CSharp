using System;
using System.Collections.Generic;
using System.IO;

namespace Database.Fnl.Sql
{
    /// <summary>
    ///     A DSN file of the original R2 server: an ODBC ini with the database access parameters.
    ///     The original reads exactly these files, so they stay the single source of the credentials
    ///     and nothing has to be duplicated in the emulator configuration
    /// </summary>
    /// <example>
    ///     [ODBC]
    ///     DRIVER=ODBC Driver 17 for SQL Server
    ///     UID=sa
    ///     PWD=...
    ///     Address=10.10.10.61\SQLSERVER,1463
    ///     DATABASE=FNLAccount
    ///     SERVER=10.10.10.61\SQLSERVER
    /// </example>
    public class DsnFile
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        private DsnFile(string path, IReadOnlyDictionary<string, string> values)
        {
            Path = path;
            _values = values;
        }

        /// <summary>
        ///     Full path the file was read from
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Database name, the DATABASE key
        /// </summary>
        public string Database => Get("DATABASE");

        /// <summary>
        ///     Login, the UID key. Empty means a trusted connection
        /// </summary>
        public string UserId => Get("UID");

        /// <summary>
        ///     Password, the PWD key
        /// </summary>
        public string Password => Get("PWD");

        /// <summary>
        ///     Server address. Address is preferred over SERVER: it is the one that carries the port
        ///     (10.10.10.61\SQLSERVER,1463 against 10.10.10.61\SQLSERVER)
        /// </summary>
        public string Server
        {
            get
            {
                string address = Get("Address");

                return string.IsNullOrWhiteSpace(address) ? Get("SERVER") : address;
            }
        }

        /// <summary>
        ///     Reads and parses a DSN file
        /// </summary>
        /// <param name="path">Full path to the file</param>
        /// <exception cref="FileNotFoundException">The file does not exist</exception>
        public static DsnFile Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"DSN file '{path}' is not found", path);
            }

            Dictionary<string, string> values =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();

                // Section headers ([ODBC]), empty lines and comments carry nothing for us
                if (trimmed.Length == 0 || trimmed[0] == '[' || trimmed[0] == ';')
                {
                    continue;
                }

                int separator = trimmed.IndexOf('=');

                if (separator <= 0)
                {
                    continue;
                }

                // Split by the first '=' only: a password may contain one as well
                string key = trimmed.Substring(0, separator).Trim();
                string value = trimmed.Substring(separator + 1).Trim();

                values[key] = value;
            }

            return new DsnFile(path, values);
        }

        private string Get(string key)
        {
            return _values.TryGetValue(key, out string value) ? value : null;
        }
    }
}

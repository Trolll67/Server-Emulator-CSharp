using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Database.Fnl.Sql
{
    /// <inheritdoc/>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="configuration"></param>
        public SqlConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public SqlConnection Create(string name)
        {
            string connectionString = _configuration.GetConnectionString(name);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // The connection string itself is never put into the message: it carries the sa password
                throw new InvalidOperationException(
                    $"Connection string '{name}' is not set; set it via user-secrets (ConnectionStrings:{name}) " +
                    $"or the environment variable ConnectionStrings__{name}");
            }

            return new SqlConnection(connectionString);
        }
    }
}

using Microsoft.Data.SqlClient;

namespace Database.Fnl.Sql
{
    /// <summary>
    ///     Creates connections to the original R2 databases
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        ///     Creates a closed connection by the connection string name
        /// </summary>
        /// <param name="name">Key in the "ConnectionStrings" section, see <see cref="FnlConnectionNames"/></param>
        /// <returns>Connection that the caller is responsible for opening and disposing</returns>
        SqlConnection Create(string name);
    }
}

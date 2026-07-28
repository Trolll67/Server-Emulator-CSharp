using Database.Fnl.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Database.Fnl.DependencyInjection
{
    /// <summary>
    ///     Registration of the data layer over the original R2 databases
    /// </summary>
    public static class FnlDatabaseServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the access to the FNL* databases
        /// </summary>
        /// <param name="services"></param>
        /// <returns>The same collection for chaining</returns>
        public static IServiceCollection AddFnlDatabase(this IServiceCollection services)
        {
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

            return services;
        }
    }
}

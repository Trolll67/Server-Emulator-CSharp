using Database.Fnl.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Database.Fnl.DependencyInjection
{
    /// <summary>
    ///     Registration of the data layer over the original R2 databases
    /// </summary>
    public static class FnlDatabaseServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the access to the FNL* databases. Credentials are read from the DSN files of the
        ///     original server, the directory is taken from the "FnlDatabase" configuration section
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns>The same collection for chaining</returns>
        public static IServiceCollection AddFnlDatabase(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<FnlDatabaseOptions>(
                configuration.GetSection(FnlDatabaseOptions.SectionName));

            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

            return services;
        }
    }
}

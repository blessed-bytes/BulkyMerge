using BulkyMerge.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

namespace BulkyMerge.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers BulkyMerge bulk operations.</summary>
    public static IServiceCollection AddBulkyMerge(this IServiceCollection services)
    {
        services.AddSingleton<IBulkOperations>(PostgreSqlBulkOperations.Instance);
        return services;
    }

    /// <summary>Registers PostgreSQL as the bulk operations provider.</summary>
    public static IServiceCollection AddBulkyMergePostgreSql(this IServiceCollection services)
        => services.AddSingleton<IBulkOperations>(PostgreSqlBulkOperations.Instance);
}

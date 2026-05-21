using BulkyMerge.PostgreSql;
using Dapper;
using Npgsql;

namespace BulkyMerge.PostgreSql.Tests;

public sealed class InsertManyTests : PgSqlTestsBase
{
    [Fact]
    public async Task InsertMany_maps_generated_keys_and_inserts_all_rows()
    {
        var tableName = $"AllFieldTypesTests_{Guid.NewGuid():N}";
        try
        {
            CreateAllFieldsTable(tableName);
            var items = Enumerable.Range(0, 100)
                .Select(i => new AllFieldTypesWithIdentityTests
                {
                    DecimalValue = i,
                    IntValue = i,
                    BigTextValue = $"Text {i}",
                    EnumValue = EnumValue.Third,
                    GuidValue = Guid.NewGuid(),
                })
                .ToList();

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await connection.InsertManyAsync(
                items,
                new InsertManyOptions
                {
                    Table = TableRef.Parse(tableName),
                    MapGeneratedKeys = true,
                });

            var select = (await connection.QueryAsync<AllFieldTypesWithIdentityTests>(
                $"""SELECT * FROM "{tableName}" ORDER BY "Id" ASC""")).ToList();

            AllFieldsTestAssertions(select, items);
        }
        finally
        {
            DropTable(tableName);
        }
    }
}

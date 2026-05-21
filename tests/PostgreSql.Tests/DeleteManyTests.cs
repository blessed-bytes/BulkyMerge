using BulkyMerge.PostgreSql;
using Dapper;
using Npgsql;

namespace BulkyMerge.PostgreSql.Tests;

public sealed class DeleteManyTests : PgSqlTestsBase
{
    [Fact]
    public async Task DeleteMany_removes_rows_by_primary_key()
    {
        var tableName = $"DeleteMany_{Guid.NewGuid():N}";
        try
        {
            CreateAllFieldsTable(tableName);
            var items = Enumerable.Range(0, 40)
                .Select(i => new AllFieldTypesWithIdentityTests
                {
                    IntValue = i,
                    DecimalValue = i,
                    NvarcharValue = $"row {i}",
                    GuidValue = Guid.NewGuid(),
                })
                .ToList();

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await connection.InsertManyAsync(items, new InsertManyOptions { Table = TableRef.Parse(tableName) });

            var loaded = (await connection.QueryAsync<AllFieldTypesWithIdentityTests>(
                $"""SELECT * FROM "{tableName}" ORDER BY "Id" ASC""")).ToList();

            var toDelete = loaded.Where(x => x.IntValue % 2 == 0).ToList();
            await connection.DeleteManyAsync(toDelete, new DeleteManyOptions { Table = TableRef.Parse(tableName) });

            var count = await connection.ExecuteScalarAsync<int>($"""SELECT COUNT(*) FROM "{tableName}" """);
            Assert.Equal(20, count);
        }
        finally
        {
            DropTable(tableName);
        }
    }
}

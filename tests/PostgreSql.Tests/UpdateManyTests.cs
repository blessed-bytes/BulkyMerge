using BulkyMerge.PostgreSql;
using Dapper;
using Npgsql;

namespace BulkyMerge.PostgreSql.Tests;

public sealed class UpdateManyTests : PgSqlTestsBase
{
    [Fact]
    public async Task UpdateMany_changes_existing_rows()
    {
        var tableName = $"UpdateMany_{Guid.NewGuid():N}";
        try
        {
            CreateAllFieldsTable(tableName);
            var items = Enumerable.Range(0, 50)
                .Select(i => new AllFieldTypesWithIdentityTests
                {
                    IntValue = i,
                    DecimalValue = i,
                    NvarcharValue = $"before {i}",
                    EnumValue = EnumValue.First,
                    GuidValue = Guid.NewGuid(),
                })
                .ToList();

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await connection.InsertManyAsync(items, new InsertManyOptions { Table = TableRef.Parse(tableName) });

            var loaded = (await connection.QueryAsync<AllFieldTypesWithIdentityTests>(
                $"""SELECT * FROM "{tableName}" ORDER BY "Id" ASC""")).ToList();

            foreach (var row in loaded)
                row.NvarcharValue = $"after {row.IntValue}";

            await connection.UpdateManyAsync(loaded, new UpdateManyOptions { Table = TableRef.Parse(tableName) });

            var updated = (await connection.QueryAsync<AllFieldTypesWithIdentityTests>(
                $"""SELECT * FROM "{tableName}" ORDER BY "IntValue" ASC""")).ToList();

            Assert.Equal(50, updated.Count);
            Assert.All(updated, r => Assert.StartsWith("after ", r.NvarcharValue, StringComparison.Ordinal));
        }
        finally
        {
            DropTable(tableName);
        }
    }
}

using BulkyMerge.PostgreSql;
using Dapper;
using Npgsql;

namespace BulkyMerge.PostgreSql.Tests;

public sealed class UpsertManyTests : PgSqlTestsBase
{
    [Fact]
    public async Task UpsertMany_inserts_new_and_updates_existing()
    {
        var tableName = $"UpsertMany_{Guid.NewGuid():N}";
        try
        {
            CreateAllFieldsTable(tableName);
            var existing = Enumerable.Range(0, 30)
                .Select(i => new AllFieldTypesWithIdentityTests
                {
                    IntValue = i,
                    DecimalValue = i,
                    NvarcharValue = $"seed {i}",
                    EnumValue = EnumValue.First,
                    GuidValue = Guid.NewGuid(),
                })
                .ToList();

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await connection.InsertManyAsync(existing, new InsertManyOptions { Table = TableRef.Parse(tableName) });

            var loaded = (await connection.QueryAsync<AllFieldTypesWithIdentityTests>(
                $"""SELECT * FROM "{tableName}" ORDER BY "Id" ASC""")).ToList();

            var batch = new List<AllFieldTypesWithIdentityTests>();
            foreach (var row in loaded)
            {
                row.NvarcharValue = $"updated {row.IntValue}";
                batch.Add(row);
            }

            batch.AddRange(Enumerable.Range(30, 20).Select(i => new AllFieldTypesWithIdentityTests
            {
                IntValue = i,
                DecimalValue = i,
                NvarcharValue = $"new {i}",
                EnumValue = EnumValue.Second,
                GuidValue = Guid.NewGuid(),
            }));

            await connection.UpsertManyAsync(
                batch,
                new UpsertManyOptions
                {
                    Table = TableRef.Parse(tableName),
                    MapGeneratedKeys = true,
                });

            var count = await connection.ExecuteScalarAsync<int>($"""SELECT COUNT(*) FROM "{tableName}" """);
            Assert.Equal(50, count);

            var all = (await connection.QueryAsync<AllFieldTypesWithIdentityTests>(
                $"""SELECT * FROM "{tableName}" ORDER BY "IntValue" ASC""")).ToList();

            Assert.Equal(30, all.Count(r => r.NvarcharValue.StartsWith("updated ", StringComparison.Ordinal)));
            Assert.Equal(20, all.Count(r => r.NvarcharValue.StartsWith("new ", StringComparison.Ordinal)));
        }
        finally
        {
            DropTable(tableName);
        }
    }
}

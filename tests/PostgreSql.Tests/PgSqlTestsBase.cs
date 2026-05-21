using Dapper;
using Npgsql;

namespace BulkyMerge.PostgreSql.Tests;

public class AllFieldTypesWithIdentityTests
{
    public long Id { get; set; }
    public string NvarcharValue { get; set; } = "";
    public EnumValue? EnumValue { get; set; }
    public string BigTextValue { get; set; } = "";
    public int IntValue { get; set; }
    public decimal DecimalValue { get; set; }
    public Guid GuidValue { get; set; }
    public DateTime CreateDate { get; set; }
}

public enum EnumValue : short
{
    First = 0,
    Second,
    Third,
}

public class PgSqlTestsBase
{
    protected static string ConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=YourPassword;";

    protected void DropTable(string name)
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        connection.Execute($"""DROP TABLE IF EXISTS "{name}";""");
    }

    protected void CreateAllFieldsTable(string name)
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        connection.Execute($"""
            DROP TABLE IF EXISTS "{name}";
            CREATE TABLE "{name}"
            (
                "Id" SERIAL PRIMARY KEY,
                "IntValue" integer NULL,
                "BigIntValue" bigint NULL,
                "DecimalValue" decimal(10, 4) NULL,
                "NvarcharValue" varchar(255) NULL,
                "EnumValue" integer NULL,
                "BigTextValue" TEXT NULL,
                "CreateDate" date NULL,
                "GuidValue" uuid NULL
            );
            """);
    }

    protected static void AllFieldsTestAssertions(
        IEnumerable<AllFieldTypesWithIdentityTests> select,
        IEnumerable<AllFieldTypesWithIdentityTests> items)
    {
        var list = select.ToList();
        var source = items.ToList();
        var count = list.Count;
        Assert.Equal(count, source.Count);
        Assert.True(list.Select(x => x.Id).SequenceEqual(source.Select(x => x.Id)));
        Assert.True(list.OrderBy(x => x.IntValue).Select(x => x.IntValue).SequenceEqual(Enumerable.Range(0, count)));
        Assert.True(list.OrderBy(x => x.DecimalValue).Select(x => x.DecimalValue)
            .SequenceEqual(Enumerable.Range(0, count).Select(x => (decimal)x)));
        Assert.True(list.All(x => x.EnumValue == EnumValue.Third));
    }
}

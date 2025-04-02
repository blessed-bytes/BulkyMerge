using BulkyMerge.PostgreSql;
using BulkyMerge.Root;
using BulkyMerge.SqlServer;
using Newtonsoft.Json;
using Npgsql;
using System.Data;
using Dapper;
using System.Diagnostics;
const string pgsqlConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=YourPassword;";

await CreateTable();

TypeConverters.RegisterTypeConverter(typeof(JsonObj), JsonConvert.SerializeObject);

var list = Enumerable.Range(0, 1_000_000).Select(x => CreateOrUpdatePerson(x)).ToList();

var stopWatch = Stopwatch.StartNew();
await using var insertConnection = new NpgsqlConnection(pgsqlConnectionString); // MysqlConenction or NpgsqlConnection
await insertConnection.BulkInsertAsync(list);
Console.WriteLine($"BulkInsertAsync {list.Count} takes {stopWatch.Elapsed}");

var updated = list.Select(x => CreateOrUpdatePerson(0, x)).ToList();

stopWatch.Restart();
await using var insertOrUpdateConnection = new NpgsqlConnection(pgsqlConnectionString); // MysqlConenction or NpgsqlConnection
await insertOrUpdateConnection.BulkInsertOrUpdateAsync(updated);
Console.WriteLine($"BulkInsertOrUpdateAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var deleteConnection = new NpgsqlConnection(pgsqlConnectionString); // MysqlConenction or NpgsqlConnection
await deleteConnection.BulkDeleteAsync(list);
Console.WriteLine($"BulkDeleteAsync {list.Count} takes {stopWatch.Elapsed}");

;
Person CreateOrUpdatePerson(int i, Person p = null)
{
    var randString = Guid.NewGuid().ToString("N");
    p ??= new Person();
    p.FullName = randString;
    p.JsonObj = new JsonObj {  JsonProp = randString };
    p.EnumValue = i % 2 == 0 ? EnumValues.First : EnumValues.Second;
    p.CreateDate = DateTime.UtcNow;
    p.BigTextValue = randString;
    p.NvarcharValue = randString;
    p.BigIntValue = i;
    p.IntValue = i;
    p.DecimalValue = i;
    return p;
}



async Task CreateTable()
{
    await using var createNpgSql = new NpgsqlConnection(pgsqlConnectionString);
    {
        createNpgSql.Execute($@"
            DROP TABLE IF EXISTS ""Person"";
            CREATE TABLE ""Person""
            (
                ""IdentityId"" SERIAL PRIMARY KEY,
                ""IntValue"" integer NULL,
                ""BigIntValue"" bigint NULL,
                ""DecimalValue"" decimal(10, 4) NULL,
                ""NvarcharValue"" varchar(255) NULL,
                ""FullName"" varchar(255) NULL,
                ""JsonObj"" JSONB NULL,
                ""EnumValue"" integer NULL,
                ""BigTextValue"" TEXT NULL,
                ""CreateDate"" date NULL,
                ""GuidValue"" uuid NULL
            )");

    }

}


public class Person
{
    public int IdentityId { get; set; }
    public string FullName { get; set; }
    public JsonObj JsonObj { get; set; }
    public string BigTextValue { get; set; }
    public decimal DecimalValue { get; set; }
    public EnumValues EnumValue { get; set; }
    public DateTime CreateDate { get; set; }
    public long BigIntValue { get; set; }
    public int IntValue { get; set; }
    public Guid GuidValue { get; set; }
    public string NvarcharValue { get; set; }
}
public enum EnumValues
{
    First = 1,
    Second = 2
}

public class JsonObj
{
    public string JsonProp { get; set; }
}
BulkyMerge - fast BulkInsert, BulkUpdate, BulkInsertOrUpdate, BulkCopy and BulkDelete extensions
========================================

[![Build status](https://ci.appveyor.com/api/projects/status/iylj7wjrak5866i6?svg=true)](https://ci.appveyor.com/project/filipppka/dapper-fastbulkoperations)

Benchmark :
https://dotnetfiddle.net/rKyO3Z

![image](https://github.com/user-attachments/assets/d2f1b9fc-e87c-44a6-b545-f0bf23b5c096)

Timinng:

For 10_000 items:

BulkInsertAsync 10000 takes 00:00:00.7225412
BulkInsertOrUpdateAsync 10000 takes 00:00:00.3958080
BulkDeleteAsync 10000 takes 00:00:00.3807820

For 1_000_000 items:

BulkInsertAsync 1000000 takes 00:00:11.0477860
BulkInsertOrUpdateAsync 1000000 takes 00:00:21.3268264
BulkDeleteAsync 1000000 takes 00:00:08.5113587

Simple usage :

Sample for PostgreSQL, same will work for MySql and SqlServer

```csharp
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

var list = Enumerable.Range(0, 10_000).Select(x => CreateOrUpdatePerson(x)).ToList();

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
```
Please check samples folder






# BulkyMerge - Fast Bulk Operations for .NET  
🚀 **Fast** BulkInsert, BulkUpdate, BulkInsertOrUpdate, BulkCopy, and BulkDelete extensions  

🔗 **Benchmark**: [dotnetfiddle.net/rKyO3Z](https://dotnetfiddle.net/rKyO3Z)  

![Performance Graph](https://github.com/user-attachments/assets/d2f1b9fc-e87c-44a6-b545-f0bf23b5c096)  

## ⚡ Performance Timing  

### For **10,000** items:  
- **BulkInsertAsync**: ⏱ 00:00:00.7225412  
- **BulkInsertOrUpdateAsync**: ⏱ 00:00:00.3958080  
- **BulkDeleteAsync**: ⏱ 00:00:00.3807820  

### For **1,000,000** items:  
- **BulkInsertAsync**: ⏱ 00:00:11.0477860  
- **BulkInsertOrUpdateAsync**: ⏱ 00:00:21.3268264  
- **BulkDeleteAsync**: ⏱ 00:00:08.5113587  

---  

## 🚀 Simple Usage  

### Sample for **PostgreSQL**  
*(Works the same for MySQL and SQL Server)*  

```csharp
using BulkyMerge.PostgreSql;
using BulkyMerge.Root;
using Newtonsoft.Json;
using Npgsql;
using Dapper;
using System.Diagnostics;
using System.Data;

const string pgsqlConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=YourPassword;";

await CreateTable();

TypeConverters.RegisterTypeConverter(typeof(JsonObj), JsonConvert.SerializeObject);

var list = Enumerable.Range(0, 10_000).Select(x => CreateOrUpdatePerson(x)).ToList();

var stopWatch = Stopwatch.StartNew();
await using var insertConnection = new NpgsqlConnection(pgsqlConnectionString);
await insertConnection.BulkInsertAsync(list);
Console.WriteLine($"BulkInsertAsync {list.Count} took {stopWatch.Elapsed}");

var updated = list.Select(x => CreateOrUpdatePerson(0, x)).ToList();

stopWatch.Restart();
await using var insertOrUpdateConnection = new NpgsqlConnection(pgsqlConnectionString);
await insertOrUpdateConnection.BulkInsertOrUpdateAsync(updated);
Console.WriteLine($"BulkInsertOrUpdateAsync {list.Count} took {stopWatch.Elapsed}");

stopWatch.Restart();
await using var deleteConnection = new NpgsqlConnection(pgsqlConnectionString);
await deleteConnection.BulkDeleteAsync(list);
Console.WriteLine($"BulkDeleteAsync {list.Count} took {stopWatch.Elapsed}");

Person CreateOrUpdatePerson(int i, Person p = null)
{
    var randString = Guid.NewGuid().ToString("N");
    p ??= new Person();
    p.FullName = randString;
    p.JsonObj = new JsonObj { JsonProp = randString };
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
    createNpgSql.Execute(@"
        DROP TABLE IF EXISTS \"Person\";
        CREATE TABLE \"Person\" (
            \"IdentityId\" SERIAL PRIMARY KEY,
            \"IntValue\" INTEGER NULL,
            \"BigIntValue\" BIGINT NULL,
            \"DecimalValue\" DECIMAL(10, 4) NULL,
            \"NvarcharValue\" VARCHAR(255) NULL,
            \"FullName\" VARCHAR(255) NULL,
            \"JsonObj\" JSONB NULL,
            \"EnumValue\" INTEGER NULL,
            \"BigTextValue\" TEXT NULL,
            \"CreateDate\" DATE NULL,
            \"GuidValue\" UUID NULL
        )");
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

📂 **Check the `samples` folder for more examples!**  

---  

🔥 **Why Use BulkyMerge?**  
✅ High performance bulk operations  
✅ Supports PostgreSQL, MySQL, and SQL Server  
✅ Simple and intuitive API  
✅ Reduces database load and speeds up data processing  

---


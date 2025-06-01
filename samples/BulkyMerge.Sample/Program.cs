using BulkyMerge.PostgreSql;
using BulkyMerge.SqlServer;
using Newtonsoft.Json;
using Npgsql;
using System.Data;
using Dapper;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using BulkyMerge.MySql;
using BulkyMerge;
const string pgsqlConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=YourPassword;";
const string sqlServerConnectionString = "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";
const string mysqlConnectionString = "Server=localhost;Database=test;Uid=root;Pwd=YourPassword;Port=3306;AllowLoadLocalInfile=true;Allow User Variables=true";

await CreateMysqlTable();
await CreateSqlServerTable();
await CreatePgTable();

TypeConverters.RegisterTypeConverter(typeof(JsonObj), JsonConvert.SerializeObject);

var list = Enumerable.Range(0, 100_000).Select(x => CreateOrUpdatePerson(x)).ToList();


var stopWatch = Stopwatch.StartNew();
await using var mysqlInsertConnection = MysqlConnect(); 
await mysqlInsertConnection.BulkInsertAsync(list);
Console.WriteLine($"Mysql.BulkInsertAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var postgreInsertConnection = PostgreConnect();
await postgreInsertConnection.BulkInsertAsync(list);
Console.WriteLine($"PostgreSQL.BulkInsertAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var sqlServerInsertConnection = SqlServerConnect();
await sqlServerInsertConnection.BulkInsertAsync(list);
Console.WriteLine($"SqlServer.BulkInsertAsync {list.Count} takes {stopWatch.Elapsed}");

var updated = list.Select(x => CreateOrUpdatePerson(0, x)).ToList();

stopWatch.Restart();
await using var mysqlInsertOrUpdateConnection = MysqlConnect(); 
await mysqlInsertOrUpdateConnection.BulkInsertOrUpdateAsync(updated);
Console.WriteLine($"Mysql.BulkInsertOrUpdateAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var postgreInsertOrUpdateConnection = PostgreConnect(); 
await postgreInsertOrUpdateConnection.BulkInsertOrUpdateAsync(updated);
Console.WriteLine($"PostgreSQL.BulkInsertOrUpdateAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var sqlServerInsertOrUpdateConnection = SqlServerConnect(); 
await sqlServerInsertOrUpdateConnection.BulkInsertOrUpdateAsync(updated);
Console.WriteLine($"SqlServer.BulkInsertOrUpdateAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var mysqlDeleteConnection = MysqlConnect(); 
await mysqlDeleteConnection.BulkDeleteAsync(list);
Console.WriteLine($"Mysql.BulkDeleteAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var postgreDeleteConnection = PostgreConnect(); 
await postgreDeleteConnection.BulkDeleteAsync(list);
Console.WriteLine($"PostgreSQL.BulkDeleteAsync {list.Count} takes {stopWatch.Elapsed}");

stopWatch.Restart();
await using var sqlServerDeleteConnection = SqlServerConnect(); 
await sqlServerDeleteConnection.BulkDeleteAsync(list);
Console.WriteLine($"SqlServer.BulkDeleteAsync {list.Count} takes {stopWatch.Elapsed}");

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

SqlConnection SqlServerConnect() => new SqlConnection(sqlServerConnectionString);

NpgsqlConnection PostgreConnect() => new NpgsqlConnection(pgsqlConnectionString);
MySqlConnection MysqlConnect() => new MySqlConnection(mysqlConnectionString);

async Task CreateSqlServerTable()
{
    await using var conn = SqlServerConnect();
    {
        conn.Execute($@"
            DROP TABLE IF EXISTS Person;
CREATE TABLE Person
(
    IdentityId INT IDENTITY(1,1) PRIMARY KEY,
    IntValue INT NULL,
    BigIntValue BIGINT NULL,
    DecimalValue DECIMAL(10,4) NULL,
    NvarcharValue NVARCHAR(255) NULL,
    FullName NVARCHAR(255) NULL,
    JsonObj NVARCHAR(MAX) NULL,  
    EnumValue INT NULL,
    BigTextValue NVARCHAR(MAX) NULL,
    CreateDate DATE NULL,
    GuidValue UNIQUEIDENTIFIER NULL
);");

    }

}

async Task CreateMysqlTable()
{
    await using var createNpgSql = MysqlConnect();
{
    createNpgSql.Execute($@"
            DROP TABLE IF EXISTS Person;
CREATE TABLE Person
(
    IdentityId INT AUTO_INCREMENT PRIMARY KEY,
    IntValue INT NULL,
    BigIntValue BIGINT NULL,
    DecimalValue DECIMAL(10,4) NULL,
    NvarcharValue VARCHAR(255) NULL,
    FullName VARCHAR(255) NULL,
    JsonObj JSON NULL, 
    EnumValue INT NULL,
    BigTextValue TEXT NULL,
    CreateDate DATE NULL,
    GuidValue CHAR(36) NULL 
);");

}

}


async Task CreatePgTable()
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
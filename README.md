# BulkyMerge - Fast Bulk Operations for .NET  
🚀 **Fast** BulkInsert, BulkUpdate, BulkInsertOrUpdate, BulkCopy, and BulkDelete extensions  

🔗 **Benchmark**: [dotnetfiddle.net/rKyO3Z](https://dotnetfiddle.net/rKyO3Z)  

![Performance Graph](https://github.com/user-attachments/assets/d2f1b9fc-e87c-44a6-b545-f0bf23b5c096)  

## ⚡ Performance Timing  

### For **10,000** items:  
- **Mysql.BulkInsertAsync**: ⏱ **638 ms**  
- **PostgreSQL.BulkInsertAsync**: ⏱ **392 ms**  
- **SqlServer.BulkInsertAsync**: ⏱ **412 ms**  

- **Mysql.BulkInsertOrUpdateAsync**: ⏱ **455 ms**  
- **PostgreSQL.BulkInsertOrUpdateAsync**: ⏱ **480 ms**  
- **SqlServer.BulkInsertOrUpdateAsync**: ⏱ **579 ms**  

- **Mysql.BulkDeleteAsync**: ⏱ **320 ms**  
- **PostgreSQL.BulkDeleteAsync**: ⏱ **200 ms**  
- **SqlServer.BulkDeleteAsync**: ⏱ **213 ms**  

### For **100,000** items:  
- **Mysql.BulkInsertAsync**: ⏱ **2.76 s**  
- **PostgreSQL.BulkInsertAsync**: ⏱ **1.84 s**  
- **SqlServer.BulkInsertAsync**: ⏱ **3.09 s**  

- **Mysql.BulkInsertOrUpdateAsync**: ⏱ **2.85 s**  
- **PostgreSQL.BulkInsertOrUpdateAsync**: ⏱ **1.32 s**  
- **SqlServer.BulkInsertOrUpdateAsync**: ⏱ **3.24 s**  

- **Mysql.BulkDeleteAsync**: ⏱ **1.97 s**  
- **PostgreSQL.BulkDeleteAsync**: ⏱ **993 ms**  
- **SqlServer.BulkDeleteAsync**: ⏱ **831 ms**  

---  

## 🚀 Simple Usage  

### Sample for **PostgreSQL**  
*(Works the same for MySQL and SQL Server)*  

```csharp
...
await using var insertConnection = new NpgsqlConnection(pgsqlConnectionString); // MysqlConenction or NpgsqlConnection
await insertConnection.BulkInsertAsync(list);

var updated = list.Select(x => CreateOrUpdatePerson(0, x)).ToList();

await using var insertOrUpdateConnection = new NpgsqlConnection(pgsqlConnectionString); // MysqlConenction or NpgsqlConnection
await insertOrUpdateConnection.BulkInsertOrUpdateAsync(updated);

await using var deleteConnection = new NpgsqlConnection(pgsqlConnectionString); // MysqlConenction or NpgsqlConnection
await deleteConnection.BulkDeleteAsync(list);
...
```

📂 **Check the `samples` folder for more examples!**  

---  

🔥 **Why Use BulkyMerge?**  
✅ High performance bulk operations  
✅ Supports PostgreSQL, MySQL, and SQL Server  
✅ Simple and intuitive API  
✅ Reduces database load and speeds up data processing  

# BulkyMerge - Fast Bulk Operations for .NET  
🚀 **Fast** BulkInsert, BulkUpdate, BulkInsertOrUpdate, BulkCopy, and BulkDelete extensions  

🔗 **Benchmark**: [dotnetfiddle.net/rKyO3Z](https://dotnetfiddle.net/rKyO3Z)  

![Performance Graph](https://github.com/user-attachments/assets/d2f1b9fc-e87c-44a6-b545-f0bf23b5c096)  

## ⚡ Performance Timing  

### For **10,000** items:  
- **Mysql.BulkInsertAsync**: ⏱ 00:00:00.6386250  
- **PostgreSQL.BulkInsertAsync**: ⏱ 00:00:00.3918789  
- **SqlServer.BulkInsertAsync**: ⏱ 00:00:00.4121973  

- **Mysql.BulkInsertOrUpdateAsync**: ⏱ 00:00:00.4553051  
- **PostgreSQL.BulkInsertOrUpdateAsync**: ⏱ 00:00:00.4802001  
- **SqlServer.BulkInsertOrUpdateAsync**: ⏱ 00:00:00.5790086  

- **Mysql.BulkDeleteAsync**: ⏱ 00:00:00.3198663  
- **PostgreSQL.BulkDeleteAsync**: ⏱ 00:00:00.2004163  
- **SqlServer.BulkDeleteAsync**: ⏱ 00:00:00.2130967  

### For **100,000** items:  
- **Mysql.BulkInsertAsync**: ⏱ 00:00:02.7574623  
- **PostgreSQL.BulkInsertAsync**: ⏱ 00:00:01.8421319  
- **SqlServer.BulkInsertAsync**: ⏱ 00:00:03.0949602  

- **Mysql.BulkInsertOrUpdateAsync**: ⏱ 00:00:02.8529519  
- **PostgreSQL.BulkInsertOrUpdateAsync**: ⏱ 00:00:01.3165880  
- **SqlServer.BulkInsertOrUpdateAsync**: ⏱ 00:00:03.2408191  

- **Mysql.BulkDeleteAsync**: ⏱ 00:00:01.9721236  
- **PostgreSQL.BulkDeleteAsync**: ⏱ 00:00:00.9930003  
- **SqlServer.BulkDeleteAsync**: ⏱ 00:00:00.8305408  

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


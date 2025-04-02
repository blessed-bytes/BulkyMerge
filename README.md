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

---


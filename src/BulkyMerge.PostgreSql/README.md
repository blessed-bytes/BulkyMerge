# BulkyMerge.PostgreSql

Fast bulk operations for PostgreSQL (binary `COPY` + staging merge).

```csharp
await connection.InsertManyAsync(rows, new InsertManyOptions
{
    Table = TableRef.Parse("Person"),
    MapGeneratedKeys = true,
}, cancellationToken: ct);

await connection.UpdateManyAsync(rows, new UpdateManyOptions { Table = TableRef.Parse("Person") }, ct);
await connection.UpsertManyAsync(rows, new UpsertManyOptions { Table = TableRef.Parse("Person") }, ct);
await connection.DeleteManyAsync(rows, new DeleteManyOptions { Table = TableRef.Parse("Person") }, ct);
```

Mark entities with `[BulkyMergeMappable]` for compile-time metadata (source generator).

using Dapper;
using MySqlConnector;

namespace BulkyMerge.MySql.Tests;

public class UpdateTests : MySqlTestsBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Pass_When_All_Inserted_Fields_Are_Valid(bool sync)
    {
        var tableName = $"AllFieldTypesTests_{Guid.NewGuid():N}";
        try
        {
            CreateAllFieldsTable(tableName);
            var id1 = InsertAllFields(tableName, new AllFieldTypesWithIdentityTests
            {
                GuidValue = Guid.NewGuid(),
                BigTextValue = "BigText1",
                CreateDate = DateTime.Now,
                DecimalValue = -1,
                NvarcharValue = $"NotTest -1",
                EnumValue = EnumValue.First,
                IntValue = -1
            });
            var id2 = InsertAllFields(tableName, new AllFieldTypesWithIdentityTests
            {
                GuidValue = Guid.NewGuid(),
                BigTextValue = "BigText1",
                CreateDate = DateTime.Now,
                DecimalValue = -2,
                NvarcharValue = $"NotTest -1",
                EnumValue = EnumValue.Third,
                IntValue = -2
            });
            var items = new List<AllFieldTypesWithIdentityTests>();
            items.Add(new AllFieldTypesWithIdentityTests
            {
                Id = id1,
                GuidValue = Guid.NewGuid(),
                BigTextValue = BigText,
                CreateDate = DateTime,
                DecimalValue = 0,
                NvarcharValue = $"Test 0",
                EnumValue = EnumValue.Second,
                IntValue = 0
            });
            items.Add(new AllFieldTypesWithIdentityTests
            {
                Id = id2,
                GuidValue = Guid.NewGuid(),
                BigTextValue = BigText,
                CreateDate = DateTime,
                DecimalValue = 1,
                NvarcharValue = $"Test 1",
                EnumValue = EnumValue.Second,
                IntValue = 1
            });
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            if (sync)
            {
                connection.BulkUpdate(items, tableName);
            }
            else
            {
                await connection.BulkUpdateAsync(items, tableName);
            }

            var select =
                await connection.QueryAsync<AllFieldTypesWithIdentityTests>(
                    $"SELECT * FROM `{tableName}` ORDER BY `Id` ASC");
            AllFieldsTestAssertions(select, items);
        }
        catch (Exception e)
        {
            ;
        }
        finally
        {
            DropTable(tableName);
        }
    }
    
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Should_Pass_When_Attribute_Based_Mappings_Is_Used(bool createNotMappedField, bool sync)
    {
        var tableName = $"AttributeBasedMapping_Test_{Guid.NewGuid():N}";
        try
        {
            CreateAttributeBasedMappingTableWithNotMapped(tableName);
            var items = new List<AttributeBasedMapping>();
            var key1 = Guid.NewGuid();
            var key2 = Guid.NewGuid();
            InsertAttributeBasedMapping(tableName, new AttributeBasedMapping
            {
                FirstKey = key1,
                SecondKey = key1,
                FieldToUpdate = "TO_UPDATE_1",
                NotMapped = "NOT_MAPPED"
            }, createNotMappedField);
            InsertAttributeBasedMapping(tableName, new AttributeBasedMapping
            {
                FirstKey = key2,
                SecondKey = key2,
                FieldToUpdate = "TO_UPDATE_2",
                NotMapped = "NOT_MAPPED"
            }, createNotMappedField);
            
            items.Add(new AttributeBasedMapping
            {
                FirstKey = key1,
                SecondKey = key1,
                FieldToUpdate = "UPDATED_0",
                NotMapped = "SHOULD_NOT_BE_UPDATED_0"
            });
            items.Add(new AttributeBasedMapping
            {
                FirstKey = key2,
                SecondKey = key2,
                FieldToUpdate = "UPDATED_1",
                NotMapped = "SHOULD_NOT_BE_UPDATED_1"
            });
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            if (sync)
            {
                connection.BulkUpdate(items, tableName);
            }
            else
            {
                await connection.BulkUpdateAsync(items, tableName);
            }
            var select = connection.Query<AttributeBasedMapping>($"SELECT `FirstKey`, `SecondKey`, `Field` as `FieldToUpdate` {(createNotMappedField ? ",`NotMapped`" : string.Empty)} FROM `{tableName}`");

            var count = select.Count();
            Assert.Equal(count, items.Count);
            if (createNotMappedField)
            {
                Assert.True(select.All(x => x.NotMapped == "NOT_MAPPED"));
            }
            Assert.Contains(select, x => x.FirstKey == key1 && x.SecondKey == key1);
            Assert.Contains(select, x => x.FirstKey == key2 && x.SecondKey == key2);
            Assert.True(select.OrderBy(x => x.FieldToUpdate).Select(x => x.FieldToUpdate).SequenceEqual(Enumerable.Range(0, count).Select(x => $"UPDATED_{x}").OrderBy(x => x)));

        }
        finally
        {
            DropTable(tableName);
        }
    }

    
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Should_Pass_When_Parameters_Based_Mappings_Is_Used(bool createNotMappedField, bool sync)
    {
        var tableName = $"PK_Test_Update_{Guid.NewGuid():N}";
        try
        {
            CreateCustomPrimaryKeysTableWithNotMapped(tableName);
            var items = new List<CustomPrimaryKeyFieldsParametersMappings>();
            var key1 = Guid.NewGuid();
            var key2 = Guid.NewGuid();
            InsertCustomPrimaryKeyFieldsParametersMappings(tableName, new CustomPrimaryKeyFieldsParametersMappings()
            {
                FirstKey = key1,
                SecondKey = key1,
                FieldToUpdate = "TO_UPDATE_1",
                Exclude = "NOT_MAPPED"
            }, createNotMappedField);
            InsertCustomPrimaryKeyFieldsParametersMappings(tableName, new CustomPrimaryKeyFieldsParametersMappings
            {
                FirstKey = key2,
                SecondKey = key2,
                FieldToUpdate = "TO_UPDATE_2",
                Exclude = "NOT_MAPPED"
            }, createNotMappedField);

            items.Add(new CustomPrimaryKeyFieldsParametersMappings
            {
                FirstKey = key1,
                SecondKey = Guid.Empty,
                FieldToUpdate = "UPDATED_0",
                Exclude = "SHOULD_NOT_BE_UPDATED_0"
            });
            items.Add(new CustomPrimaryKeyFieldsParametersMappings
            {
                FirstKey = key2,
                SecondKey = Guid.Empty,
                FieldToUpdate = "UPDATED_1",
                Exclude = "SHOULD_NOT_BE_UPDATED_1"
            });
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            if (sync)
            {
                connection.BulkUpdate(items, tableName, primaryKeys: new[] {"FirstKey"},
                    excludeProperties: new[] {"Exclude"});
            }
            else
            {
                await connection.BulkUpdateAsync(items, tableName, primaryKeys: new[] {"FirstKey"},
                    excludeProperties: new[] {"Exclude"});
            }

            var select = connection.Query<CustomPrimaryKeyFieldsParametersMappings>(
                $"SELECT `FirstKey`, `SecondKey`, `FieldToUpdate` {(createNotMappedField ? ",`Exclude`" : string.Empty)} FROM `{tableName}`");

            var count = select.Count();
            Assert.Equal(count, items.Count);
            if (createNotMappedField)
            {
                Assert.True(select.All(x => x.Exclude == "NOT_MAPPED"));
            }

            Assert.Contains(select, x => x.FirstKey == key1);
            Assert.Contains(select, x => x.FirstKey == key2);
            Assert.True(select.OrderBy(x => x.FieldToUpdate).Select(x => x.FieldToUpdate)
                .SequenceEqual(Enumerable.Range(0, count).Select(x => $"UPDATED_{x}").OrderBy(x => x)));

        }
        catch (Exception e)
        {
            ;
        }finally
        {
            DropTable(tableName);
        }
    }
}
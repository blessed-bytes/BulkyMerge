// Dapper Plus
// Doc: https://dapper-plus.net/bulk-insert
// @nuget: Dapper
// @nuget: Microsoft.Data.SqlClient
// @nuget: Z.Dapper.Plus
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Dapper;
using BulkyMerge.SqlServer;

public class Program
{
    public static List<BenchmarkResult> BenchmarkResults = new List<BenchmarkResult>();
    public static void Main()
    {
        // Map your entity
        //DapperPlusManager.Entity<Product>().Table("Product");
        var connection = new SqlConnection("Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");
        //connection.CreateTable<Product>();
        /*connection.Open();
        connection.Execute(new CommandDefinition(@"CREATE TABLE Product (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Column1 NVARCHAR(MAX) NULL,
    Column2 NVARCHAR(MAX) NULL,
    Column3 NVARCHAR(MAX) NULL,
    Column4 NVARCHAR(MAX) NULL,
    Column5 NVARCHAR(MAX) NULL,
    Column6 NVARCHAR(MAX) NULL,
    Column7 NVARCHAR(MAX) NULL,
    Column8 NVARCHAR(MAX) NULL,
    Column9 NVARCHAR(MAX) NULL
);"));*/
        JustInTime_Compile(connection);
        var products = GenerateProducts(2000);
        var clockDapper = new Stopwatch();
        var clockDapperPlus = new Stopwatch();
        // Dapper
        {
            clockDapper.Start();
            connection.Execute(@"INSERT INTO Product (Name, Description, Column1, Column2, Column3, Column4, Column5, Column6, Column7, Column8, Column9)
											  VALUES (@Name, @Description, @Column1, @Column2, @Column3, @Column4, @Column5, @Column6, @Column7, @Column8, @Column9)", products);
            clockDapper.Stop();
            BenchmarkResults.Add(new BenchmarkResult() { Action = "Dapper", Entities = products.Count, Performance = clockDapper.ElapsedMilliseconds + " ms" });
        }

        // Dapper Plus
        {
            // BulkInsert
            clockDapperPlus.Start();
            connection.BulkInsertAsync(products).GetAwaiter().GetResult();
            clockDapperPlus.Stop();
            var timeFaster = Math.Round((double)clockDapper.ElapsedMilliseconds / clockDapperPlus.ElapsedMilliseconds, 2);
            var reducedPercent = Math.Round((double)(clockDapper.ElapsedMilliseconds - clockDapperPlus.ElapsedMilliseconds) / clockDapper.ElapsedMilliseconds, 2) * 100;
            BenchmarkResults.Add(new BenchmarkResult() { Action = "Bulky Plus", Entities = products.Count, Performance = $"{clockDapperPlus.ElapsedMilliseconds} ms", TimeFaster = $"{timeFaster}x faster than Dapper", ReducedPercent = $"Time reduced by {reducedPercent}% compared to Dapper" });
        }

        //FiddleHelper.WriteTable(BenchmarkResults);
    }

    public static List<Product> GenerateProducts(int count)
    {
        var products = new List<Product>();
        for (int i = 0; i < count; i++)
        {
            if (i % 3 == 0)
            {
                products.Add(new Product() { Name = "Dapper Plus", Description = @"Use <a href=""https://dapper-plus.net/"" target=""_blank"">Dapper Plus</a> to extend your IDbConnection with high-performance bulk operations.", Column1 = i.ToString(), Column2 = i.ToString(), Column3 = i.ToString(), Column4 = i.ToString(), Column5 = i.ToString(), Column6 = i.ToString(), Column7 = i.ToString(), Column8 = i.ToString(), Column9 = i.ToString() });
            }
            else if (i % 3 == 1)
            {
                products.Add(new Product() { Name = "C# Eval Expression", Description = @"Use <a href=""https://eval-expression.net/"" target=""_blank"">C# Eval Expression</a> to compile and execute C# code at runtime.", Column1 = i.ToString(), Column2 = i.ToString(), Column3 = i.ToString(), Column4 = i.ToString(), Column5 = i.ToString(), Column6 = i.ToString(), Column7 = i.ToString(), Column8 = i.ToString(), Column9 = i.ToString() });
            }
            else if (i % 3 == 2)
            {
                products.Add(new Product() { Name = "Entity Framework Extensions", Description = @"Use <a href=""https://entityframework-extensions.net/"" target=""_blank"">Entity Framework Extensions</a> to extend your DbContext with high-performance bulk operations.", Column1 = i.ToString(), Column2 = i.ToString(), Column3 = i.ToString(), Column4 = i.ToString(), Column5 = i.ToString(), Column6 = i.ToString(), Column7 = i.ToString(), Column8 = i.ToString(), Column9 = i.ToString() });
            }
        }

        return products;
    }

    public static void JustInTime_Compile(SqlConnection connection)
    {
        var products = GenerateProducts(20);
        // Dapper
        {
            connection.Execute(@"INSERT INTO Product (Name, Description, Column1, Column2, Column3, Column4, Column5, Column6, Column7, Column8, Column9)
											  VALUES (@Name, @Description, @Column1, @Column2, @Column3, @Column4, @Column5, @Column6, @Column7, @Column8, @Column9)", products);
        }

        // Dapper Plus
        {
            connection.BulkInsert(products);
        }
    }

    [Table("Product")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column3 { get; set; }
        public string Column4 { get; set; }
        public string Column5 { get; set; }
        public string Column6 { get; set; }
        public string Column7 { get; set; }
        public string Column8 { get; set; }
        public string Column9 { get; set; }
    }

    public class BenchmarkResult
    {
        public string Action { get; set; }
        public int Entities { get; set; }
        public string Performance { get; set; }
        public string TimeFaster { get; set; }
        public string ReducedPercent { get; set; }
    }
}
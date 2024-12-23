using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

public class SchemaComparer
{
    public static bool CompareSchemas(string originalDbPath, string backupDbPath)
    {
        try
        {
            using (var originalConn = new SqliteConnection($"Data Source={originalDbPath}"))
            using (var backupConn = new SqliteConnection($"Data Source={backupDbPath}"))
            {
                originalConn.Open();
                backupConn.Open();

                // الحصول على قائمة الجداول في كلا قاعدتي البيانات
                var originalTables = GetTableNames(originalConn);
                var backupTables = GetTableNames(backupConn);

                // مقارنة عدد الجداول وأسمائها
                if (!originalTables.SequenceEqual(backupTables))
                {
                    Console.WriteLine("عدد أو أسماء الجداول مختلفة.");
                    return false;
                }

                // مقارنة بنية كل جدول
                foreach (var tableName in originalTables)
                {
                    if (!CompareTableSchemas(originalConn, backupConn, tableName))
                    {
                        Console.WriteLine($"بنية الجدول {tableName} مختلفة.");
                        return false;
                    }
                }

                return true; // البنيتان متطابقتان
            }
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"خطأ في قاعدة البيانات: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ غير متوقع: {ex.Message}");
            return false;
        }
    }

    private static List<string> GetTableNames(SqliteConnection connection)
    {
        List<string> tableNames = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }
        }
        return tableNames.OrderBy(n => n).ToList(); // ترتيب الأسماء للمقارنة
    }

    private static bool CompareTableSchemas(SqliteConnection originalConn, SqliteConnection backupConn, string tableName)
    {
        var originalColumns = GetColumnInfo(originalConn, tableName);
        var backupColumns = GetColumnInfo(backupConn, tableName);

        if (originalColumns.Count != backupColumns.Count)
        {
            Console.WriteLine($"عدد الأعمدة في الجدول {tableName} مختلف.");
            Console.WriteLine($"عدد الأعمدة في النسخة الأصلية : {originalColumns.Count} وعددها في النسخة الاحتياطية : {backupColumns.Count}");
            return false;
        }
        return originalColumns.SequenceEqual(backupColumns);
    }

    private static List<ColumnInfo> GetColumnInfo(SqliteConnection connection, string tableName)
    {
        List<ColumnInfo> columns = new List<ColumnInfo>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info({tableName})";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var INF = new ColumnInfo();
                    INF.Name = reader.IsDBNull(0) ? null : reader.GetString(0); // معالجة قيم NULL
                    INF.Type = reader.IsDBNull(1) ? null : reader.GetString(1); // معالجة قيم NULL
                    INF.NotNull = !reader.IsDBNull(2) && reader.GetInt32(2) == 1;
                    INF.DefaultValue = reader.IsDBNull(3) ? null : reader.GetValue(3);
                    INF.PrimaryKey = !reader.IsDBNull(4) && reader.GetInt32(4) == 1;
                    INF.Collation = reader.IsDBNull(5) ? null : reader.GetString(5);
                    columns.Add(INF);
                }
            }
        }
        return columns.OrderBy(c => c.Name).ToList(); // ترتيب الأعمدة حسب الاسم
    }

    public class ColumnInfo : IEquatable<ColumnInfo>
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool NotNull { get; set; }
        public object DefaultValue { get; set; }
        public bool PrimaryKey { get; set; }
        public string Collation { get; set; }

        public bool Equals(ColumnInfo other)
        {
            if (other == null) return false;

            return Name == other.Name &&
                   Type == other.Type &&
                   NotNull == other.NotNull &&
                   Equals(DefaultValue, other.DefaultValue) && // مقارنة القيم الافتراضية
                   PrimaryKey == other.PrimaryKey &&
                   Collation == other.Collation;
        }

        public override bool Equals(object obj) => Equals(obj as ColumnInfo);
        public override int GetHashCode() => (Name, Type, NotNull, DefaultValue, PrimaryKey, Collation).GetHashCode();
    }
}


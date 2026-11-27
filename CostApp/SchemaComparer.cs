using Microsoft.Data.Sqlite;

namespace CostApp
{
    public static class SchemaComparer
    {
        private static readonly string[] RequiredCoreTables = ["TreeItem", "DetailItem"];

        public static bool HasRequiredTables(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                return false;

            try
            {
                using var conn = new SqliteConnection(
                    new SqliteConnectionStringBuilder { DataSource = dbPath, Mode = SqliteOpenMode.ReadOnly }.ToString());
                conn.Open();

                var tables = GetTableNames(conn);
                foreach (var required in RequiredCoreTables)
                {
                    if (!tables.Contains(required, StringComparer.OrdinalIgnoreCase))
                        return false;
                }

                // Verify core columns exist in TreeItem and DetailItem
                var treeCols = GetColumnNames(conn, "TreeItem");
                var detailCols = GetColumnNames(conn, "DetailItem");

                return treeCols.Contains("id", StringComparer.OrdinalIgnoreCase)
                    && treeCols.Contains("Title", StringComparer.OrdinalIgnoreCase)
                    && detailCols.Contains("id", StringComparer.OrdinalIgnoreCase)
                    && detailCols.Contains("ParentID", StringComparer.OrdinalIgnoreCase)
                    && detailCols.Contains("Date", StringComparer.OrdinalIgnoreCase)
                    && detailCols.Contains("Amount", StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static List<string> GetTableNames(SqliteConnection connection)
        {
            var tableNames = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tableNames.Add(reader.GetString(0));
            }
            return tableNames;
        }

        private static List<string> GetColumnNames(SqliteConnection connection, string tableName)
        {
            var columns = new List<string>();
            using var command = connection.CreateCommand();
            // PRAGMA table_info returns: 0=cid, 1=name, 2=type, 3=notnull, 4=dflt_value, 5=pk
            command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(1))
                    columns.Add(reader.GetString(1));
            }
            return columns;
        }
    }
}

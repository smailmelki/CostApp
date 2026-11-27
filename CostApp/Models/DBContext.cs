using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CostApp.Models
{
    public class DBContext : DbContext
    {
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<DetailItem> DetailItem { get; set; }
        public virtual DbSet<TreeItem> TreeItem { get; set; }

        public static string GetDatabasePath()
        {
            string appDataDir;
            try
            {
                appDataDir = FileSystem.AppDataDirectory;
            }
            catch
            {
                appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            if (!string.IsNullOrWhiteSpace(appDataDir))
                Directory.CreateDirectory(appDataDir);

            var targetPath = Path.Combine(appDataDir, "CostAppDB.db");

            // Preserve existing database from previous versions that used SpecialFolder.ApplicationData
            if (!File.Exists(targetPath))
            {
                try
                {
                    var legacyDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    if (!string.IsNullOrWhiteSpace(legacyDir))
                    {
                        var legacyPath = Path.Combine(legacyDir, "CostAppDB.db");
                        if (File.Exists(legacyPath) &&
                            !string.Equals(Path.GetFullPath(legacyPath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(legacyPath, targetPath, overwrite: false);
                        }
                    }
                }
                catch
                {
                    // Best-effort legacy migration; do not block startup if legacy folder is inaccessible.
                }
            }

            return targetPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite($"Data Source={GetDatabasePath()}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Projects");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("Name");
                entity.Property(e => e.Description).HasMaxLength(300).HasColumnName("Description");
                entity.Property(e => e.Budget).IsRequired().HasDefaultValue(0m).HasColumnName("Budget");
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("CreatedAt");
                entity.Ignore(p => p.TotalSpent);
                entity.Ignore(p => p.ExpenseCount);
                entity.Ignore(p => p.RemainingBudget);
                entity.Ignore(p => p.HasBudget);
            });

            modelBuilder.Entity<DetailItem>(entity =>
            {
                entity.ToTable("DetailItem");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.ParentID).IsRequired().HasColumnName("ParentID");
                entity.Property(e => e.Date).IsRequired().HasColumnName("Date");
                entity.Property(e => e.Amount).IsRequired().HasColumnName("Amount");
                entity.Property(e => e.Note).HasMaxLength(250).HasColumnName("Note");
                entity.Ignore(p => p.CategoryTitle);
            });

            modelBuilder.Entity<TreeItem>(entity =>
            {
                entity.ToTable("TreeItem");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.ProjectID).IsRequired().HasDefaultValue(1).HasColumnName("ProjectID");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(50).HasColumnName("Title");
                entity.Ignore(p => p.Details);
                entity.Ignore(p => p.Total);
            });
        }

        /// <summary>
        /// Safely initializes or migrates an existing CostApp database without losing any existing records.
        /// </summary>
        public static void EnsureDatabaseReady()
        {
            using var context = new DBContext();
            context.Database.EnsureCreated();

            var dbPath = GetDatabasePath();
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var transaction = connection.BeginTransaction();

            // 1. Ensure Projects table exists (for upgrades from single-project DBs)
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Projects (
                        id INTEGER NOT NULL CONSTRAINT PK_Projects PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT NULL,
                        Budget TEXT NOT NULL DEFAULT '0',
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                    );";
                cmd.ExecuteNonQuery();
            }

            // 2. Ensure TreeItem has ProjectID column (for upgrades from single-project DBs)
            bool hasProjectIdColumn = false;
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = "PRAGMA table_info(TreeItem);";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(1) &&
                        string.Equals(reader.GetString(1), "ProjectID", StringComparison.OrdinalIgnoreCase))
                    {
                        hasProjectIdColumn = true;
                        break;
                    }
                }
            }

            if (!hasProjectIdColumn)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.Transaction = transaction;
                alterCmd.CommandText = "ALTER TABLE TreeItem ADD COLUMN ProjectID INTEGER NOT NULL DEFAULT 1;";
                alterCmd.ExecuteNonQuery();
            }

            // 3. Ensure at least one default project exists and orphaned TreeItems are linked to it
            long projectCount;
            using (var countCmd = connection.CreateCommand())
            {
                countCmd.Transaction = transaction;
                countCmd.CommandText = "SELECT COUNT(*) FROM Projects;";
                projectCount = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
            }

            long defaultProjectId;
            if (projectCount == 0)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO Projects (Name, Description, Budget, CreatedAt)
                    VALUES (@name, @desc, '0', @createdAt);
                    SELECT last_insert_rowid();";
                insertCmd.Parameters.AddWithValue("@name", "المشروع الرئيسي");
                insertCmd.Parameters.AddWithValue("@desc", "المشروع الافتراضي");
                insertCmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                defaultProjectId = Convert.ToInt64(insertCmd.ExecuteScalar() ?? 1);
            }
            else
            {
                using var firstCmd = connection.CreateCommand();
                firstCmd.Transaction = transaction;
                firstCmd.CommandText = "SELECT id FROM Projects ORDER BY id ASC LIMIT 1;";
                defaultProjectId = Convert.ToInt64(firstCmd.ExecuteScalar() ?? 1);
            }

            // Re-link any orphaned TreeItem rows (e.g. ProjectID <= 0 or referencing a missing project)
            using (var fixCmd = connection.CreateCommand())
            {
                fixCmd.Transaction = transaction;
                fixCmd.CommandText = @"
                    UPDATE TreeItem
                    SET ProjectID = @defaultId
                    WHERE ProjectID <= 0
                       OR ProjectID NOT IN (SELECT id FROM Projects);";
                fixCmd.Parameters.AddWithValue("@defaultId", defaultProjectId);
                fixCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}

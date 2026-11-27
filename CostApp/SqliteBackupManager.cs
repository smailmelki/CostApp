using CommunityToolkit.Maui.Storage;
using CostApp.Models;
using Microsoft.Data.Sqlite;

namespace CostApp
{
    public static class SqliteBackupManager
    {
        public static async Task<string?> PickBackupFileAsync()
        {
            try
            {
                var file = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "اختر ملف النسخة الاحتياطية",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".db" } },
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS, new[] { "db" } },
                        { DevicePlatform.MacCatalyst, new[] { "db" } }
                    })
                });

                if (file == null)
                    return null;

                // Android providers often return content:// URIs rather than a real filesystem path.
                var localPath = Path.Combine(FileSystem.CacheDirectory,
                    $"CostApp_import_{Guid.NewGuid():N}.db");

                await using var input = await file.OpenReadAsync();
                await using var output = File.Create(localPath);
                await input.CopyToAsync(output);
                return localPath;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> ExportBackupAsync(string sourcePath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return false;

            var tempBackup = Path.Combine(FileSystem.CacheDirectory, $"CostApp_export_{Guid.NewGuid():N}.db");
            try
            {
                if (!await BackupDatabaseAsync(sourcePath, tempBackup))
                    return false;

                await using var stream = File.OpenRead(tempBackup);
                var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
                return result.IsSuccessful;
            }
            catch (Exception ex)
            {
                await DialogService.DisplayAlertAsync("خطأ",
                    $"تعذر حفظ النسخة الاحتياطية: {ex.Message}", "موافق");
                return false;
            }
            finally
            {
                TryDeleteFile(tempBackup);
            }
        }

        public static bool IsValidDatabase(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            try
            {
                SqliteConnection.ClearAllPools();
                using var connection = new SqliteConnection(
                    new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly }.ToString());
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check;";
                return string.Equals(Convert.ToString(command.ExecuteScalar()), "ok",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> BackupDatabaseAsync(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return false;
            if (string.IsNullOrWhiteSpace(destinationPath))
                return false;

            var sourceFullPath = Path.GetFullPath(sourcePath);
            var destinationFullPath = Path.GetFullPath(destinationPath);
            if (string.Equals(sourceFullPath, destinationFullPath,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return false;

            var directory = Path.GetDirectoryName(destinationFullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            try
            {
                SqliteConnection.ClearAllPools();
                using var source = new SqliteConnection($"Data Source={sourceFullPath}");
                using var destination = new SqliteConnection($"Data Source={destinationFullPath}");
                await source.OpenAsync();
                await destination.OpenAsync();
                source.BackupDatabase(destination);
                return IsValidDatabase(destinationFullPath);
            }
            catch (Exception ex)
            {
                await DialogService.DisplayAlertAsync("خطأ",
                    $"تعذر إتمام النسخ أو الاستعادة: {ex.Message}", "موافق");
                return false;
            }
        }

        public static async Task<(bool success, string message)> RestoreDatabaseSafelyAsync(string backupFilePath)
        {
            var targetDbPath = DBContext.GetDatabasePath();
            if (!IsValidDatabase(backupFilePath))
                return (false, "الملف المحدد ليس قاعدة بيانات SQLite سليمة.");

            if (!SchemaComparer.HasRequiredTables(backupFilePath))
                return (false, "الملف المحدد لا يحتوي على جداول تطبيق CostApp المطلوبة.");

            var safetyCopy = Path.Combine(FileSystem.CacheDirectory,
                $"CostApp_before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            if (File.Exists(targetDbPath))
            {
                if (!await BackupDatabaseAsync(targetDbPath, safetyCopy))
                    return (false, "تعذر إنشاء نسخة أمان قبل الاستعادة؛ لم يتم تعديل البيانات.");
            }

            try
            {
                SqliteConnection.ClearAllPools();
                bool restored = await BackupDatabaseAsync(backupFilePath, targetDbPath);
                if (!restored)
                {
                    if (File.Exists(safetyCopy))
                        await BackupDatabaseAsync(safetyCopy, targetDbPath);
                    return (false, "فشلت عملية الاستعادة، وتم الاحتفاظ بالبيانات الأصلية.");
                }

                // Upgrade restored DB safely if it came from an older single-project version
                DBContext.EnsureDatabaseReady();
                return (true, "تمت استعادة قاعدة البيانات بنجاح.");
            }
            catch (Exception ex)
            {
                if (File.Exists(safetyCopy))
                    await BackupDatabaseAsync(safetyCopy, targetDbPath);
                return (false, $"حدث خطأ أثناء الاستعادة وتم التراجع للنسخة الآمنة: {ex.Message}");
            }
        }

        public static void TryDeleteFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Ignore temp cleanup errors
            }
        }
    }
}

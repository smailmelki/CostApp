using CommunityToolkit.Maui.Storage;
using Microsoft.Data.Sqlite;

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

            // Android providers may return content:// URIs rather than a real filesystem path.
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

    public static async Task<string?> PickBackupFolderAsync()
    {
        try
        {
            var result = await FolderPicker.PickAsync(CancellationToken.None);
            return result?.Folder?.Path;
        }
        catch (Exception ex)
        {
            await DialogService.DisplayAlertAsync("خطأ",
                $"تعذر اختيار مجلد النسخ الاحتياطي: {ex.Message}", "موافق");
            return null;
        }
    }

    public static bool IsValidDatabase(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
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
}
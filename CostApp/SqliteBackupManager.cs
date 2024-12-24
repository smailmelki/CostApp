using CommunityToolkit.Maui.Storage;
using Microsoft.Data.Sqlite;
using CostApp;

public static class SqliteBackupManager
{

    /// <summary>
    /// يتيح للمستخدم اختيار ملف النسخة الاحتياطية.
    /// </summary>
    /// <returns>مسار الملف أو null إذا تم الإلغاء.</returns>
    public static async Task<string?> PickBackupFileAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { "db"} },
                    { DevicePlatform.Android, new[] { "*/*" } },
                    { DevicePlatform.iOS, new[] { "db" } },
                    { DevicePlatform.MacCatalyst, new[] { "db" } }
                }),
                PickerTitle = "اختر ملف النسخة الاحتياطية",
            });

            if (result == null)
                return null;
            return result.FullPath;
        }
        catch (Exception )
        {
            return null;
        }
    }


    /// <summary>
    /// يتيح للمستخدم اختيار مجلد لحفظ النسخة الاحتياطية.
    /// </summary>
    /// <returns>مسار المجلد أو null إذا تم الإلغاء.</returns>
    public static async Task<string?> PickBackupFolderAsync()
    {
        try
        {
            var result = await FolderPicker.PickAsync(CancellationToken.None);
            return result?.Folder?.Path;
        }
        catch (Exception ex)
        {
            await DialogService.DisplayAlertAsync("خطأ", $"خطأ أثناء اختيار مجلد النسخ الاحتياطي: {ex.Message}", "موافق");
            return null;
        }
    }


    /// <summary>
    /// يقوم بإنشاء نسخة احتياطية من قاعدة بيانات SQLite بطريقة احترافية.
    /// </summary>
    /// <param name="SourceOriginPath">المسار الكامل لقاعدة البيانات الأصلية.</param>
    /// <param name="DestinationPath">المسار لحفظ النسخة الاحتياطية (مجلد أو مسار ملف كامل).</param>
    /// <returns>مسار ملف النسخة الاحتياطية أو null في حالة الفشل.</returns>
    public static async Task<bool> BackupDatabaseAsync(string SourceOriginPath, string DestinationPath)
    {
        if (string.IsNullOrWhiteSpace(SourceOriginPath))
            throw new ArgumentNullException(nameof(SourceOriginPath), "مسار قاعدة البيانات الأصلية غير صالح.");

        if (!File.Exists(SourceOriginPath))
            throw new FileNotFoundException("لم يتم العثور على ملف قاعدة البيانات.", SourceOriginPath);

        if (string.IsNullOrWhiteSpace(DestinationPath))
            throw new ArgumentNullException(nameof(DestinationPath), "مسار النسخ الاحتياطي غير صالح.");

        string? backupDirectory = Path.GetDirectoryName(DestinationPath);
        if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
            Directory.CreateDirectory(backupDirectory);

        try
        {
            // استخدام اتصال SQLite لنسخ قاعدة البيانات (أكثر موثوقية)
            using (var sourceConnection = new SqliteConnection($"Data Source={SourceOriginPath}"))
            using (var destinationConnection = new SqliteConnection($"Data Source={DestinationPath}"))
            {
                await sourceConnection.OpenAsync();
                await destinationConnection.OpenAsync();
                sourceConnection.BackupDatabase(destinationConnection);
            }

            return true;
        }
        catch (SqliteException ex)
        {
            await DialogService.DisplayAlertAsync("خطأ", $"خطأ SQLite أثناء إنشاء النسخة الاحتياطية: {ex.Message} (ErrorCode: {ex.SqliteErrorCode})", "موافق");
            // يمكنك هنا معالجة أخطاء SQLite بشكل أكثر تحديدًا بناءً على رمز الخطأ
            return false; // ارجع null للإشارة إلى الفشل
        }
        catch (IOException ex)
        {
            await DialogService.DisplayAlertAsync("خطأ", $"خطأ إدخال/إخراج أثناء إنشاء النسخة الاحتياطية: {ex.Message}", "موافق");
            return false;
        }
        catch (Exception ex)
        {
            await DialogService.DisplayAlertAsync("خطأ", $"خطأ غير متوقع أثناء إنشاء النسخة الاحتياطية: {ex.Message}", "موافق");
            return false;
        }
    }
}
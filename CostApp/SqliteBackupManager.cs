using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Maui.Storage;

public static class SqliteBackupManager
{
    /// <summary>
    /// يستعيد قاعدة بيانات SQLite من ملف النسخة الاحتياطية المحدد.
    /// </summary>
    /// <param name="context">كائن DbContext لإدارة قاعدة البيانات.</param>
    /// <param name="backupPath">المسار إلى ملف النسخة الاحتياطية.</param>
    /// <returns>نتيجة العملية (ناجحة أو فاشلة).</returns>
    public static async Task<bool> RestoreDataAsync(DbContext context, string backupPath)
    {
        try
        {
            string databasePath = context.Database.GetDbConnection().DataSource;

            if (string.IsNullOrWhiteSpace(backupPath))
                throw new ArgumentException("مسار النسخة الاحتياطية غير صالح.");

            if (!File.Exists(backupPath))
                throw new FileNotFoundException("لم يتم العثور على ملف النسخة الاحتياطية.", backupPath);

            if (!File.Exists(databasePath))
                throw new FileNotFoundException("لم يتم العثور على ملف قاعدة البيانات.", databasePath);

            await context.Database.CloseConnectionAsync();
            await context.DisposeAsync();

            File.Copy(backupPath, databasePath, overwrite: true);

            Console.WriteLine("تم استعادة قاعدة البيانات بنجاح.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ أثناء استعادة قاعدة البيانات: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// استعادة ملف قاعدة البيانات باستخدام تيار البيانات.
    /// </summary>
    /// <param name="dbFileStream">تيار الملف الخاص بقاعدة البيانات الاحتياطية.</param>
    /// <returns>إرجاع true إذا نجحت العملية، وfalse إذا فشلت.</returns>
    public static async Task<bool> RestoreBdFileAsync(Stream dbFileStream ,DbContext context)
    {
        try
        {
            // الحصول على مسار قاعدة البيانات
            string databasePath = context.Database.GetDbConnection().DataSource;
            if (File.Exists(databasePath))
                context.Database.EnsureDeleted();

            // استبدال قاعدة البيانات الحالية بالنسخة الاحتياطية
            using var fs = File.Create(databasePath);
            await dbFileStream.CopyToAsync(fs);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ أثناء استعادة قاعدة البيانات: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// يقوم بإنشاء نسخة احتياطية من قاعدة بيانات SQLite في الموقع المحدد.
    /// </summary>
    /// <param name="context">كائن DbContext لإدارة قاعدة البيانات.</param>
    /// <param name="backupPath">المسار لحفظ النسخة الاحتياطية.</param>
    /// <returns>نتيجة العملية (ناجحة أو فاشلة).</returns>
    public static async Task<bool> BackupDatabaseAsync(DbContext context, string backupPath)
    {
        try
        {
            string databasePath = context.Database.GetDbConnection().DataSource;
            string backupFilePath = Path.Combine(backupPath, GenerateBackupFileName());

            if (string.IsNullOrWhiteSpace(backupPath))
                throw new ArgumentException("مسار النسخ الاحتياطي غير صالح.");

            if (!File.Exists(databasePath))
                throw new FileNotFoundException("لم يتم العثور على ملف قاعدة البيانات.", databasePath);

            string? backupDirectory = Path.GetDirectoryName(backupFilePath);
            if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);

            File.Copy(databasePath, backupFilePath, overwrite: true);

            Console.WriteLine($"تم إنشاء النسخة الاحتياطية بنجاح في {backupFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ أثناء إنشاء النسخة الاحتياطية: {ex.Message}");
            return false;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
            context.Dispose();
        }
    }

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
                    { DevicePlatform.WinUI, new[] { "db" } },
                    { DevicePlatform.Android, new[] { "*/*" } },
                    { DevicePlatform.iOS, new[] { "db" } },
                    { DevicePlatform.MacCatalyst, new[] { "db" } }
                }),
                PickerTitle = "اختر ملف النسخة الاحتياطية"
            });

            return result.FullPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ أثناء اختيار ملف النسخة الاحتياطية: {ex.Message}");
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
            Console.WriteLine($"خطأ أثناء اختيار مجلد النسخ الاحتياطي: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// ينشئ اسمًا لملف النسخة الاحتياطية بناءً على الوقت الحالي.
    /// </summary>
    /// <returns>اسم الملف.</returns>
    public static string GenerateBackupFileName()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"backup_{timestamp}.db";
    }
}

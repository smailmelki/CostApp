using Microsoft.Data.Sqlite; // استخدام مكتبة Sqlite المباشرة
using System.IO;
using System.Threading.Tasks;

public static class DatabaseBackup
{
    /// <summary>
    /// يقوم بإنشاء نسخة احتياطية من قاعدة بيانات SQLite بطريقة احترافية.
    /// </summary>
    /// <param name="databasePath">المسار الكامل لقاعدة البيانات الأصلية.</param>
    /// <param name="backupPath">المسار لحفظ النسخة الاحتياطية (مجلد أو مسار ملف كامل).</param>
    /// <param name="backupFileName">اسم ملف النسخة الاحتياطية (اختياري، يتم إنشاء اسم افتراضي إذا لم يتم تحديده).</param>
    /// <returns>مسار ملف النسخة الاحتياطية أو null في حالة الفشل.</returns>
    public static async Task<string?> BackupDatabaseAsync(string databasePath, string backupPath, string? backupFileName = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentNullException(nameof(databasePath), "مسار قاعدة البيانات الأصلية غير صالح.");

        if (!File.Exists(databasePath))
            throw new FileNotFoundException("لم يتم العثور على ملف قاعدة البيانات.", databasePath);

        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentNullException(nameof(backupPath), "مسار النسخ الاحتياطي غير صالح.");

        if (string.IsNullOrEmpty(backupFileName))
        {
            backupFileName = GenerateBackupFileName();
        }

        string backupFilePath = Path.Combine(backupPath, backupFileName);

        string? backupDirectory = Path.GetDirectoryName(backupFilePath);
        if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
            Directory.CreateDirectory(backupDirectory);

        try
        {
            // استخدام اتصال SQLite لنسخ قاعدة البيانات (أكثر موثوقية)
            using (var sourceConnection = new SqliteConnection($"Data Source={databasePath}"))
            using (var destinationConnection = new SqliteConnection($"Data Source={backupFilePath}"))
            {
                await sourceConnection.OpenAsync();
                await destinationConnection.OpenAsync();

                sourceConnection.BackupDatabase(destinationConnection);
            }

            //Console.WriteLine($"تم إنشاء النسخة الاحتياطية بنجاح في {backupFilePath}");
            return backupFilePath;
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"خطأ SQLite أثناء إنشاء النسخة الاحتياطية: {ex.Message} (ErrorCode: {ex.SqliteErrorCode})");
            // يمكنك هنا معالجة أخطاء SQLite بشكل أكثر تحديدًا بناءً على رمز الخطأ
            return null; // ارجع null للإشارة إلى الفشل
        }
        catch (IOException ex)
        {
            Console.WriteLine($"خطأ إدخال/إخراج أثناء إنشاء النسخة الاحتياطية: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ غير متوقع أثناء إنشاء النسخة الاحتياطية: {ex.Message}");
            return null;
        }
    }

    private static string GenerateBackupFileName()
    {
        return $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
    }
}

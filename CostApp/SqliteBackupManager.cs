using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Maui.Storage;
using Microsoft.Data.Sqlite;
using CostApp;

public static class SqliteBackupManager
{

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
        catch (Exception)
        {
            return false;
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

            await DialogService.DisplayAlertAsync("info",$"تم إنشاء النسخة الاحتياطية بنجاح في \n{backupFilePath}","موافق");
            return backupFilePath;
        }
        catch (SqliteException ex)
        {
            await DialogService.DisplayAlertAsync("خطأ", $"خطأ SQLite أثناء إنشاء النسخة الاحتياطية: {ex.Message} (ErrorCode: {ex.SqliteErrorCode})", "موافق");
            // يمكنك هنا معالجة أخطاء SQLite بشكل أكثر تحديدًا بناءً على رمز الخطأ
            return null; // ارجع null للإشارة إلى الفشل
        }
        catch (IOException ex)
        {
            await DialogService.DisplayAlertAsync("خطأ", $"خطأ إدخال/إخراج أثناء إنشاء النسخة الاحتياطية: {ex.Message}", "موافق");
            return null;
        }
        catch (Exception ex)
        {
            await DialogService.DisplayAlertAsync("خطأ", $"خطأ غير متوقع أثناء إنشاء النسخة الاحتياطية: {ex.Message}", "موافق");
            return null;
        }
    }

    /// <summary>
    /// يولد اسم ملف نسخة احتياطية جديدة بناءً على التاريخ والوقت الحاليين.
    /// </summary>
    /// <returns>اسم ملف النسخة الاحتياطية</returns>
    private static string GenerateBackupFileName()
    {
        return $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
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
    /// ينشئ اسمًا لملف النسخة الاحتياطية بناءً على الوقت الحالي.
    /// </summary>
    /// <returns>اسم الملف.</returns>
    //public static string GenerateBackupFileName2()
    //{
    //    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    //    return $"backup_{timestamp}.db";
    //}

    /// <summary>
    /// يستعيد قاعدة بيانات SQLite من ملف النسخة الاحتياطية المحدد.
    /// </summary>
    /// <param name="context">كائن DbContext لإدارة قاعدة البيانات.</param>
    /// <param name="backupPath">المسار إلى ملف النسخة الاحتياطية.</param>
    /// <returns>نتيجة العملية (ناجحة أو فاشلة).</returns>
    //public static async Task<bool> RestoreDataAsync(DbContext context, string backupPath)
    //{
    //    try
    //    {
    //        string databasePath = context.Database.GetDbConnection().DataSource;

    //        if (string.IsNullOrWhiteSpace(backupPath))
    //            throw new ArgumentException("مسار النسخة الاحتياطية غير صالح.");

    //        if (!File.Exists(backupPath))
    //            throw new FileNotFoundException("لم يتم العثور على ملف النسخة الاحتياطية.", backupPath);

    //        if (!File.Exists(databasePath))
    //            throw new FileNotFoundException("لم يتم العثور على ملف قاعدة البيانات.", databasePath);

    //        await context.Database.CloseConnectionAsync();
    //        await context.DisposeAsync();

    //        File.Copy(backupPath, databasePath, overwrite: true);

    //        Console.WriteLine("تم استعادة قاعدة البيانات بنجاح.");
    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"خطأ أثناء استعادة قاعدة البيانات: {ex.Message}");
    //        return false;
    //    }
    //}



    /// <summary>
    /// يقوم بإنشاء نسخة احتياطية من قاعدة بيانات SQLite في الموقع المحدد.
    /// </summary>
    /// <param name="context">كائن DbContext لإدارة قاعدة البيانات.</param>
    /// <param name="backupPath">المسار لحفظ النسخة الاحتياطية.</param>
    /// <returns>نتيجة العملية (ناجحة أو فاشلة).</returns>
    //public static async Task<bool> BackupDatabaseAsync(DbContext context, string backupPath)
    //{
    //    try
    //    {
    //        string databasePath = context.Database.GetDbConnection().DataSource;
    //        string backupFilePath = Path.Combine(backupPath, GenerateBackupFileName());

    //        if (string.IsNullOrWhiteSpace(backupPath))
    //            throw new ArgumentException("مسار النسخ الاحتياطي غير صالح.");

    //        if (!File.Exists(databasePath))
    //            throw new FileNotFoundException("لم يتم العثور على ملف قاعدة البيانات.", databasePath);

    //        string? backupDirectory = Path.GetDirectoryName(backupFilePath);
    //        if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
    //            Directory.CreateDirectory(backupDirectory);

    //        File.Copy(databasePath, backupFilePath, overwrite: true);

    //        Console.WriteLine($"تم إنشاء النسخة الاحتياطية بنجاح في {backupFilePath}");
    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"خطأ أثناء إنشاء النسخة الاحتياطية: {ex.Message}");
    //        return false;
    //    }
    //    finally
    //    {
    //        await context.Database.CloseConnectionAsync();
    //        context.Dispose();
    //    }
    //}


}
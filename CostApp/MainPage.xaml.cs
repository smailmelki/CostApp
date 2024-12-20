using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Views;
using CostApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Maui.Storage;

namespace CostApp
{
    public partial class MainPage : ContentPage
    {
        DBContext db = new DBContext();
        TreeItem treeItem;
        public ObservableCollection<TreeItem> Items { get; set; }

        public MainPage()
        {
            InitializeComponent();
            // ضبط BindingContext للصفحة
            this.BindingContext = this;

            FillData();
        }

        private void FillData()
        {
            try
            {           
            // تهيئة البيانات
            Items = (from i in db.TreeItem
                     select new TreeItem
                     {
                         ID = i.ID,
                         Title = i.Title,
                         Details = (from dt in db.DetailItem.Where(x => x.ParentID == i.ID)
                                    select new DetailItem
                                    {
                                        ID = dt.ID,
                                        ParentID = dt.ParentID,
                                        Date = dt.Date,
                                        Amount = dt.Amount,
                                        Note = dt.Note,
                                    }).ToList()
                     }).ToObservableCollection();
            CollectionItemView.ItemsSource = Items;
            lblTotal.Text = (Items?.Sum(item => item.Total) ?? 0).ToString("C", new CultureInfo("ar-DZ"));
            }
            catch (Exception)
            {
                ShowToast("حدث خطأ ما !");
            }
        }

        private async void ShowToast(string message)
        {


#if ANDROID || IOS
            CancellationTokenSource cancellation = new CancellationTokenSource();

            // إنشاء Toast
            var toast = Toast.Make(message, ToastDuration.Short, 14); // 14 حجم الخط
            // عرض Toast
            await toast.Show(cancellation.Token);
#else
            await DisplayAlert("خطأ",message,"موافق");
#endif
        }

        private async void SupBtnEdit_Clicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.BindingContext is DetailItem detailItem)
            {
                var popup = new EditPopup(detailItem);
                var result = await this.ShowPopupAsync(popup);

                if (result is DetailItem item)
                {
                    db = new DBContext();
                    db.DetailItem.Update(item);
                    db.SaveChanges();
                    FillData();
                }
            }
        }

        private async void SupBtnDelete_Clicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.BindingContext is DetailItem detailItem)
            {
                bool confirm = await DisplayAlert("تأكيد الحذف", $"هل تريد حذف العنصر ؟", "نعم", "لا");
                if (confirm)
                {
                    db = new DBContext();
                    db.DetailItem.Remove(detailItem);
                    db.SaveChanges();
                    FillData();
                }
            }
        }

        private async void BtnAddItem_Clicked(object sender, EventArgs e)
        {
            string result = await DisplayPromptAsync(title: "اضافة عنصر", message: "", accept: "حفظ", cancel: "الغاء");
            if (!string.IsNullOrWhiteSpace(result))
            {
                if (db.TreeItem.Any(i=>i.Title==result))
                {
                    ShowToast("هذا العنصر موجود مسبقا !");
                    return;
                }
                treeItem = new TreeItem
                {
                    Title = result,
                };
                db = new DBContext();
                db.TreeItem.Update(treeItem);
                db.SaveChanges();
                FillData();
            }
        }


        private async void BtnDelete_Clicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.BindingContext is TreeItem treeItem)
            {
                bool confirm = await DisplayAlert("تأكيد الحذف", $"هل تريد حذف العنصر ؟ \n سيتم حذف كل العناصر المرتبطة به", "نعم", "لا");
                if (confirm)
                {
                    db = new DBContext();
                    if (treeItem.Details != null)
                    {
                        db.DetailItem.RemoveRange(treeItem.Details);
                        db.SaveChanges();
                    }                     
                    db.TreeItem.Remove(treeItem);
                    db.SaveChanges();
                    FillData();
                }
            }
        }

        private async void BtnAdd_Clicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.BindingContext is TreeItem treeItem)
            {
                var popup = new EditPopup(treeItem);
                var result = await this.ShowPopupAsync(popup);

                if (result is DetailItem detailItem)
                {
                    db = new DBContext();
                    db.DetailItem.Add(detailItem);
                    db.SaveChanges();
                    FillData();
                }
            }
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            if (BottomSheetBorder.IsVisible)
            {
                // Bottom Sheet إخفاء الـ 
                await BottomSheetBorder.TranslateTo(0, 100, 500, Easing.Linear); // انتقال للأسفل
                //await BottomSheetBorder.FadeTo(0, 500); // تقليل الشفافية
                BottomSheetBorder.IsVisible = false; // إخفاء المكون
            }
            else
            {
                // Bottom Sheet إظهار الـ
                BottomSheetBorder.IsVisible = true; // إظهار المكون
                await BottomSheetBorder.TranslateTo(0, 0, 500, Easing.Linear); // انتقال للأعلى
                //await BottomSheetBorder.FadeTo(1, 500); // زيادة الشفافية
            }       
        }
        private async void CloseBottomSheet_Clicked(object sender, EventArgs e)
        {
            // Bottom Sheet إخفاء الـ 
            await BottomSheetBorder.TranslateTo(0, 100, 500, Easing.Linear); // انتقال للأسفل
            BottomSheetBorder.IsVisible = false; // إخفاء المكون
        }

        private async void btnBackUp_Clicked(object sender, EventArgs e)
        {
            //Get user to pick backup location
            string? backupPath = await SqliteBackupManager.PickBackupFolderAsync();
            if (string.IsNullOrEmpty(backupPath))
            {
                await DisplayAlert("Error", "Backup location not selected.", "OK");
                return;
            }
            using (var context = new DBContext()) // Replace YourDbContext with your actual DbContext class
            {
                string databasePath = context.Database.GetDbConnection().DataSource;
                string? backupFilePath = await DatabaseBackup.BackupDatabaseAsync(databasePath, backupPath);

                if (backupFilePath != null)
                {
                    await DisplayAlert("Info", "Backup operation is successful", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Backup operation has failed.", "OK");
                }
            }
        }

        private async void btnRestor_Clicked(object sender, EventArgs e)
        {
            // اختيار ملف النسخة الاحتياطية
            var backupPath = await SqliteBackupManager.PickBackupFileAsync();
            if (string.IsNullOrEmpty(backupPath))
            {
                await DisplayAlert("خطأ", "لم يتم العثور على قاعدة البيانات الاحتياطية.", "موافق");
                return;
            }

            try
            {
                // فتح الملف كتيار بيانات
                using var dbFileStream = File.OpenRead(backupPath);
                // استعادة قاعدة البيانات
                if (await SqliteBackupManager.RestoreBdFileAsync(dbFileStream, db))
                {
                    FillData();
                    await DisplayAlert("نجاح", "تمت استعادة قاعدة البيانات بنجاح.", "موافق");
                }
                else
                {
                    await DisplayAlert("خطأ", "فشلت عملية استعادة قاعدة البيانات.", "موافق");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", $"حدث خطأ أثناء استعادة قاعدة البيانات: {ex.Message}", "موافق");
            }
        }       
    }
}


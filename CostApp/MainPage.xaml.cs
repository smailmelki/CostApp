using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CostApp.Models;

namespace CostApp
{
    public partial class MainPage : ContentPage
    {
        private Project activeProject = new();
        public ObservableCollection<TreeItem> Items { get; set; } = new();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            FillData();
        }

        private void FillData()
        {
            try
            {
                activeProject = CostDataService.GetOrEnsureActiveProject();
                var categories = CostDataService.GetProjectCategories(activeProject.ID, searchBar.Text);

                Items = new ObservableCollection<TreeItem>(categories);
                CollectionItemView.ItemsSource = Items;

                lblProjectTitle.Text = activeProject.Name;
                lblTotal.Text = $"المجموع: {CostDataService.FormatCurrency(activeProject.TotalSpent)}";

                if (activeProject.HasBudget)
                {
                    lblRemaining.IsVisible = true;
                    lblRemaining.Text = $"المتبقي: {CostDataService.FormatCurrency(activeProject.RemainingBudget)}";
                    lblRemaining.TextColor = activeProject.RemainingBudget >= 0 ? Colors.SeaGreen : Colors.IndianRed;
                }
                else
                {
                    lblRemaining.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                ShowToast($"حدث خطأ: {ex.Message}");
            }
        }

        private async void ShowToast(string message)
        {
#if ANDROID || IOS
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show();
#else
            await DisplayAlertAsync("تنبيه", message, "موافق");
#endif
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            FillData();
        }

        private void OnSearchPressed(object? sender, EventArgs e)
        {
            FillData();
        }

        private async void ProjectsToolbarItem_Clicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ProjectsPage");
        }

        private async void SupBtnEdit_Clicked(object? sender, EventArgs e)
        {
            if (sender is ImageButton { BindingContext: DetailItem detailItem })
            {
                var popup = new EditPopup(detailItem, activeProject.ID);
                var result = await this.ShowPopupAsync<DetailItem>(popup);

                if (result?.Result is DetailItem updated)
                {
                    CostDataService.UpdateExpense(updated);
                    FillData();
                }
            }
        }

        private async void SupBtnDelete_Clicked(object? sender, EventArgs e)
        {
            if (sender is ImageButton { BindingContext: DetailItem detailItem })
            {
                bool confirm = await DisplayAlertAsync("تأكيد الحذف", "هل تريد حذف هذا المصروف؟", "نعم", "لا");
                if (confirm)
                {
                    CostDataService.DeleteExpense(detailItem.ID);
                    FillData();
                }
            }
        }

        private async void BtnAddItem_Clicked(object? sender, EventArgs e)
        {
            string? result = await DisplayPromptAsync(
                title: "إضافة تصنيف",
                message: $"أدخل اسم التصنيف ضمن مشروع \"{activeProject.Name}\":",
                accept: "حفظ",
                cancel: "إلغاء",
                maxLength: 50);

            if (!string.IsNullOrWhiteSpace(result))
            {
                var (success, error) = CostDataService.AddCategory(activeProject.ID, result);
                if (!success)
                {
                    ShowToast(error ?? "تعذر إضافة التصنيف.");
                    return;
                }

                FillData();
            }
        }

        private async void BtnRenameCategory_Clicked(object? sender, EventArgs e)
        {
            if (sender is ImageButton { BindingContext: TreeItem treeItem })
            {
                string? result = await DisplayPromptAsync(
                    title: "تعديل اسم التصنيف",
                    message: "أدخل الاسم الجديد:",
                    accept: "حفظ",
                    cancel: "إلغاء",
                    initialValue: treeItem.Title,
                    maxLength: 50);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    var (success, error) = CostDataService.RenameCategory(treeItem.ID, result);
                    if (!success)
                    {
                        ShowToast(error ?? "تعذر تعديل اسم التصنيف.");
                        return;
                    }

                    FillData();
                }
            }
        }

        private async void BtnDelete_Clicked(object? sender, EventArgs e)
        {
            if (sender is ImageButton { BindingContext: TreeItem treeItem })
            {
                bool confirm = await DisplayAlertAsync(
                    "تأكيد الحذف",
                    $"هل تريد حذف \"{treeItem.Title}\"؟\nسيتم حذف كل المصاريف المرتبطة به.",
                    "نعم",
                    "لا");

                if (confirm)
                {
                    CostDataService.DeleteCategory(treeItem.ID);
                    FillData();
                }
            }
        }

        private async void BtnAdd_Clicked(object? sender, EventArgs e)
        {
            if (sender is ImageButton { BindingContext: TreeItem treeItem })
            {
                var popup = new EditPopup(treeItem);
                var result = await this.ShowPopupAsync<DetailItem>(popup);

                if (result?.Result is DetailItem detailItem)
                {
                    CostDataService.AddExpense(detailItem);
                    FillData();
                }
            }
        }

        private async void ToolbarItem_Clicked(object? sender, EventArgs e)
        {
            if (BottomSheetBorder.IsVisible)
            {
                await BottomSheetBorder.TranslateToAsync(0, 120, 250, Easing.CubicIn);
                BottomSheetBorder.IsVisible = false;
            }
            else
            {
                BottomSheetBorder.TranslationY = 120;
                BottomSheetBorder.IsVisible = true;
                await BottomSheetBorder.TranslateToAsync(0, 0, 250, Easing.CubicOut);
            }
        }

        private async void CloseBottomSheet_Clicked(object? sender, EventArgs e)
        {
            await BottomSheetBorder.TranslateToAsync(0, 120, 250, Easing.CubicIn);
            BottomSheetBorder.IsVisible = false;
        }

        private async void btnBackUp_Clicked(object? sender, EventArgs e)
        {
            var dbPath = DBContext.GetDatabasePath();
            var fileName = $"CostApp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            bool backupDone = await SqliteBackupManager.ExportBackupAsync(dbPath, fileName);

            if (backupDone)
            {
                await DisplayAlertAsync("نسخ احتياطي", "تم إنشاء النسخة الاحتياطية بنجاح.", "موافق");
            }
            else
            {
                await DisplayAlertAsync("تنبيه", "لم يتم حفظ النسخة الاحتياطية.", "موافق");
            }
        }

        private async void btnRestor_Clicked(object? sender, EventArgs e)
        {
            var pickedBackupPath = await SqliteBackupManager.PickBackupFileAsync();
            if (string.IsNullOrEmpty(pickedBackupPath))
                return;

            try
            {
                bool confirm = await DisplayAlertAsync(
                    "تأكيد الاستعادة",
                    "سيتم استبدال البيانات الحالية بالنسخة المختارة (مع إنشاء نسخة أمان تلقائية قبل الاستبدال). هل تريد المتابعة؟",
                    "استعادة",
                    "إلغاء");

                if (!confirm)
                    return;

                var (success, message) = await SqliteBackupManager.RestoreDatabaseSafelyAsync(pickedBackupPath);
                if (success)
                {
                    FillData();
                    await DisplayAlertAsync("نجاح", message, "موافق");
                }
                else
                {
                    await DisplayAlertAsync("خطأ", message, "موافق");
                }
            }
            finally
            {
                SqliteBackupManager.TryDeleteFile(pickedBackupPath);
            }
        }
    }
}

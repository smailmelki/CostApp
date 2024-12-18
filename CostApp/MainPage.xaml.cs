using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Views;
using CostApp.Models;

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

        private void btnBackUp_Clicked(object sender, EventArgs e)
        {

        }

        private void btnRestor_Clicked(object sender, EventArgs e)
        {

        }
    }
}


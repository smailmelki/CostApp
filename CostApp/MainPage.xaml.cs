using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Views;
using CostApp.Models;

namespace CostApp
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<TreeItem> Items { get; set; }

        public decimal GrandTotal => Items?.Sum(item => item.Total) ?? 0;

        public MainPage()
        {
            InitializeComponent();

            // تهيئة البيانات
            Items = new ObservableCollection<TreeItem>
            {
                new TreeItem
                {
                    Title = "Item 1",
                    Details = new List<DetailItem>
                    {
                        new DetailItem {ID = 0, Date = DateTime.Now, Amount = 500 },
                        new DetailItem {ID = 1, Date = DateTime.Now.AddDays(-1), Amount = 1000 },
                        new DetailItem {ID = 2, Date = DateTime.Now.AddDays(-2), Amount = 650 }
                    }
                },
                new TreeItem
                {
                    Title = "Item 2",
                    Details = new List<DetailItem>
                    {
                        new DetailItem {ID = 3, Date = DateTime.Now, Amount = 800 },
                        new DetailItem {ID = 4, Date = DateTime.Now.AddDays(-2), Amount = 1500 }
                    }
                }
            };

            // ضبط BindingContext للصفحة
            this.BindingContext = this;
        }

        private async void SupBtnEdit_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.BindingContext is DetailItem detailItem)
            {
                var popup = new EditPopup(detailItem.Amount);
                var result = await this.ShowPopupAsync(popup);

                if (result is true)
                {
                    detailItem.Amount = popup.Amount;
                }
            }
        }

        private async void SupBtnDelete_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.BindingContext is DetailItem detailItem)
            {
                bool confirm = await DisplayAlert("تأكيد الحذف", $"هل تريد حذف العنصر بتاريخ {detailItem.Date:dd-MM-yyyy}؟", "نعم", "لا");
                if (confirm)
                {
                    // العثور على العنصر الأساسي الذي يحتوي على هذا العنصر الفرعي
                    foreach (var item in Items)
                    {
                        if (item.Details.Contains(detailItem))
                        {
                            item.Details.Remove(detailItem); // حذف العنصر الفرعي
                            break;
                        }
                    }
                }
            }
        }
    }
}


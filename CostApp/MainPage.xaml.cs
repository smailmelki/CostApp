using CommunityToolkit.Maui.Views;
using CostApp.Models;

namespace CostApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();            
        }

        private async void BtnEdit_Clicked(object sender, EventArgs e)
        {
            // التحقق من أن المرسل هو زر
            if (sender is Button button)
            {
                // استخراج القيمة من CommandParameter
                DetailItem item = (DetailItem)button.CommandParameter;
                var result = await this.ShowPopupAsync(new AddItem(item));

            }
        }

        private void BtnDelete_Clicked(object sender, EventArgs e)
        {
            // التحقق من أن المرسل هو زر
            if (sender is Button button)
            {
                // استخراج القيمة من CommandParameter
                DetailItem item = (DetailItem)button.CommandParameter;

                // عرض الـ ID (يمكنك استخدامه كما تريد)
                DisplayAlert("Info", $"ID هو: {item.ID}", "OK");
            }
        }
    }    
}

namespace CostApp
{
    public static class DialogService // أو في أي مكان مناسب آخر
    {

        public static async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            if (App.Current?.MainPage != null) // تحقق من وجود MainPage
            {
                await App.Current.MainPage.DisplayAlert(title, message, cancel);
            }
            else
            {
                // معالجة حالة عدم وجود MainPage (نادرًا ما تحدث)
                System.Diagnostics.Debug.WriteLine("MainPage is null!");
            }
        }


        public static async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            if (App.Current?.MainPage != null)
            {
                return await App.Current.MainPage.DisplayAlert(title, message, accept, cancel);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainPage is null!");
                return false; // أو قيمة افتراضية مناسبة
            }
        }
    }

}

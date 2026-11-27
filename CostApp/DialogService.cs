namespace CostApp
{
    public static class DialogService
    {
        private static Page? CurrentPage =>
            Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;

        public static async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            var page = CurrentPage;
            if (page != null)
            {
                await page.DisplayAlertAsync(title, message, cancel);
            }
        }

        public static async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            var page = CurrentPage;
            if (page != null)
            {
                return await page.DisplayAlertAsync(title, message, accept, cancel);
            }

            return false;
        }
    }
}

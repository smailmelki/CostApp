using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CostApp.Models;

namespace CostApp
{
    public partial class EditPopup : Popup
    {
        private readonly DetailItem detailItem;

        public EditPopup(TreeItem treeItem)
        {
            InitializeComponent();
            detailItem = new DetailItem { ParentID = treeItem.ID, Date = DateTime.Today };
            lblpicker.IsVisible = false;
            pickerItems.IsVisible = false;
        }

        public EditPopup(DetailItem detailItem)
        {
            InitializeComponent();
            LoadData();
            this.detailItem = detailItem;
            GetData();
        }

        private void LoadData()
        {
            using var db = new DBContext();
            pickerItems.ItemsSource = db.TreeItem
                .Select(a => new { a.ID, a.Title })
                .ToList();
        }

        private bool TrySetData()
        {
            var text = txtAmount.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
                && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                return false;

            if (amount <= 0)
                return false;

            detailItem.Date = dtpDate.Date;
            detailItem.Amount = amount;
            detailItem.Note = txtNote.Text?.Trim();
            if (pickerItems.SelectedItem != null)
                detailItem.ParentID = ((dynamic)pickerItems.SelectedItem).ID;

            return true;
        }

        private void GetData()
        {
            dtpDate.Date = detailItem.Date;
            txtAmount.Text = detailItem.Amount.ToString(CultureInfo.CurrentCulture);
            txtNote.Text = detailItem.Note;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (!TrySetData())
            {
                await DisplayAlert("بيانات غير صحيحة",
                    "أدخل مبلغًا رقميًا أكبر من صفر، مع استخدام الفاصلة العشرية المناسبة.", "موافق");
                return;
            }

            await CloseAsync(detailItem);
        }

        private void OnCancelClicked(object sender, EventArgs e) => Close(false);
    }
}
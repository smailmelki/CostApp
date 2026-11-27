using System.Globalization;
using CommunityToolkit.Maui.Views;
using CostApp.Models;

namespace CostApp
{
    public partial class EditPopup : Popup<DetailItem>
    {
        private readonly DetailItem detailItem;

        public EditPopup(TreeItem treeItem)
        {
            InitializeComponent();
            lblHeader.Text = $"إضافة مصروف - {treeItem.Title}";
            detailItem = new DetailItem { ParentID = treeItem.ID, Date = DateTime.Today };
            lblpicker.IsVisible = false;
            pickerItems.IsVisible = false;
            dtpDate.Date = DateTime.Today;
        }

        public EditPopup(DetailItem existingItem, int projectId)
        {
            InitializeComponent();
            lblHeader.Text = "تعديل مصروف";
            detailItem = new DetailItem
            {
                ID = existingItem.ID,
                ParentID = existingItem.ParentID,
                Date = existingItem.Date,
                Amount = existingItem.Amount,
                Note = existingItem.Note
            };
            LoadCategories(projectId);
            PopulateFields();
        }

        private void LoadCategories(int projectId)
        {
            var categories = CostDataService.GetCategoriesList(projectId);
            pickerItems.ItemsSource = categories;
            var current = categories.FirstOrDefault(c => c.ID == detailItem.ParentID);
            if (current != null)
                pickerItems.SelectedItem = current;
        }

        private void PopulateFields()
        {
            dtpDate.Date = detailItem.Date;
            txtAmount.Text = detailItem.Amount.ToString("0.##", CultureInfo.CurrentCulture);
            txtNote.Text = detailItem.Note;
        }

        private bool TryBuildResult()
        {
            if (!CostDataService.TryParseAmount(txtAmount.Text, allowZero: false, out var amount))
                return false;

            detailItem.Date = (dtpDate.Date ?? DateTime.Today).Date;
            detailItem.Amount = amount;
            detailItem.Note = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim();

            if (pickerItems.IsVisible && pickerItems.SelectedItem is TreeItem selectedCategory)
            {
                detailItem.ParentID = selectedCategory.ID;
            }

            return true;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (!TryBuildResult())
            {
                await DialogService.DisplayAlertAsync("بيانات غير صحيحة",
                    "أدخل مبلغًا رقميًا صحيحًا أكبر من صفر.", "موافق");
                return;
            }

            await CloseAsync(detailItem);
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await CloseAsync(default!);
        }
    }
}

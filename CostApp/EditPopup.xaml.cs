using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CostApp.Models;

namespace CostApp
{
    public partial class EditPopup : Popup
    {

        DetailItem detailItem;
        public EditPopup(TreeItem treeItem)
        {
            InitializeComponent();
            detailItem = new DetailItem
            {
                ParentID = treeItem.ID,
            };
        }
        public EditPopup(DetailItem detailItem)
        {
            InitializeComponent();
            this.detailItem = detailItem;
            GetData();
        }

        void SetData()
        {
            if (!string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                detailItem.Date = dtpDate.Date;
                detailItem.Amount = decimal.Parse(txtAmount.Text);
                detailItem.Note = txtNote.Text;
            }
        }
        void GetData()
        {
            dtpDate.Date = detailItem.Date;
            txtAmount.Text = detailItem.Amount.ToString();
            txtNote.Text = detailItem.Note;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            SetData();
            if (!string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                await CloseAsync(detailItem);
            }
            else
            {
                //CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                //string message = "ÌÃ» «œŒ«· ﬁÌ„… ’«·Õ… !";
                //var toast = Toast.Make(message, ToastDuration.Short, 14); // «·—”«·…° «·„œ…° ÊÕÃ„ «·Œÿ
                //await toast.Show(cancellationTokenSource.Token);
                await CloseAsync(false);
            }
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            Close(false);
        }
    }
}

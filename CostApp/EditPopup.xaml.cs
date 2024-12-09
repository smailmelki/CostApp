using CommunityToolkit.Maui.Views;

namespace CostApp
{
    public partial class EditPopup : Popup
    {
        public decimal Amount { get; private set; }

        public EditPopup(decimal currentAmount)
        {
            InitializeComponent();
            txtAmount.Text = currentAmount.ToString();
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtAmount.Text, out var newAmount))
            {
                Amount = newAmount;
                Close(true);
            }
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            Close(false);
        }
    }
}

using CommunityToolkit.Maui.Views;
using CostApp.Models;

namespace CostApp;

public partial class AddItem:Popup
{
    DetailItem Item;
    public AddItem(DetailItem item)
	{
		InitializeComponent();
        this.Item = item;
		GetData();
	}

    private void GetData()
    {
        txtAmount.Text = Item.Amount.ToString();
        dtpDate.Date=Item.Date;
        txtNote.Text=Item.Note;
    }
}
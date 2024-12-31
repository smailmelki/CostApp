using CostApp.Models;
using System.Collections.ObjectModel;

namespace CostApp;

public partial class FilterPage : ContentPage
{
    DBContext db = new DBContext();
    public ObservableCollection<DetailItem> Items { get; set; }
    IQueryable<DetailItem> data;
    decimal total= 0;
    public FilterPage()
	{
		InitializeComponent();
        ItializeData();
    }

    private void ItializeData()
    {
        FillDate(dtpFrom.Date, dtpTo.Date);
        Items = new ObservableCollection<DetailItem>(data);
        CollectionFilter.ItemsSource = Items;
        total = Items.Sum(cost => cost.Amount);
        // ��� ������� �� Label
        lblTotal.Text = $"�������: {total:N2} ��";
    }

    private void FillDate(DateTime dateFrom, DateTime dateTo)
    {
        data = (from d in db.DetailItem
                         select new DetailItem
                         {
                             Date = d.Date,
                             Amount = d.Amount,
                             Note = d.Note
                         });
    }

    private void btnFilter_Clicked(object sender, EventArgs e)
    {
        if (dtpFrom.Date > dtpTo.Date)
        {
            // ��� ����� ��� �������� ��� ����� ������� ���� �� ����� �������
            DisplayAlert("���", "����� ������� ��� �� ���� ��� ����� �������", "�����");
            return;
        }
        Items = new ObservableCollection<DetailItem>(data.Where(x => x.Date <= dtpTo.Date && x.Date >= dtpFrom.Date));
        CollectionFilter.ItemsSource = Items;
        // ���� �������
        total = Items.Sum(cost => cost.Amount);

        // ��� ������� �� Label
        lblTotal.Text = $"�������: {total:N2} ��";

    }
}
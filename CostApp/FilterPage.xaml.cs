using CostApp.Models;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;

namespace CostApp;

public partial class FilterPage : ContentPage
{
    private readonly DBContext db = new();
    public ObservableCollection<DetailItem> Items { get; private set; } = new();

    public FilterPage()
    {
        InitializeComponent();
        dtpFrom.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dtpTo.Date = DateTime.Today;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var start = dtpFrom.Date.Date;
        var endExclusive = dtpTo.Date.Date.AddDays(1);

        Items = new ObservableCollection<DetailItem>(
            db.DetailItem.AsNoTracking()
                .Where(x => x.Date >= start && x.Date < endExclusive)
                .OrderByDescending(x => x.Date)
                .Select(x => new DetailItem
                {
                    ID = x.ID,
                    ParentID = x.ParentID,
                    Date = x.Date,
                    Amount = x.Amount,
                    Note = x.Note
                })
                .ToList());

        CollectionFilter.ItemsSource = Items;
        lblTotal.Text = $"المجموع: {Items.Sum(x => x.Amount):N2} دج";
    }

    private async void btnFilter_Clicked(object sender, EventArgs e)
    {
        if (dtpFrom.Date > dtpTo.Date)
        {
            await DisplayAlert("خطأ", "تاريخ البداية يجب أن يكون قبل تاريخ النهاية أو مساويًا له.", "موافق");
            return;
        }

        ApplyFilter();
    }
}
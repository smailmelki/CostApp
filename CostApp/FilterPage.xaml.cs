using System.Collections.ObjectModel;
using CostApp.Models;

namespace CostApp;

public partial class FilterPage : ContentPage
{
    private int activeProjectId;
    private bool isLoadingCategories;
    public ObservableCollection<DetailItem> Items { get; private set; } = new();

    public FilterPage()
    {
        InitializeComponent();
        dtpFrom.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dtpTo.Date = DateTime.Today;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var project = CostDataService.GetOrEnsureActiveProject();
        activeProjectId = project.ID;
        lblProjectName.Text = $"المشروع: {project.Name}";
        LoadCategoryOptions();
        ApplyFilter();
    }

    private void LoadCategoryOptions()
    {
        isLoadingCategories = true;
        try
        {
            var options = new List<TreeItem>
            {
                new TreeItem { ID = 0, Title = "كل التصنيفات" }
            };
            options.AddRange(CostDataService.GetCategoriesList(activeProjectId));
            pickerCategory.ItemsSource = options;
            pickerCategory.SelectedIndex = 0;
        }
        finally
        {
            isLoadingCategories = false;
        }
    }

    private void ApplyFilter()
    {
        var start = (dtpFrom.Date ?? DateTime.Today).Date;
        var end = (dtpTo.Date ?? DateTime.Today).Date;
        if (start > end)
            return;

        int? selectedCategoryId = null;
        if (pickerCategory.SelectedItem is TreeItem cat && cat.ID > 0)
            selectedCategoryId = cat.ID;

        var filtered = CostDataService.FilterExpenses(
            activeProjectId,
            start,
            end,
            selectedCategoryId,
            searchBar.Text);

        Items = new ObservableCollection<DetailItem>(filtered);
        CollectionFilter.ItemsSource = Items;
        lblTotal.Text = $"العدد: {Items.Count}  |  المجموع: {CostDataService.FormatCurrency(Items.Sum(x => x.Amount))}";
    }

    private async void btnFilter_Clicked(object sender, EventArgs e)
    {
        var start = (dtpFrom.Date ?? DateTime.Today).Date;
        var end = (dtpTo.Date ?? DateTime.Today).Date;
        if (start > end)
        {
            await DisplayAlertAsync("خطأ", "تاريخ البداية يجب أن يكون قبل تاريخ النهاية أو مساويًا له.", "موافق");
            return;
        }

        ApplyFilter();
    }

    private void OnCategoryChanged(object? sender, EventArgs e)
    {
        if (!isLoadingCategories)
            ApplyFilter();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void OnSearchPressed(object? sender, EventArgs e)
    {
        ApplyFilter();
    }
}

using CommunityToolkit.Maui.Extensions;
using CostApp.Models;

namespace CostApp
{
    public partial class ProjectsPage : ContentPage
    {
        public ProjectsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProjects();
        }

        private void LoadProjects()
        {
            var activeId = CostDataService.GetOrEnsureActiveProject().ID;
            var projects = CostDataService.GetProjectsWithSummary();
            ProjectsCollection.ItemsSource = projects
                .Select(p => new ProjectCardViewModel(p, p.ID == activeId))
                .ToList();
        }

        private async void OnNewProjectClicked(object sender, EventArgs e)
        {
            var popup = new ProjectPopup();
            var result = await this.ShowPopupAsync<Project>(popup);
            if (result?.Result is Project newProject)
            {
                var (success, error) = CostDataService.SaveProject(newProject);
                if (!success)
                {
                    await DisplayAlertAsync("تعذر الحفظ", error ?? "حدث خطأ غير متوقع.", "موافق");
                    return;
                }

                CostDataService.SetActiveProjectId(newProject.ID);
                LoadProjects();
                await Shell.Current.GoToAsync("//MainPage");
            }
        }

        private async void OnOpenProjectClicked(object sender, EventArgs e)
        {
            if (sender is Button { CommandParameter: ProjectCardViewModel vm })
            {
                CostDataService.SetActiveProjectId(vm.Project.ID);
                await Shell.Current.GoToAsync("//MainPage");
            }
        }

        private async void OnEditProjectClicked(object sender, EventArgs e)
        {
            if (sender is Button { CommandParameter: ProjectCardViewModel vm })
            {
                var popup = new ProjectPopup(vm.Project);
                var result = await this.ShowPopupAsync<Project>(popup);
                if (result?.Result is Project updated)
                {
                    var (success, error) = CostDataService.SaveProject(updated);
                    if (!success)
                    {
                        await DisplayAlertAsync("تعذر التعديل", error ?? "حدث خطأ غير متوقع.", "موافق");
                        return;
                    }

                    LoadProjects();
                }
            }
        }

        private async void OnDeleteProjectClicked(object sender, EventArgs e)
        {
            if (sender is Button { CommandParameter: ProjectCardViewModel vm })
            {
                bool confirm = await DisplayAlertAsync(
                    "تأكيد حذف المشروع",
                    $"هل تريد حذف مشروع \"{vm.Name}\" نهائيًا؟\nسيتم حذف جميع التصنيفات والمصاريف المرتبطة به.",
                    "حذف",
                    "إلغاء");

                if (!confirm)
                    return;

                var (success, error) = CostDataService.DeleteProject(vm.Project.ID);
                if (!success)
                {
                    await DisplayAlertAsync("تنبيه", error ?? "تعذر حذف المشروع.", "موافق");
                    return;
                }

                LoadProjects();
            }
        }

        public sealed class ProjectCardViewModel
        {
            public Project Project { get; }
            public bool IsActive { get; }

            public ProjectCardViewModel(Project project, bool isActive)
            {
                Project = project;
                IsActive = isActive;
            }

            public string Name => Project.Name;
            public string? Description => Project.Description;
            public bool HasDescription => !string.IsNullOrWhiteSpace(Project.Description);
            public bool HasBudget => Project.HasBudget;
            public string TotalSpentText => $"المصروفات: {CostDataService.FormatCurrency(Project.TotalSpent)}";
            public string ExpenseCountText => $"السجلات: {Project.ExpenseCount}";
            public string BudgetText => $"الميزانية: {CostDataService.FormatCurrency(Project.Budget)}";
            public string RemainingText => $"المتبقي: {CostDataService.FormatCurrency(Project.RemainingBudget)}";
            public Color RemainingColor => Project.RemainingBudget >= 0 ? Colors.SeaGreen : Colors.IndianRed;
        }
    }
}

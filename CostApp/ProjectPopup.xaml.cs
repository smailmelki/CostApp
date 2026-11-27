using System.Globalization;
using CommunityToolkit.Maui.Views;
using CostApp.Models;

namespace CostApp
{
    public partial class ProjectPopup : Popup<Project>
    {
        private readonly Project project;

        public ProjectPopup()
        {
            InitializeComponent();
            project = new Project();
            lblTitle.Text = "مشروع جديد";
        }

        public ProjectPopup(Project existingProject)
        {
            InitializeComponent();
            project = new Project
            {
                ID = existingProject.ID,
                Name = existingProject.Name,
                Description = existingProject.Description,
                Budget = existingProject.Budget,
                CreatedAt = existingProject.CreatedAt
            };
            lblTitle.Text = "تعديل المشروع";
            txtName.Text = project.Name;
            txtDescription.Text = project.Description;
            txtBudget.Text = project.Budget > 0
                ? project.Budget.ToString("0.##", CultureInfo.CurrentCulture)
                : string.Empty;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var name = txtName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DialogService.DisplayAlertAsync("بيانات غير صحيحة", "يرجى إدخال اسم المشروع.", "موافق");
                return;
            }

            decimal budget = 0m;
            if (!string.IsNullOrWhiteSpace(txtBudget.Text))
            {
                if (!CostDataService.TryParseAmount(txtBudget.Text, allowZero: true, out budget))
                {
                    await DialogService.DisplayAlertAsync("بيانات غير صحيحة",
                        "يرجى إدخال ميزانية رقمية صحيحة (صفر أو أكبر).", "موافق");
                    return;
                }
            }

            project.Name = name;
            project.Description = txtDescription.Text?.Trim();
            project.Budget = budget;

            await CloseAsync(project);
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await CloseAsync(default!);
        }
    }
}

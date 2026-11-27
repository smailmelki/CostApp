using System.Globalization;
using System.Text;
using CostApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CostApp
{
    public static class CostDataService
    {
        private const string ActiveProjectKey = "ActiveProjectId";

        public static int GetActiveProjectId()
        {
            return Preferences.Default.Get(ActiveProjectKey, 0);
        }

        public static void SetActiveProjectId(int projectId)
        {
            Preferences.Default.Set(ActiveProjectKey, projectId);
        }

        public static Project GetOrEnsureActiveProject()
        {
            using var db = new DBContext();
            var activeId = GetActiveProjectId();
            var project = activeId > 0
                ? db.Projects.AsNoTracking().FirstOrDefault(p => p.ID == activeId)
                : null;

            if (project == null)
            {
                project = db.Projects.AsNoTracking().OrderBy(p => p.ID).FirstOrDefault();
                if (project == null)
                {
                    project = new Project
                    {
                        Name = "المشروع الرئيسي",
                        Description = "المشروع الافتراضي",
                        Budget = 0m,
                        CreatedAt = DateTime.Now
                    };
                    db.Projects.Add(project);
                    db.SaveChanges();
                }

                SetActiveProjectId(project.ID);
            }

            PopulateProjectSummary(db, project);
            return project;
        }

        public static List<Project> GetProjectsWithSummary()
        {
            using var db = new DBContext();
            var projects = db.Projects.AsNoTracking()
                .OrderBy(p => p.ID)
                .ToList();

            var categories = db.TreeItem.AsNoTracking()
                .Select(t => new { t.ID, t.ProjectID })
                .ToList();

            var details = db.DetailItem.AsNoTracking()
                .Select(d => new { d.ParentID, d.Amount })
                .ToList();

            var categoryToProject = categories.ToDictionary(c => c.ID, c => c.ProjectID);

            var totalsByProject = new Dictionary<int, (decimal total, int count)>();
            foreach (var d in details)
            {
                if (categoryToProject.TryGetValue(d.ParentID, out var projectId))
                {
                    totalsByProject.TryGetValue(projectId, out var current);
                    totalsByProject[projectId] = (current.total + d.Amount, current.count + 1);
                }
            }

            foreach (var project in projects)
            {
                if (totalsByProject.TryGetValue(project.ID, out var summary))
                {
                    project.TotalSpent = summary.total;
                    project.ExpenseCount = summary.count;
                }
            }

            return projects;
        }

        private static void PopulateProjectSummary(DBContext db, Project project)
        {
            var categoryIds = db.TreeItem.AsNoTracking()
                .Where(t => t.ProjectID == project.ID)
                .Select(t => t.ID)
                .ToList();

            if (categoryIds.Count == 0)
            {
                project.TotalSpent = 0m;
                project.ExpenseCount = 0;
                return;
            }

            var amounts = db.DetailItem.AsNoTracking()
                .Where(d => categoryIds.Contains(d.ParentID))
                .Select(d => d.Amount)
                .ToList();

            project.TotalSpent = amounts.Sum();
            project.ExpenseCount = amounts.Count;
        }

        public static (bool success, string? error) SaveProject(Project project)
        {
            var name = project.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return (false, "اسم المشروع مطلوب.");

            if (project.Budget < 0)
                return (false, "الميزانية لا يمكن أن تكون سالبة.");

            using var db = new DBContext();
            var duplicate = db.Projects.AsNoTracking()
                .Any(p => p.ID != project.ID && p.Name.ToLower() == name.ToLower());
            if (duplicate)
                return (false, "يوجد مشروع آخر بنفس الاسم.");

            project.Name = name;
            project.Description = string.IsNullOrWhiteSpace(project.Description)
                ? null
                : project.Description.Trim();

            if (project.ID == 0)
            {
                project.CreatedAt = DateTime.Now;
                db.Projects.Add(project);
            }
            else
            {
                var existing = db.Projects.FirstOrDefault(p => p.ID == project.ID);
                if (existing == null)
                    return (false, "المشروع غير موجود.");

                existing.Name = project.Name;
                existing.Description = project.Description;
                existing.Budget = project.Budget;
            }

            db.SaveChanges();
            return (true, null);
        }

        public static (bool success, string? error) DeleteProject(int projectId)
        {
            using var db = new DBContext();
            var projectCount = db.Projects.Count();
            if (projectCount <= 1)
                return (false, "لا يمكن حذف المشروع الوحيد. يجب أن يبقى مشروع واحد على الأقل.");

            using var tx = db.Database.BeginTransaction();
            var project = db.Projects.FirstOrDefault(p => p.ID == projectId);
            if (project == null)
                return (false, "المشروع غير موجود.");

            var categoryIds = db.TreeItem
                .Where(t => t.ProjectID == projectId)
                .Select(t => t.ID)
                .ToList();

            if (categoryIds.Count > 0)
            {
                var details = db.DetailItem.Where(d => categoryIds.Contains(d.ParentID)).ToList();
                db.DetailItem.RemoveRange(details);

                var categories = db.TreeItem.Where(t => t.ProjectID == projectId).ToList();
                db.TreeItem.RemoveRange(categories);
            }

            db.Projects.Remove(project);
            db.SaveChanges();
            tx.Commit();

            if (GetActiveProjectId() == projectId)
            {
                var nextProject = db.Projects.AsNoTracking().OrderBy(p => p.ID).FirstOrDefault();
                if (nextProject != null)
                    SetActiveProjectId(nextProject.ID);
            }

            return (true, null);
        }

        public static List<TreeItem> GetProjectCategories(int projectId, string? searchText = null)
        {
            using var db = new DBContext();
            var categories = db.TreeItem.AsNoTracking()
                .Where(t => t.ProjectID == projectId)
                .OrderBy(t => t.Title)
                .ToList();

            var categoryIds = categories.Select(c => c.ID).ToList();
            var details = categoryIds.Count == 0
                ? new List<DetailItem>()
                : db.DetailItem.AsNoTracking()
                    .Where(d => categoryIds.Contains(d.ParentID))
                    .OrderByDescending(d => d.Date)
                    .ThenByDescending(d => d.ID)
                    .ToList();

            var detailsByParent = details.GroupBy(d => d.ParentID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var query = searchText?.Trim();
            bool hasSearch = !string.IsNullOrWhiteSpace(query);

            var result = new List<TreeItem>();
            foreach (var category in categories)
            {
                detailsByParent.TryGetValue(category.ID, out var catDetails);
                catDetails ??= new List<DetailItem>();

                if (hasSearch)
                {
                    bool titleMatches = category.Title.Contains(query!, StringComparison.OrdinalIgnoreCase);
                    var matchingDetails = catDetails.Where(d =>
                        (!string.IsNullOrEmpty(d.Note) && d.Note.Contains(query!, StringComparison.OrdinalIgnoreCase)) ||
                        d.Amount.ToString("0.##", CultureInfo.InvariantCulture).Contains(query!, StringComparison.OrdinalIgnoreCase) ||
                        d.Amount.ToString("N2", CultureInfo.CurrentCulture).Contains(query!, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (!titleMatches && matchingDetails.Count == 0)
                        continue;

                    category.Details = titleMatches && matchingDetails.Count == 0
                        ? catDetails
                        : (titleMatches ? catDetails : matchingDetails);
                }
                else
                {
                    category.Details = catDetails;
                }

                result.Add(category);
            }

            return result;
        }

        public static List<TreeItem> GetCategoriesList(int projectId)
        {
            using var db = new DBContext();
            return db.TreeItem.AsNoTracking()
                .Where(t => t.ProjectID == projectId)
                .OrderBy(t => t.Title)
                .ToList();
        }

        public static (bool success, string? error) AddCategory(int projectId, string title)
        {
            var trimmed = title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
                return (false, "اسم التصنيف مطلوب.");

            using var db = new DBContext();
            if (db.TreeItem.Any(i => i.ProjectID == projectId && i.Title.ToLower() == trimmed.ToLower()))
                return (false, "هذا التصنيف موجود مسبقًا في هذا المشروع.");

            db.TreeItem.Add(new TreeItem
            {
                ProjectID = projectId,
                Title = trimmed
            });
            db.SaveChanges();
            return (true, null);
        }

        public static (bool success, string? error) RenameCategory(int categoryId, string newTitle)
        {
            var trimmed = newTitle?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
                return (false, "اسم التصنيف مطلوب.");

            using var db = new DBContext();
            var category = db.TreeItem.FirstOrDefault(i => i.ID == categoryId);
            if (category == null)
                return (false, "التصنيف غير موجود.");

            if (db.TreeItem.Any(i => i.ProjectID == category.ProjectID && i.ID != categoryId && i.Title.ToLower() == trimmed.ToLower()))
                return (false, "يوجد تصنيف آخر بنفس الاسم في هذا المشروع.");

            category.Title = trimmed;
            db.SaveChanges();
            return (true, null);
        }

        public static void DeleteCategory(int categoryId)
        {
            using var db = new DBContext();
            using var tx = db.Database.BeginTransaction();

            var details = db.DetailItem.Where(d => d.ParentID == categoryId).ToList();
            if (details.Count > 0)
                db.DetailItem.RemoveRange(details);

            var category = db.TreeItem.FirstOrDefault(t => t.ID == categoryId);
            if (category != null)
                db.TreeItem.Remove(category);

            db.SaveChanges();
            tx.Commit();
        }

        public static void AddExpense(DetailItem item)
        {
            using var db = new DBContext();
            db.DetailItem.Add(item);
            db.SaveChanges();
        }

        public static void UpdateExpense(DetailItem item)
        {
            using var db = new DBContext();
            var existing = db.DetailItem.FirstOrDefault(d => d.ID == item.ID);
            if (existing == null)
                return;

            existing.ParentID = item.ParentID;
            existing.Date = item.Date;
            existing.Amount = item.Amount;
            existing.Note = item.Note;
            db.SaveChanges();
        }

        public static void DeleteExpense(int detailId)
        {
            using var db = new DBContext();
            var existing = db.DetailItem.FirstOrDefault(d => d.ID == detailId);
            if (existing == null)
                return;

            db.DetailItem.Remove(existing);
            db.SaveChanges();
        }

        public static List<DetailItem> FilterExpenses(
            int projectId,
            DateTime fromDate,
            DateTime toDate,
            int? categoryId = null,
            string? searchText = null)
        {
            var start = fromDate.Date;
            var endExclusive = toDate.Date.AddDays(1);

            using var db = new DBContext();
            var categories = db.TreeItem.AsNoTracking()
                .Where(t => t.ProjectID == projectId)
                .ToDictionary(t => t.ID, t => t.Title);

            if (categories.Count == 0)
                return new List<DetailItem>();

            var targetCategoryIds = categoryId.HasValue && categories.ContainsKey(categoryId.Value)
                ? new List<int> { categoryId.Value }
                : categories.Keys.ToList();

            var items = db.DetailItem.AsNoTracking()
                .Where(d => targetCategoryIds.Contains(d.ParentID)
                         && d.Date >= start
                         && d.Date < endExclusive)
                .OrderByDescending(d => d.Date)
                .ThenByDescending(d => d.ID)
                .ToList();

            foreach (var item in items)
            {
                if (categories.TryGetValue(item.ParentID, out var catTitle))
                    item.CategoryTitle = catTitle;
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var q = searchText.Trim();
                items = items.Where(d =>
                    (!string.IsNullOrEmpty(d.Note) && d.Note.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(d.CategoryTitle) && d.CategoryTitle.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    d.Amount.ToString("0.##", CultureInfo.InvariantCulture).Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return items;
        }

        public static bool TryParseAmount(string? input, bool allowZero, out decimal amount)
        {
            amount = 0m;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var sb = new StringBuilder(input.Length);
            foreach (char ch in input.Trim())
            {
                if (ch >= '\u0660' && ch <= '\u0669') // Arabic-Indic digits
                    sb.Append((char)('0' + (ch - '\u0660')));
                else if (ch >= '\u06F0' && ch <= '\u06F9') // Extended Arabic-Indic digits
                    sb.Append((char)('0' + (ch - '\u06F0')));
                else if (ch == '\u066B' || ch == '،') // Arabic decimal/comma
                    sb.Append(',');
                else if (!char.IsWhiteSpace(ch) && ch != '\u066C')
                    sb.Append(ch);
            }

            var cleaned = sb.ToString();
            if (string.IsNullOrEmpty(cleaned))
                return false;

            int lastDot = cleaned.LastIndexOf('.');
            int lastComma = cleaned.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                if (lastComma > lastDot)
                {
                    cleaned = cleaned.Replace(".", string.Empty).Replace(',', '.');
                }
                else
                {
                    cleaned = cleaned.Replace(",", string.Empty);
                }
            }
            else if (lastComma >= 0)
            {
                int commaCount = cleaned.Count(c => c == ',');
                cleaned = commaCount == 1
                    ? cleaned.Replace(',', '.')
                    : cleaned.Replace(",", string.Empty);
            }

            if (!decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out amount))
                return false;

            if (allowZero ? amount < 0m : amount <= 0m)
                return false;

            if (amount > 999_999_999_999m)
                return false;

            return true;
        }

        public static string FormatCurrency(decimal amount)
        {
            return $"{amount:N2} دج";
        }
    }
}

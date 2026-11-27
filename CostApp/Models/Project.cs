namespace CostApp.Models
{
    public class Project
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Computed summary properties (not mapped to DB)
        public decimal TotalSpent { get; set; }
        public int ExpenseCount { get; set; }
        public decimal RemainingBudget => Budget - TotalSpent;
        public bool HasBudget => Budget > 0;
    }
}

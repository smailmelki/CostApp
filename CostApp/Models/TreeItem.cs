namespace CostApp.Models
{
    public class TreeItem
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<DetailItem>? Details { get; set; }

        public decimal Total => Details?.Sum(detail => detail.Amount) ?? 0m;
    }
}

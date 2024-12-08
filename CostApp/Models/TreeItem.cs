namespace CostApp.Models
{
    public class TreeItem
    {
        public int ID { get; set; }
        public List<DetailItem>? Details { get; set; }
        public string? Title { get; set; }
        public decimal Total
        {
            get
            {
                return Details?.Sum(detail => detail.Amount) ?? 0;
            }
        }
    }
}

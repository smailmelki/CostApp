namespace CostApp.Models
{
    public class DetailItem
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}

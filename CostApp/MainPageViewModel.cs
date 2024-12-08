using CostApp.Models;

namespace CostApp
{
    public class MainPageViewModel
    {
        public List<TreeItem> Items { get; set; }
        // خاصية لحساب المجموع الكلي لجميع العناصر
        public decimal GrandTotal
        {
            get
            {
                return Items?.Sum(item => item.Total) ?? 0;
            }
        }
        public MainPageViewModel()
        {
            Items = new List<TreeItem>
            {
                new TreeItem
                {
                    Title = "Item 1",
                    Details = new List<DetailItem>
                    {
                        new DetailItem {ID = 0, Date = DateTime.Now, Amount = 500 },
                        new DetailItem {ID = 1, Date = DateTime.Now.AddDays(-1), Amount = 1000 },
                        new DetailItem {ID = 2, Date = DateTime.Now.AddDays(-2), Amount = 650 }
                    }
                },
                new TreeItem
                {
                    Title = "Item 2",
                    Details = new List<DetailItem>
                    {
                        new DetailItem {ID = 3, Date = DateTime.Now, Amount = 800 },
                        new DetailItem {ID = 4, Date = DateTime.Now.AddDays(-2), Amount = 1500 }
                    }
                }
            };
        }
    }
}

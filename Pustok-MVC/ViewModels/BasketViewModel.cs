namespace Pustok_MVC.ViewModels
{
    public class BasketViewModel
    {
        public int BookId { get; set; }
        public int Count { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public double Price { get; set; }
        public double TotalAmount { get; set; }
    }
}

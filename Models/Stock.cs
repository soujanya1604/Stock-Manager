namespace Stock_Manager.Models
{
    public class Stock
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal LatestPrice { get; set; }
        public DateTime LastUpdated { get; set; }
    }

}

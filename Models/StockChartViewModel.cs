namespace Stock_Manager.Models
{
    public class StockChartViewModel
    {
        public string Symbol { get; set; }
        public string[] Dates { get; set; }
        public decimal[] ClosingPrices { get; set; }
        public decimal[] OpeningPrices { get; set; }
        public decimal[] HighPrices { get; set; }
        public decimal[] LowPrices { get; set; }
        public int Id { get; set; } // This array will hold the Ids of each stock entry
    }
}

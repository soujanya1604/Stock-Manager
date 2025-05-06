namespace Stock_Manager.Models
{
    public class StockChartViewModel
    {
        public string Symbol { get; set; }
        public string[] Dates { get; set; }
        public decimal[] ClosingPrices { get; set; }
    }

}

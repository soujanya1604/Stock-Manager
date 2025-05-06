namespace Stock_Manager.Models
{
    public class PortfolioStock
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public int StockId { get; set; }
        public DateTime Date { get; set; } // Ensure you have a Date field to allow editing the date
        public decimal ClosingPrice { get; set; } // Add ClosingPrice to store the stock price
        public int Quantity { get; set; }

        // Navigation properties
        public Portfolio Portfolio { get; set; }
        public Stock Stock { get; set; }
    }

}

namespace Stock_Manager.Models
{
    public class PortfolioStock
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public int StockId { get; set; }
        public int Quantity { get; set; }

        public Portfolio Portfolio { get; set; }
        public Stock Stock { get; set; }
    }

}

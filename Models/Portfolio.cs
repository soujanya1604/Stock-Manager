namespace Stock_Manager.Models
{
    public class Portfolio
    {
        public int Id { get; set; }
        public string UserId { get; set; }  // Assuming user authentication
        public string Name { get; set; }
        public ICollection<PortfolioStock> PortfolioStocks { get; set; }
    }

}

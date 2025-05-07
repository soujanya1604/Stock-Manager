using System.ComponentModel.DataAnnotations;

namespace Stock_Manager.Models
{
    public class PortfolioStock
    {
        [Key]
        public int Id { get; set; }
        public int StockId { get; set; }
        public DateTime Date { get; set; }
        public decimal ClosingPrice { get; set; }
        public decimal OpeningPrice { get; set; }  
        public decimal HighPrice { get; set; }    
        public decimal LowPrice { get; set; }     
        public int Quantity { get; set; }

        // Navigation property for the related Stock
        public Stock Stock { get; set; }
    }


}

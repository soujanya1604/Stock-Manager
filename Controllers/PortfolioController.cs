using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stock_Manager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Stock_Manager.Controllers
{
    public class PortfolioController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor to inject the database context
        public PortfolioController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var portfolioStocks = await _context.PortfolioStocks
                .Include(ps => ps.Stock)  // Include stock details in the query
                .ToListAsync();

            var stockData = portfolioStocks.Select(ps =>
            {
                var currentPrice = ps.Stock.Symbol == "AAPL" ? 150m : 200m; // Example logic for current price
                var profitLoss = (currentPrice - ps.OpeningPrice) * ps.Quantity;

                return new PortfolioStockViewModel
                {
                    Id = ps.Id,  // Ensure the Id is passed to the view model
                    StockSymbol = ps.Stock.Symbol,
                    Quantity = ps.Quantity,
                    OpeningPrice = ps.OpeningPrice,
                    ProfitLoss = profitLoss
                };
            }).ToList();

            return View(stockData);  // Return the portfolio data with the Ids
        }


        // Sample method to get the current price (implement fetching real-time data here)
        private decimal GetCurrentPrice(string symbol)
        {
            if (symbol == "AAPL") return 150m;
            if (symbol == "GOOGL") return 200m;
            // Add more logic here for other symbols or fetch from an API
            return 100m;  // Default price if not found
        }


        // GET: Portfolio/Create
        public IActionResult Create()
        {
            var portfolioStock = new PortfolioStock
            {
                Stock = new Stock() // Ensure stock is initialized
            };

            var stockSymbols = new List<string> { "AAPL", "GOOGL", "MSFT", "AMZN" };
            ViewBag.StockSymbols = new SelectList(stockSymbols.Select(s => new SelectListItem
            {
                Text = s,
                Value = s
            }), "Value", "Text");

            return View(portfolioStock);
        }

        // POST: Portfolio/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PortfolioStock portfolioStock)
        {
            if (ModelState.IsValid)
            {
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.Symbol == portfolioStock.Stock.Symbol);

                if (stock == null)
                {
                    stock = new Stock { Symbol = portfolioStock.Stock.Symbol };
                    _context.Stocks.Add(stock);
                    await _context.SaveChangesAsync();
                }

                // Add stock to the portfolio with initial quantity
                portfolioStock.StockId = stock.Id;
                _context.PortfolioStocks.Add(portfolioStock);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(portfolioStock);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var portfolioStock = await _context.PortfolioStocks
                .Include(ps => ps.Stock)
                .FirstOrDefaultAsync(ps => ps.Id == id);

            if (portfolioStock == null)
            {
                return NotFound(); // If no stock found, return NotFound
            }

            // List of stock symbols to display in the dropdown
            var stockSymbols = new List<string> { "AAPL", "GOOGL", "MSFT", "AMZN" };
            ViewBag.StockSymbols = new SelectList(stockSymbols, portfolioStock.Stock.Symbol); // Set the selected symbol

            var viewModel = new PortfolioStockViewModel
            {
                Id = portfolioStock.Id,
                StockSymbol = portfolioStock.Stock.Symbol, 
                Quantity = portfolioStock.Quantity,
                OpeningPrice = portfolioStock.OpeningPrice,
            };

            return View(viewModel); // Pass the view model to the view
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PortfolioStockViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound(); // If IDs don't match, return NotFound
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Find the PortfolioStock record
                    var portfolioStock = await _context.PortfolioStocks
                        .FirstOrDefaultAsync(ps => ps.Id == id);

                    if (portfolioStock == null)
                    {
                        return NotFound(); // If the portfolio stock is not found, return NotFound
                    }

                    // Update the portfolio stock properties
                    portfolioStock.Quantity = viewModel.Quantity;
                    portfolioStock.OpeningPrice = viewModel.OpeningPrice;

                    // Find the corresponding Stock and update its symbol if necessary
                    var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol == viewModel.StockSymbol);
                    if (stock == null)
                    {
                        // If the stock doesn't exist, create a new one (if applicable)
                        stock = new Stock { Symbol = viewModel.StockSymbol };
                        _context.Stocks.Add(stock);
                        await _context.SaveChangesAsync();
                    }
                    portfolioStock.StockId = stock.Id;

                    // Save changes to the database
                    _context.Update(portfolioStock);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index)); // Redirect to the portfolio index after successful update
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PortfolioStockExists(viewModel.Id))
                    {
                        return NotFound(); // If the stock record no longer exists
                    }
                    else
                    {
                        throw;  // Rethrow the exception if another issue occurs
                    }
                }
            }

            return View(viewModel); // Return the view with validation errors (if any)
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var portfolioStock = await _context.PortfolioStocks
                .Include(ps => ps.Stock)
                .FirstOrDefaultAsync(ps => ps.Id == id);

            if (portfolioStock == null)
            {
                return NotFound();
            }

            // Display the stock and its current quantity to the user for deletion confirmation
            var model = new DeleteStockViewModel
            {
                StockSymbol = portfolioStock.Stock.Symbol,
                Quantity = portfolioStock.Quantity
            };

            return View(model);
        }

        // POST: Portfolio/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int quantityToDelete)
        {
            var portfolioStock = await _context.PortfolioStocks.FindAsync(id);

            if (portfolioStock == null)
            {
                return NotFound();
            }

            if (quantityToDelete <= 0 || quantityToDelete > portfolioStock.Quantity)
            {
                // If the quantity to delete is invalid (either 0 or greater than the quantity), return an error
                ModelState.AddModelError(string.Empty, "Invalid quantity. Please specify a quantity less than or equal to the number of stocks you own.");
                return View(new DeleteStockViewModel
                {
                    StockSymbol = portfolioStock.Stock.Symbol,
                    Quantity = portfolioStock.Quantity
                });
            }

            if (quantityToDelete == portfolioStock.Quantity)
            {
                // If the user wants to delete all, remove the stock from the portfolio
                _context.PortfolioStocks.Remove(portfolioStock);
            }
            else
            {
                // If the user wants to reduce the quantity, just update the quantity
                portfolioStock.Quantity -= quantityToDelete;
                _context.Update(portfolioStock);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    // ViewModel for Delete confirmation
    

        // Helper method to check if a PortfolioStock exists
        private bool PortfolioStockExists(int id)
        {
            return _context.PortfolioStocks.Any(e => e.Id == id);
        }
    }
}

public class PortfolioStockViewModel
{
    public int Id { get; set; }
    public string StockSymbol { get; set; }
    public int Quantity { get; set; }
    public decimal OpeningPrice { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ClosingPrice { get; set; } // Assuming ClosingPrice is stored in PortfolioStock
}


public class DeleteStockViewModel
{
    public string StockSymbol { get; set; }
    public string StockName { get; set; }
    public int Quantity { get; set; }
    public int QuantityToDelete { get; set; }  // This is the number of stocks to delete
}


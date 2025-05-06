using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stock_Manager.Models;

namespace Stock_Manager.Controllers
{
    public class StockController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;

        public StockController(IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        private const string ApiKey = " 9FA7ZI8MRQFWLKIB";
        string symbol = "AAPL";

        // Action method for getting stock data
        public async Task<IActionResult> GetStockPrice()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={ApiKey}");
            dynamic data = JsonConvert.DeserializeObject(response);
            if (data["Time Series (Daily)"] != null)
            {
                var timeSeries = data["Time Series (Daily)"];
                var dates = new List<string>();
                var closingPrices = new List<decimal>();
                // Prepare the data for charting
                foreach (var dayData in timeSeries)
                {
                    var dateString = dayData.Name;  // Get the date as a string
                    var closingPrice = dayData.Value["4. close"];
                    // Parse the string to DateTime
                    DateTime date;
                    if (!DateTime.TryParse(dateString, out date))
                    {
                        continue;  // Skip this iteration if the date is not valid
                    }
                    dates.Add(dateString);
                    closingPrices.Add(decimal.Parse(closingPrice.ToString()));
                    // Now you can safely compare the parsed date in the database query
                    var existingStock = await _context.PortfolioStocks
                        .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == date.Date);  // Compare using DateTime
                    if (existingStock == null)
                    {
                        var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol);
                        if (stock == null)
                        {
                            // If the stock doesn't exist in the database, add it
                            stock = new Stock { Symbol = symbol, Name = "Default Stock" }; // You can customize the stock details here
                            _context.Stocks.Add(stock);
                            await _context.SaveChangesAsync();
                        }
                        var portfolioStock = new PortfolioStock
                        {
                            StockId = stock.Id,
                            Date = date.Date,  // Store the date without time
                            ClosingPrice = decimal.Parse(closingPrice.ToString()), // Save closing price here
                            Quantity = 0 // Default to 0 or set it based on user input or logic
                        };
                        _context.PortfolioStocks.Add(portfolioStock);
                    }
                }
                await _context.SaveChangesAsync(); // Save all changes to the database
                var stockData = new StockChartViewModel
                {
                    Symbol = symbol,
                    Dates = dates.ToArray(),
                    ClosingPrices = closingPrices.ToArray()
                };
                return View(stockData); // Pass the stock data to the view
            }
            return NotFound($"No data found for symbol {symbol}.");
        }

        public async Task<IActionResult> Edit(string symbol, string date)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(date))
            {
                return NotFound();
            }

            // Convert date from string to DateTime
            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            // Fetch the portfolio stock based on the symbol and date
            var portfolioStock = await _context.PortfolioStocks
                .Include(ps => ps.Stock) // Include stock details
                .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == parsedDate);

            if (portfolioStock == null)
            {
                return NotFound();
            }

            return View(portfolioStock); // Return the stock data to the view
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, PortfolioStock portfolioStock)
        {
            if (id != portfolioStock.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(portfolioStock); // Update the portfolio stock with the edited data
                    await _context.SaveChangesAsync(); // Save the changes to the database
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PortfolioStockExists(portfolioStock.Id)) // Check if the stock still exists in the database
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("GetStockPrice", "Stock", new { symbol = portfolioStock.Stock.Symbol }); // Redirect after successful edit
            }
            return View(portfolioStock); // Return the view with validation errors if any
        }


        // Delete Stock action - Display confirmation page for deletion
        public async Task<IActionResult> Delete(string symbol, string date)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(date))
            {
                return NotFound();
            }

            // Convert the date string to DateTime
            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            // Fetch the portfolio stock based on the symbol and date
            var portfolioStock = await _context.PortfolioStocks
                .Include(ps => ps.Stock) // Include stock details
                .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == parsedDate);

            if (portfolioStock == null)
            {
                return NotFound();
            }

            // Return the confirmation view to delete
            return View(portfolioStock);
        }


        // Post method to delete the stock data
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var portfolioStock = await _context.PortfolioStocks.FindAsync(id);
            _context.PortfolioStocks.Remove(portfolioStock);
            await _context.SaveChangesAsync();
            return RedirectToAction("GetStockPrice", "Stock", new { symbol = portfolioStock.Stock.Symbol });
        }

        private bool PortfolioStockExists(int id)
        {
            return _context.PortfolioStocks.Any(e => e.Id == id);
        }

    }
}

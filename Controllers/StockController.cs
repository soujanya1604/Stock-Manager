using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        private const string ApiKey = "GX96NSRXWQ7MG763";
        //string symbol = "AAPL";

        // Action method for getting stock data
        public async Task<IActionResult> GetStockPrice(string symbol)
        {
            // Default to AAPL if no symbol is selected
            if (string.IsNullOrEmpty(symbol))
            {
                symbol = "AAPL";  // Default symbol
            }

            var stockSymbols = new List<string> { "AAPL", "GOOGL", "MSFT", "AMZN" };
            ViewBag.StockSymbols = new SelectList(stockSymbols, symbol);  // Populate dropdown with stock symbols

            // Fetch stock data based on the symbol
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={ApiKey}");

            dynamic data = JsonConvert.DeserializeObject(response);

            if (data["Time Series (Daily)"] != null)
            {
                var timeSeries = data["Time Series (Daily)"];
                var dates = new List<string>();
                var closingPrices = new List<decimal>();
                var openingPrices = new List<decimal>();
                var highPrices = new List<decimal>();
                var lowPrices = new List<decimal>();

                foreach (var dayData in timeSeries)
                {
                    var dateString = dayData.Name;
                    var closingPrice = dayData.Value["4. close"];
                    var openingPrice = dayData.Value["1. open"];
                    var highPrice = dayData.Value["2. high"];
                    var lowPrice = dayData.Value["3. low"];

                    // Parse the string to DateTime
                    DateTime date;
                    if (DateTime.TryParse(dateString, out date))
                    {
                        dates.Add(dateString);
                        closingPrices.Add(decimal.Parse(closingPrice.ToString()));
                        openingPrices.Add(decimal.Parse(openingPrice.ToString()));
                        highPrices.Add(decimal.Parse(highPrice.ToString()));
                        lowPrices.Add(decimal.Parse(lowPrice.ToString()));

                        // Fetch the existing stock from the database
                        var existingStock = await _context.PortfolioStocks
                            .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == date.Date);

                        if (existingStock == null)
                        {
                            // If the stock doesn't exist in the database, create it
                            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol);
                            if (stock == null)
                            {
                                stock = new Stock { Symbol = symbol, Name = "Default Stock" };
                                _context.Stocks.Add(stock);
                                await _context.SaveChangesAsync();
                            }

                            var portfolioStock = new PortfolioStock
                            {
                                StockId = stock.Id,
                                Date = date.Date,  // Store the date without time
                                ClosingPrice = decimal.Parse(closingPrice.ToString()), 
                                OpeningPrice = decimal.Parse(openingPrice.ToString()), 
                                HighPrice = decimal.Parse(highPrice.ToString()),       
                                LowPrice = decimal.Parse(lowPrice.ToString()),        
                                Quantity = 0  
                            };

                            _context.PortfolioStocks.Add(portfolioStock); 
                            await _context.SaveChangesAsync();  
                        }
                    }
                }

                // Create the view model
                var stockData = new StockChartViewModel
                {
                    Symbol = symbol,
                    Dates = dates.ToArray(),
                    ClosingPrices = closingPrices.ToArray(),
                    OpeningPrices = openingPrices.ToArray(),
                    HighPrices = highPrices.ToArray(),
                    LowPrices = lowPrices.ToArray()
                };

                // Store stock data in ViewBag
                ViewBag.StockData = stockData;
                ViewBag.SelectedSymbol = symbol;  // Store the selected symbol

                return View(stockData);  // Return the view with the stock data
            }

            return NotFound($"No data found for symbol {symbol}.");  // Handle the case if no data is found
        }

        // Edit method (GET) to fetch stock data based on ID, symbol, and date
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


        // Post method to save the edited stock data
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // Delete Stock action (GET) to confirm deletion
        public async Task<IActionResult> Delete(int id, string symbol, string date)
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

            // Fetch the portfolio stock based on the id, symbol, and date
            var portfolioStock = await _context.PortfolioStocks
                .Include(ps => ps.Stock) // Include stock details
                .FirstOrDefaultAsync(ps => ps.Id == id && ps.Stock.Symbol == symbol && ps.Date == parsedDate);

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
            _context.PortfolioStocks.Remove(portfolioStock); // Remove the stock from the database
            await _context.SaveChangesAsync(); // Save the changes
            return RedirectToAction("GetStockPrice", "Stock", new { symbol = portfolioStock.Stock.Symbol }); // Redirect after successful deletion
        }

        private bool PortfolioStockExists(int id)
        {
            return _context.PortfolioStocks.Any(e => e.Id == id);
        }
    }
}

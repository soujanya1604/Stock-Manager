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

            var stockSymbols = new List<string>
{
    "AAPL",  // Apple Inc.
    "GOOGL", // Alphabet Inc. (Google)
    "MSFT",  // Microsoft Corporation
    "AMZN",  // Amazon.com, Inc.
    "TSLA",  // Tesla, Inc.
    "FB",    // Meta Platforms, Inc. (formerly Facebook)
    "NFLX",  // Netflix, Inc.
    "NVDA",  // NVIDIA Corporation
    "INTC",  // Intel Corporation
    "AMD",   // Advanced Micro Devices, Inc.
    "BA",    // The Boeing Company
    "DIS",   // The Walt Disney Company
    "V",     // Visa Inc.
    "MA",    // Mastercard Incorporated
    "PYPL",  // PayPal Holdings, Inc.
    "CSCO",  // Cisco Systems, Inc.
    "IBM",   // International Business Machines Corporation
    "BABA",  // Alibaba Group Holding Limited
    "SPY",   // SPDR S&P 500 ETF Trust
    "QQQ",   // Invesco QQQ Trust
    "XOM",   // Exxon Mobil Corporation
    "CVX",   // Chevron Corporation
    "GE",    // General Electric Company
    "WMT",   // Walmart Inc.
    "KO",    // The Coca-Cola Company
    "PEP",   // PepsiCo, Inc.
    "MCD",   // McDonald's Corporation
    "NKE",   // NIKE, Inc.
    "T",     // AT&T Inc.
    "VZ",    // Verizon Communications Inc.
    "GM",    // General Motors Company
    "F",     // Ford Motor Company
    "SLB",   // Schlumberger Limited
    "PFE",   // Pfizer Inc.
    "MRK",   // Merck & Co., Inc.
    "JNJ",   // Johnson & Johnson
    "AMGN",  // Amgen Inc.
    "GILD",  // Gilead Sciences, Inc.
    "BMY",   // Bristol-Myers Squibb Company
    "LLY"    // Eli Lilly and Company
};
            ViewBag.StockSymbols = new SelectList(stockSymbols, symbol);  // Populate dropdown with stock symbols

            // Check if the stock data exists in the in-memory database
            var stockDataFromDb = await _context.PortfolioStocks
                .Where(ps => ps.Stock.Symbol == symbol)
                .OrderByDescending(ps => ps.Date)  // Sorting by most recent date
                .ToListAsync();

            if (stockDataFromDb.Any()) // If data exists in the database, use that data
            {
                // Convert the database data into the format used by the view model
                var dates = stockDataFromDb.Select(ps => ps.Date.ToString("yyyy-MM-dd")).ToArray();
                var closingPrices = stockDataFromDb.Select(ps => ps.ClosingPrice).ToArray();
                var openingPrices = stockDataFromDb.Select(ps => ps.OpeningPrice).ToArray();
                var highPrices = stockDataFromDb.Select(ps => ps.HighPrice).ToArray();
                var lowPrices = stockDataFromDb.Select(ps => ps.LowPrice).ToArray();

                // Create the view model
                var stockData = new StockChartViewModel
                {
                    Symbol = symbol,
                    Dates = dates,
                    ClosingPrices = closingPrices,
                    OpeningPrices = openingPrices,
                    HighPrices = highPrices,
                    LowPrices = lowPrices
                };

                // Store stock data in ViewBag for rendering in the view
                ViewBag.StockData = stockData;
                ViewBag.SelectedSymbol = symbol;

                return View(stockData);  // Return the view with the stock data from the database
            }
            else
            {
                // If no data exists in the database, call the API to fetch data
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
                            // Check if the date is within the past 10 days
                            if (date >= DateTime.Now.AddDays(-10))
                            {
                                dates.Add(dateString);
                                closingPrices.Add(decimal.Parse(closingPrice.ToString()));
                                openingPrices.Add(decimal.Parse(openingPrice.ToString()));
                                highPrices.Add(decimal.Parse(highPrice.ToString()));
                                lowPrices.Add(decimal.Parse(lowPrice.ToString()));

                                // Store the stock data in the in-memory database (if not already present)
                                var existingStock = await _context.PortfolioStocks
                                    .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == date.Date);

                                if (existingStock == null)
                                {
                                    var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol);
                                    if (stock == null)
                                    {
                                        stock = new Stock { Symbol = symbol };
                                        _context.Stocks.Add(stock);
                                        await _context.SaveChangesAsync();
                                    }

                                    var portfolioStock = new PortfolioStock
                                    {
                                        StockId = stock.Id,
                                        Date = date.Date,
                                        ClosingPrice = decimal.Parse(closingPrice.ToString()),
                                        OpeningPrice = decimal.Parse(openingPrice.ToString()),
                                        HighPrice = decimal.Parse(highPrice.ToString()),
                                        LowPrice = decimal.Parse(lowPrice.ToString()),
                                        Quantity = 0  // Default quantity (could be set as needed)
                                    };

                                    _context.PortfolioStocks.Add(portfolioStock);
                                    await _context.SaveChangesAsync();
                                }
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

                    // Store stock data in ViewBag for rendering in the view
                    ViewBag.StockData = stockData;
                    ViewBag.SelectedSymbol = symbol;

                    return View(stockData);  // Return the view with the stock data from the API
                }

                return NotFound($"No data found for symbol {symbol}.");  // Handle the case if no data is found from the API
            }
        }


        public async Task<IActionResult> Edit(string symbol, string date)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(date))
            {
                return NotFound(); // Return NotFound if no symbol or date is passed
            }

            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
            {
                return BadRequest("Invalid date format."); // Handle invalid date format
            }

            // Fetch the portfolio stock based on symbol and date
            var portfolioStock = await _context.PortfolioStocks
                .Include(ps => ps.Stock) // Include stock details for edit
                .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == parsedDate);

            if (portfolioStock == null)
            {
                return NotFound(); // If stock data is not found, return NotFound
            }

            // Pass the Date as a formatted string to the view
            ViewBag.FormattedDate = parsedDate.ToString("yyyy-MM-dd");

            return View(portfolioStock); // Return portfolio stock for editing
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string symbol, string date, PortfolioStock portfolioStock)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(date))
            {
                return NotFound(); // Ensure symbol and date are passed
            }

            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            // Fetch the existing stock data based on symbol and date
            var existingPortfolioStock = await _context.PortfolioStocks
                .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == parsedDate);

            if (existingPortfolioStock == null)
            {
                return NotFound(); // Return NotFound if stock does not exist
            }

            // Update the stock data with new values from the form
            existingPortfolioStock.ClosingPrice = portfolioStock.ClosingPrice;
            existingPortfolioStock.OpeningPrice = portfolioStock.OpeningPrice;
            existingPortfolioStock.HighPrice = portfolioStock.HighPrice;
            existingPortfolioStock.LowPrice = portfolioStock.LowPrice;
            existingPortfolioStock.Quantity = portfolioStock.Quantity;

            // Save the changes to the database
            _context.Update(existingPortfolioStock);
            await _context.SaveChangesAsync();

            // Redirect back to the stock price page to see updated data
            return RedirectToAction("GetStockPrice", "Stock", new { symbol = symbol });
        }


        // Delete Stock action (GET) - Display confirmation page for deletion
        public async Task<IActionResult> Delete(string symbol, string date)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(date))
            {
                return NotFound(); // Return NotFound if no symbol or date is passed
            }

            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            // Fetch the portfolio stock based on symbol and date
            var portfolioStock = await _context.PortfolioStocks
                .Include(ps => ps.Stock) // Include stock details
                .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == parsedDate);

            if (portfolioStock == null)
            {
                return NotFound(); // If stock does not exist, return NotFound
            }

            // Return the confirmation view for deletion
            return View(portfolioStock);
        }


        // Post method to delete the stock data (DELETE)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string symbol, string date)
        {
            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
            {
                return BadRequest("Invalid date format."); // Handle invalid date format
            }

            // Fetch the portfolio stock based on symbol and date
            var portfolioStock = await _context.PortfolioStocks
                .FirstOrDefaultAsync(ps => ps.Stock.Symbol == symbol && ps.Date == parsedDate);

            if (portfolioStock == null)
            {
                return NotFound(); // If stock does not exist, return NotFound
            }

            // Remove the stock data from the in-memory database
            _context.PortfolioStocks.Remove(portfolioStock);
            await _context.SaveChangesAsync();  // Save the changes

            // Redirect back to the GetStockPrice action after successful deletion
            return RedirectToAction("GetStockPrice", "Stock", new { symbol = symbol });
        }


        private bool PortfolioStockExists(int id)
        {
            return _context.PortfolioStocks.Any(e => e.Id == id);
        }

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
            }), "Value", "Text"); // This creates SelectList with stock symbol as both Value and Text

            return View(portfolioStock);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PortfolioStock portfolioStock)
        {
            if (portfolioStock.Stock.Id == 0)
            {
                // Set the default stock or fetch from the database
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.Symbol == portfolioStock.Stock.Symbol);

                if (stock == null)
                {
                    // Create a new stock entry if it doesn't exist in the database
                    stock = new Stock { Symbol = portfolioStock.Stock.Symbol };
                    _context.Stocks.Add(stock);
                    await _context.SaveChangesAsync();
                }

                // Assign the stock ID to the PortfolioStock
                portfolioStock.StockId = stock.Id;
            }

            // Now check if the model state is valid
            if (ModelState.IsValid)
            {
                // Add the new portfolio stock to the database
                _context.PortfolioStocks.Add(portfolioStock);
                await _context.SaveChangesAsync();

                // Redirect to the stock price page after successfully creating the stock
                return RedirectToAction("GetStockPrice", "Stock", new { symbol = portfolioStock.Stock.Symbol });
            }
            else
            {
                // Log or debug to check which validation errors are present
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(portfolioStock);
            }
        }

    }
}

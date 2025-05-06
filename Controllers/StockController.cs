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

        private const string ApiKey = "your_alpha_vantage_api_key"; // Replace with your API key

        // Action method for getting stock data
        public async Task<IActionResult> GetStockPrice(string symbol)
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
                    var date = dayData.Name;
                    var closingPrice = dayData.Value["4. close"];

                    dates.Add(date);
                    closingPrices.Add(decimal.Parse(closingPrice.ToString()));
                }

                var stockData = new StockChartViewModel
                {
                    Symbol = symbol,
                    Dates = dates.ToArray(),
                    ClosingPrices = closingPrices.ToArray()
                };

                return View(stockData); // Pass the stock data to the view
            }

            return NotFound();
        }

    }
}

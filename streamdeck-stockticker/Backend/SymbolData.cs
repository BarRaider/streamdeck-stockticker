using System.Linq;

namespace StockTicker.Backend
{
    public class SymbolData
    {
        private static readonly string[] CLOSED_MARKET_STRINGS = new string[] { "Close", "CLOSED" };

        public string SymbolName { get; private set; }
        public StockQuote Quote { get; private set; }
        public ChartBase[] Chart { get; private set; }

        public bool IsMarketClosed => CLOSED_MARKET_STRINGS.Any(s => Quote.LatestSource == s);


        public SymbolData(string symbol, StockQuote quote, ChartBase[] chart)
        {
            SymbolName = symbol;
            Quote = quote;
            Chart = chart;
        }


    }
}

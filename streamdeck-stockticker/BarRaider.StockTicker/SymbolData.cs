namespace BarRaider.StockTicker
{
    public class SymbolData
    {
        public string SymbolName { get; private set; }
        public StockQuote Quote { get; private set; }
        public ChartBase[] Chart { get; private set; }

        public SymbolData(string symbol, StockQuote quote, ChartBase[] chart)
        {
            SymbolName = symbol;
            Quote = quote;
            Chart = chart;
        }
    }
}

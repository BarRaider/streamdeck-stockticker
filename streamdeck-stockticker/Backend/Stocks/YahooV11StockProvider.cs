using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using StockTicker.Backend.Models;
using StockTicker.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace StockTicker.Backend.Stocks
{

    public class YahooV11StockProvider : IStockInfoProvider
    {
        #region Private Members

        private const string STOCK_URI = @"https://query2.finance.yahoo.com/v11/finance/quoteSummary/{0}?modules=price";

        private static YahooV11StockProvider instance = null;
        private static readonly object objLock = new object();
        private static readonly Dictionary<string, SymbolCache> dictSymbolCache = new Dictionary<string, SymbolCache>();

        #endregion

        #region Constructors

        public static YahooV11StockProvider Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new YahooV11StockProvider();
                    }
                    return instance;
                }
            }
        }

        private YahooV11StockProvider()
        {
        }

        public bool TokenExists()
        {
            return true;
        }

        public async Task<SymbolData> GetSymbol(string stockSymbol, int cooldownTimeMs)
        {
            if (!TokenExists())
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} GetSymbol was called without a valid token");
                return null;
            }

            try
            {
                // Fetch precached version if relevant
                if (dictSymbolCache.ContainsKey(stockSymbol))
                {
                    var symbolCache = dictSymbolCache[stockSymbol];
                    if (symbolCache != null && (DateTime.Now - symbolCache.LastRefresh).TotalMilliseconds <= cooldownTimeMs)
                    {
                        return symbolCache.SymbolData;
                    }
                }

                string queryUrl = String.Format(STOCK_URI, stockSymbol);
                HttpResponseMessage response = await StockQuery(queryUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(body);

                    // Invalid Stock Symbol
                    if (obj.Count == 0)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetSymbol invalid symbol: {stockSymbol}");
                        return null;
                    }

                    JToken res = obj["quoteSummary"]["result"].First;

                    StockQuote quote = CreateStockQuote(res);
                    if (quote != null)
                    {
                        var symbolData = new SymbolData(quote?.Symbol, quote);

                        Logger.Instance.LogMessage(TracingLevel.DEBUG, $"DEBUG: Symbol {stockSymbol} Dict: {dictSymbolCache.Count} Quote: {quote.Symbol} SymbolData: {symbolData.SymbolName}");
    
                        dictSymbolCache[stockSymbol] = new SymbolCache(DateTime.Now, symbolData);
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} GetSymbol retrieved Symbol: {stockSymbol}");
                        return symbolData;
                    }
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetSymbol obj invalid response: {response.StatusCode}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        TokenManager.Instance.SetStockTokenFailed();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetSymbol Exception for symbol {stockSymbol}: {ex}");
            }
            return null;
        }

        public void SetStockToken(string token)
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"SetStockToken called but {this.GetType()} does not require a token");
        }

        #endregion

        #region Private Methods

        private async Task<HttpResponseMessage> StockQuery(string uriPath, List<KeyValuePair<string, string>> optionalContent)
        {
            string queryParams = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 10);

                if (optionalContent != null)
                {
                    List<string> paramList = new List<string>();
                    foreach (var kvp in optionalContent)
                    {
                        paramList.Add($"{kvp.Key}={kvp.Value}");
                    }
                    queryParams = "?" + string.Join("&", paramList);
                }
                return await client.GetAsync($"{uriPath}{queryParams}");
            }
        }

        private StockQuote CreateStockQuote(JToken quoteInfo)
        {
            if (quoteInfo == null)
            {
                return null;
            }

            var quoteData = quoteInfo["price"];

            return new StockQuote()
            {
                Change = (double)quoteData["regularMarketChange"]["fmt"],
                ChangePercent = (double)quoteData["regularMarketChangePercent"]["raw"],
                Close = (double)quoteData["regularMarketPreviousClose"]["raw"],
                LatestPrice = (double)quoteData["regularMarketPrice"]["raw"],
                High = (double)quoteData["regularMarketDayHigh"]["raw"],
                Low = (double)quoteData["regularMarketDayLow"]["raw"],
                Symbol = (string)quoteData["symbol"],
                LatestSource = (string)quoteData["marketState"]
            };
        }

        #endregion
    }

}

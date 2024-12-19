using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using StockTicker.Backend.Models;
using StockTicker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StockTicker.Backend.Stocks
{
    internal class FinnhubStockProvider : IStockInfoProvider
    {
        private const string STOCK_URI_PREFIX = "https://finnhub.io/api/v1/";
        private const string STOCK_BATCH_CHART_QUOTE = "quote";
        private const string MARKET_STATUS = "stock/market-status";
        private const int MARKETPLACE_STATUS_COOLDOWN_MS = 600000; // 10 minutes

        #region Private Members

        private static FinnhubStockProvider instance = null;
        private static readonly object objLock = new object();
        private static readonly Dictionary<string, SymbolCache> dictSymbolCache = new Dictionary<string, SymbolCache>();
        private readonly SemaphoreSlim marketStatusLock = new SemaphoreSlim(1, 1);
        private bool isMarketOpen = true;
        private DateTime lastMarketCheck = DateTime.MinValue;

        #endregion

        #region Constructors

        public static FinnhubStockProvider Instance
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
                        instance = new FinnhubStockProvider();
                    }
                    return instance;
                }
            }
        }

        private FinnhubStockProvider()
        {
        }

        #endregion

        public bool TokenExists()
        {
            return TokenManager.Instance.StockTokenExists;
        }

        public async Task<bool?> GetIsMarketOpen()
        {
            if (!TokenExists())
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} GetMarketStatus was called without a valid API token");
                return null;
            }

            await marketStatusLock.WaitAsync();
            try
            {
                
                if ((DateTime.Now - lastMarketCheck).TotalMilliseconds <= MARKETPLACE_STATUS_COOLDOWN_MS)
                {
                    return isMarketOpen;
                }

                var kvp = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("token", TokenManager.Instance.Token.StockToken),
                    new KeyValuePair<string, string>("exchange", "US"),
                };
                HttpResponseMessage response = await StockQuery(MARKET_STATUS, kvp);
                lastMarketCheck = DateTime.Now;
                if (response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(body);

                    // Invalid Stock Symbol
                    if (obj.Count == 0)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetMarketStatus invalid response");
                        return null;
                    }

                   isMarketOpen = obj["isOpen"].ToObject<bool>();
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetMarketStatus invalid response: {response.StatusCode}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        TokenManager.Instance.SetStockTokenFailed();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetMarketStatus Exception: {ex}");
            }
            finally
            {
                marketStatusLock.Release();
            }
            return null;
        }

        public async Task<SymbolData> GetSymbol(string stockSymbol, int cooldownTimeMs)
        {
            if (!TokenExists())
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} GetSymbol was called without a valid API token");
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

                var kvp = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("token", TokenManager.Instance.Token.StockToken),
                    new KeyValuePair<string, string>("symbol", stockSymbol),
                };
                HttpResponseMessage response = await StockQuery(STOCK_BATCH_CHART_QUOTE, kvp);

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

                    bool marketOpen = await GetIsMarketOpen() ?? false;
                    //var jp = obj.Properties().First();
                    StockQuote quote = new StockQuote()
                    {
                        Change = obj["d"].ToObject<double>(),
                        LatestPrice = obj["c"].ToObject<double>(),
                        ChangePercent = obj["dp"].ToObject<double>(),
                        High = obj["h"].ToObject<double>(),
                        Low = obj["l"].ToObject<double>(),
                        Close = obj["pc"].ToObject<double>(),
                        Symbol = stockSymbol,
                        LatestSource = marketOpen ? "Open" : "Closed"
                    };

                    var symbolData = new SymbolData(quote?.Symbol, quote);
                    dictSymbolCache[stockSymbol] = new SymbolCache(DateTime.Now, symbolData);
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} GetSymbol retrieved Symbol: {stockSymbol}");
                    return symbolData;
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetSymbol invalid response: {response.StatusCode}");

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
            TokenManager.Instance.InitStockToken(token.Trim(), DateTime.Now);
        }

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
                return await client.GetAsync($"{STOCK_URI_PREFIX}{uriPath}{queryParams}");
            }
        }
    }
}

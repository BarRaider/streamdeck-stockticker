using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using StockTicker.Backend.Models;
using StockTicker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace StockTicker.Backend.Stocks
{
    internal class EXCloudStockProvider : IStockInfoProvider
    {
        //private const string STOCK_URI_PREFIX = "https://api.iextrading.com/1.0/stock/";
        private const string STOCK_URI_PREFIX = "https://cloud.iexapis.com/stable/stock/";
        private const string STOCK_BATCH_CHART_QUOTE = "market/batch";

        #region Private Members

        private static EXCloudStockProvider instance = null;
        private static readonly object objLock = new object();
        private static readonly Dictionary<string, SymbolCache> dictSymbolCache = new Dictionary<string, SymbolCache>();

        #endregion

        #region Constructors

        public static EXCloudStockProvider Instance
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
                        instance = new EXCloudStockProvider();
                    }
                    return instance;
                }
            }
        }

        private EXCloudStockProvider()
        {
        }

        #endregion

        public bool TokenExists()
        {
            return TokenManager.Instance.StockTokenExists;
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
                    new KeyValuePair<string, string>("symbols", stockSymbol),
                    new KeyValuePair<string, string>("types", "quote"),
                    new KeyValuePair<string, string>("range", "dynamic")
                };
                //kvp.Add(new KeyValuePair<string, string>("chartLast", DEFAULT_CHART_POINTS.ToString()));
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

                    var jp = obj.Properties().First();
                    StockQuote quote = jp.Value["quote"].ToObject<StockQuote>();
                    if (quote.ChangePercent.HasValue)
                    {
                        quote.ChangePercent *= 100;
                    }

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

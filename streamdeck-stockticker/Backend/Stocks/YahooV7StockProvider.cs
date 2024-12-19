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

    public class YahooV7StockProvider : IStockInfoProvider
    {
        #region Private Members

        private const string DEFAULT_COOKIES_URI = @"https://finance.yahoo.com";
        private const string STOCK_URI = @"https://query1.finance.yahoo.com/v7/finance/quote?lang=en-US&region=US&corsDomain=finance.yahoo.com&fields=symbol,marketState,regularMarketPrice,regularMarketChange,regularMarketChangePercent,preMarketPrice,preMarketChange,preMarketChangePercent,postMarketPrice,postMarketChange,postMarketChangePercent,regularMarketDayHigh,regularMarketDayLow&symbols={0}&crumb={1}";
        private const string CRUMB_URI = @"https://query2.finance.yahoo.com/v1/test/getcrumb";

        private static YahooV7StockProvider instance = null;
        private static readonly object objLock = new object();
        private static readonly Dictionary<string, SymbolCache> dictSymbolCache = new Dictionary<string, SymbolCache>();

        private HttpClient client;
        private string crumb = null;

        #endregion

        #region Constructors

        public static YahooV7StockProvider Instance
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
                        instance = new YahooV7StockProvider();
                    }
                    return instance;
                }
            }
        }

        private YahooV7StockProvider()
        {
            client = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 10)
            };

            SetHeaders(client);
            _ = client.GetAsync(DEFAULT_COOKIES_URI).GetAwaiter().GetResult();
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

                if (string.IsNullOrEmpty(crumb))
                {
                    crumb = await GetCrumb();
                }

                string queryUrl = String.Format(STOCK_URI, stockSymbol, crumb);
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

                    JToken res = obj["quoteResponse"]["result"].First;

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

        private async Task<string> GetCrumb()
        {
            var response = await client.GetAsync(CRUMB_URI);
            string body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return body;
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetCrumb failed: {response.StatusCode} {response.ReasonPhrase} {body}");
            }
            return null;
        }

        private void SetHeaders(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/112.0");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("DNT", "1");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        }

        private StockQuote CreateStockQuote(JToken quoteInfo)
        {
            if (quoteInfo == null)
            {
                return null;
            }

            return new StockQuote()
            {
                Change = (double)quoteInfo["regularMarketChange"],
                ChangePercent = (double)quoteInfo["regularMarketChangePercent"],
                Close = (double)quoteInfo["regularMarketPreviousClose"],
                LatestPrice = (double)quoteInfo["regularMarketPrice"],
                High = (double)quoteInfo["regularMarketDayHigh"],
                Low = (double)quoteInfo["regularMarketDayLow"],
                Symbol = (string)quoteInfo["symbol"],
                LatestSource = (string)quoteInfo["marketState"]
            };
        }

        #endregion
    }

}

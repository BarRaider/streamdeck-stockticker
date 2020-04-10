using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BarRaider.StockTicker
{
    internal class StockComm
    {
        private const string CURRENCY_FETCH_URI = "https://api.exchangeratesapi.io/latest?base={0}&symbols={1}";
        //private const string STOCK_URI_PREFIX = "https://api.iextrading.com/1.0/stock/";
        private const string STOCK_URI_PREFIX = "https://cloud.iexapis.com/stable/stock/";
        private const string STOCK_BATCH_CHART_QUOTE = "market/batch";
        private const string CHART_DAILY = "1m";
        private const string CHART_MINUTE = "1d";
        private const int DEFAULT_CHART_POINTS = 36;

        public bool TokenExists
        {
            get
            {
                return TokenManager.Instance.TokenExists;
            }
        }

        public async Task<SymbolData> GetSymbol(string stockSymbol)
        {
            if (!TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "GetSymbol was called without a valid token");
                return null;
            }

            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", TokenManager.Instance.Token.StockToken),
                new KeyValuePair<string, string>("symbols", stockSymbol),
                new KeyValuePair<string, string>("types", "quote"), // Remove chart as of now
                                                                    //kvp.Add(new KeyValuePair<string, string>("types", "quote,chart"));
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
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetSymbol invalid symbol: {stockSymbol}");
                    return null;
                }

                var jp = obj.Properties().First();
                StockQuote quote = jp.Value["quote"].ToObject<StockQuote>();

                ChartBase[] chart = null;
                
                /* Not used at this point
                if (jp.Value["chart"]["range"].ToString() == CHART_DAILY)
                {
                    chart = GetDailyChart(jp.Value["chart"]["data"]);
                }
                else
                {
                    chart = GetMinuteChart(jp.Value["chart"]["data"]);
                }
                */
                return new SymbolData(quote.Symbol, quote, chart);
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetSymbol invalid response: {response.StatusCode}");

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TokenManager.Instance.SetTokenFailed();
                }
            }
            return null;
        }

        public void SetStockToken(string token)
        {
            TokenManager.Instance.InitTokens(token.Trim(), DateTime.Now);
        }
        
        public async Task<JObject> FetchCurrencyData(string baseCurrency, string symbol)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = String.Format(CURRENCY_FETCH_URI, baseCurrency, symbol);
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Could not fetch data for Base: {baseCurrency} and Symbol: {symbol}");
                        return null;
                    }

                    string body = await response.Content.ReadAsStringAsync();
                    JObject obj = JObject.Parse(body);
                    return obj;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error fetching currency data {ex}");
                return null;
            }
        }

        private DailyChart[] GetDailyChart(JToken data)
        {
            return data.ToObject<DailyChart[]>();
        }

        private MinuteChart[] GetMinuteChart(JToken data)
        {
            return data.ToObject<MinuteChart[]>();
        }

        private async Task<HttpResponseMessage> StockQuery(string uriPath, List<KeyValuePair<string, string>> optionalContent)
        {
            string queryParams = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = new TimeSpan(0, 0, 10);

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

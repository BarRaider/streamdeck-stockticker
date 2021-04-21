using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using StockTicker.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker.Backend.Currency
{

    public class CurrencyManager
    {
        #region Private Members
        //private const string CURRENCY_FETCH_URI = "https://api.exchangeratesapi.io/latest?base={0}&symbols={1}";
        private const string CURRENCY_FETCH_URI = "https://free.currconv.com/api/v7/convert?q={0}&compact=ultra&apiKey={1}";
        private const int CURRENCY_FETCH_COOLDOWN_SEC = 1200; // 20 min

        private static CurrencyManager instance = null;
        private static readonly object objLock = new object();
        private static readonly Dictionary<string, CurrencyCache> dictCurrencyCache = new Dictionary<string, CurrencyCache>();

        #endregion

        #region Constructors

        public static CurrencyManager Instance
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
                        instance = new CurrencyManager();
                    }
                    return instance;
                }
            }
        }

        private CurrencyManager()
        {
        }

        #endregion

        #region Public Methods

        public bool TokenExists()
        {
            return TokenManager.Instance.CurrencyTokenExists;
        }

        internal async Task<float?> FetchCurrencyData(CurrencyType baseCurrency, CurrencyType symbol)
        {
            if (!TokenExists())
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} FetchCurrencyData was called without a valid API token");
                return null;
            }

            try
            {
                string dictKey = $"{baseCurrency}_{symbol}";
                if (dictCurrencyCache.ContainsKey(dictKey))
                {
                    var currencyCache = dictCurrencyCache[dictKey];
                    if (currencyCache != null && (DateTime.Now - currencyCache.LastRefresh).TotalSeconds <= CURRENCY_FETCH_COOLDOWN_SEC)
                    {
                        return currencyCache.Value;
                    }
                }

                Logger.Instance.LogMessage(TracingLevel.INFO, $"FetchCurrencyData called for Base: {baseCurrency} Symbol: {symbol}");
                using (HttpClient client = new HttpClient())
                {
                    string apiKey = TokenManager.Instance.Token?.CurrencyToken;
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()}: TokenManager returned empty currency API token");
                        return null;
                    }
                    string url = String.Format(CURRENCY_FETCH_URI, dictKey, apiKey);
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"FetchCurrencyData: Server failed when fetching data for Base: {baseCurrency} and Symbol: {symbol} - {response.StatusCode}");
                        // Try and parse error
                        try
                        {
                            string error = await response.Content.ReadAsStringAsync();
                            Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} Server Error: {error}");
                        }
                        catch { }
                        return null;
                    }

                    string body = await response.Content.ReadAsStringAsync();
                    JObject obj = JObject.Parse(body);
                    if (!obj.ContainsKey(dictKey))
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} FetchCurrencyData server returned invalid data: {obj}");
                        return null;
                    }
                    float value = (float)obj[dictKey];
                    dictCurrencyCache[dictKey] = new CurrencyCache(DateTime.Now, dictKey, value);
                    return value;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"FetchCurrencyData: Error fetching currency data {ex}");
                return null;
            }
        }
        internal void SetCurrencyToken(string token)
        {
            TokenManager.Instance.InitCurrencyToken(token.Trim(), DateTime.Now);
        }

        #endregion
    }

}

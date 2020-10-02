using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using StockTicker.Wrappers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker.BarRaider.StockTicker
{

    public class CurrencyManager
    {
        #region Private Members
        private const string CURRENCY_FETCH_URI = "https://api.exchangeratesapi.io/latest?base={0}&symbols={1}";

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

        public async Task<JObject> FetchCurrencyData(string baseCurrency, string symbol, int cooldownTimeMs)
        {
            try
            {
                string dictKey = $"{baseCurrency}{symbol}";
                if (dictCurrencyCache.ContainsKey(dictKey))
                {
                    var currencyCache = dictCurrencyCache[dictKey];
                    if (currencyCache != null && (DateTime.Now - currencyCache.LastRefresh).TotalMilliseconds <= cooldownTimeMs)
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"FetchCurrencyData in Cooldown for Base: {baseCurrency} Symbol: {symbol}");
                        return currencyCache.CurrencyData;
                    }
                }

                using (HttpClient client = new HttpClient())
                {
                    string url = String.Format(CURRENCY_FETCH_URI, baseCurrency, symbol);
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"FetchCurrencyData: Could not fetch data for Base: {baseCurrency} and Symbol: {symbol} - {response.StatusCode}");
                        return null;
                    }

                    string body = await response.Content.ReadAsStringAsync();
                    JObject obj = JObject.Parse(body);
                    dictCurrencyCache[dictKey] = new CurrencyCache(DateTime.Now, obj);
                    return obj;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"FetchCurrencyData: Error fetching currency data {ex}");
                return null;
            }
        }


        #endregion
    }

}

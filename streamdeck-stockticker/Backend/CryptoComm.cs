using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace StockTicker.Backend
{
    public class CryptoComm
    {
        #region Private Constants

        private const int REFRESH_SYMBOLS_SECONDS = 30;
        private const string ENDPOINT_URI = "https://api.binance.com/api/";
        private const string ENDPOINT_CURRENCIES_GENERAL_INFO = "v1/exchangeInfo";
        private const string ENDPOINT_SYMBOLS_PRICES = "v3/ticker/price";

        #endregion

        #region Private Members
        private static CryptoComm instance = null;
        private static readonly object objLock = new object();
        private List<CryptoSymbolData> latestSymbols = null;
        private List<CryptoSymbolGeneralInfo> symbolsGeneralInfo = null;
        private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1,1);
        private readonly System.Timers.Timer tmrRefreshSymbols = new System.Timers.Timer();
        private DateTime lastRefresh;
        #endregion

        #region Constructors

        public static CryptoComm Instance
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
                        instance = new CryptoComm();
                    }
                    return instance;
                }
            }
        }

        private CryptoComm()
        {
            tmrRefreshSymbols.Elapsed += TmrRefreshSymbols_Elapsed;
            tmrRefreshSymbols.Interval = 1000 * REFRESH_SYMBOLS_SECONDS;
            tmrRefreshSymbols.Start();
        }

        #endregion

        #region Public Events

        public event EventHandler<CryptoCurrencyUpdatedEventArgs> CryptoCurrencyUpdated;

        #endregion

        #region Public Methods

        public async Task<List<CryptoSymbolGeneralInfo>> GetAllSymbols()
        {
            if (symbolsGeneralInfo == null)
            {
                await LoadCurrenciesGeneralInfo();
            }
            return symbolsGeneralInfo;
        }

        public async void GetLatestSymbols()
        {
            await refreshLock.WaitAsync();
            try
            {
                if (latestSymbols == null || (DateTime.Now - lastRefresh).TotalSeconds > 2 * REFRESH_SYMBOLS_SECONDS)
                {
                    await LoadSymbolsData();
                }
                else
                {
                    CryptoCurrencyUpdated?.Invoke(this, new CryptoCurrencyUpdatedEventArgs(latestSymbols));
                }
            }
            finally
            {
                refreshLock.Release();
            }
        }

        #endregion

        private async Task LoadCurrenciesGeneralInfo()
        {
            try
            {
                var client = new HttpClient();
                string url = ENDPOINT_URI + ENDPOINT_CURRENCIES_GENERAL_INFO;
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not fetch General Info! Status Code: {response.StatusCode} Message: {response.ReasonPhrase}");
                    return;
                }

                string body = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(body);
                symbolsGeneralInfo = obj["symbols"].ToObject<List<CryptoSymbolGeneralInfo>>();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading General Info: {ex}");
            }
        }

        private async Task LoadSymbolsData()
        {
            try
            {
                var client = new HttpClient();
                string url = ENDPOINT_URI + ENDPOINT_SYMBOLS_PRICES;
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not fetch Symbols Data! Status Code: {response.StatusCode} Message: {response.ReasonPhrase}");
                    return;
                }

                string body = await response.Content.ReadAsStringAsync();
                JArray jarr = JArray.Parse(body);
                latestSymbols = jarr.ToObject<List<CryptoSymbolData>>();
                lastRefresh = DateTime.Now;
                CryptoCurrencyUpdated?.Invoke(this, new CryptoCurrencyUpdatedEventArgs(latestSymbols));
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error fetching Symbols Data: {ex}");
            }
        }

        private async void TmrRefreshSymbols_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (CryptoCurrencyUpdated == null)
            {
                return;
            }

            await LoadSymbolsData();
        }

    }
}

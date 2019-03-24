using BarRaider.SdTools;
using BarRaider.StockTicker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace StockTicker
{
    [PluginActionId("com.barraider.cryptoticker")]
    public class CryptoTicker : PluginBase
    {
        #region Settings

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.BaseCurrency = "BTC";
                instance.Quote = "TUSD";
                instance.ForegroundColor = "#ffffff";
                instance.BackgroundColor = "#000000";

                return instance;
            }

            [JsonProperty(PropertyName = "currenciesSelected")]
            public string BaseCurrency { get; set; }

            [JsonProperty(PropertyName = "quotesSelected")]
            public string Quote { get; set; }

            [JsonProperty(PropertyName = "currencies")]
            public List<string> Currencies { get; set; }

            [JsonProperty(PropertyName = "quotes")]
            public List<string> Quotes { get; set; }

            [JsonProperty(PropertyName = "foregroundColor")]
            public string ForegroundColor { get; set; }

            [JsonProperty(PropertyName = "backgroundColor")]
            public string BackgroundColor { get; set; }
        }

        #endregion

        private PluginSettings settings;
        private CryptoSymbolData symbolData;

        public string CryptoSymbol
        {
            get
            {
                return $"{settings.BaseCurrency}{settings.Quote}";
            }
        }
        
        public CryptoTicker(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0) // Called the first time you drop a new action into the Stream Deck
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            CryptoComm.Instance.CryptoCurrencyUpdated += Instance_CryptoCurrencyUpdated;

            LoadCurrencyLists();
            CryptoComm.Instance.GetLatestSymbols();
        }

        public override void Dispose()
        {
            CryptoComm.Instance.CryptoCurrencyUpdated -= Instance_CryptoCurrencyUpdated;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor Called");
        }

        public override void KeyPressed(KeyPayload payload) { }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            if (symbolData == null)
            {
                return;
            }

            await DrawCurrencyData(symbolData.Price);
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            LoadCurrencyLists();
            CryptoComm.Instance.GetLatestSymbols();
            
        }

        private void Instance_CryptoCurrencyUpdated(object sender, CryptoCurrencyUpdatedEventArgs e)
        {
            symbolData = e.Symbols.FirstOrDefault(x => x.SymbolName == CryptoSymbol);
        }

        private void LoadCurrencyLists()
        {
            var symbols = CryptoComm.Instance.GetAllSymbols();

            if (symbols != null)
            {
                settings.Currencies = symbols.Select(x => x.BaseCurrency).Distinct().ToList();
                settings.Currencies.Sort();
                settings.Quotes = symbols.Where(x => x.BaseCurrency == settings.BaseCurrency).Select(x => x.Quote).ToList();
            }
            Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private async Task DrawCurrencyData(double currency)
        {
            try
            {
                Graphics graphics;
                Bitmap bmp = Tools.GenerateKeyImage(out graphics);

                SizeF stringSize;
                float stringPos;
                var fontDefault = new Font("Verdana", 10, FontStyle.Bold);
                var fontCurrency = new Font("Verdana", 11, FontStyle.Bold);

                // Background
                var bgBrush = new SolidBrush(ColorTranslator.FromHtml(settings.BackgroundColor));
                var fgBrush = new SolidBrush(ColorTranslator.FromHtml(settings.ForegroundColor));
                graphics.FillRectangle(bgBrush, 0, 0, Tools.KEY_DEFAULT_WIDTH, Tools.KEY_DEFAULT_HEIGHT);

                // Top title
                string title = $"1 {settings.BaseCurrency}:";
                stringSize = graphics.MeasureString(title, fontDefault);
                stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                graphics.DrawString(title, fontDefault, fgBrush, new PointF(stringPos, 5));

                string currStr = currency.ToString("0.00######");
                int buffer = 0;

                // Start: Dynamic font size based on currency
                while (fontCurrency.Size > 8)
                {
                    buffer++;
                    stringSize = graphics.MeasureString(currStr, fontCurrency);
                    if (stringSize.Width > Tools.KEY_DEFAULT_WIDTH)
                    {
                        fontCurrency = new Font(fontCurrency.Name, fontCurrency.Size - 1, FontStyle.Bold);
                    }
                    else
                    {
                        break;
                    }
                }

                if (stringSize.Width > Tools.KEY_DEFAULT_WIDTH)
                {
                    currStr = currency.ToString("0.00#####");
                    stringPos = 0;
                }
                else
                {
                    stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                }
                // End: Dynamic font size based on currency

                graphics.DrawString(currStr, fontCurrency, fgBrush, new PointF(stringPos, 25 + (buffer * 2)));

                stringSize = graphics.MeasureString(settings.Quote, fontDefault);
                stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                graphics.DrawString(settings.Quote, fontDefault, fgBrush, new PointF(stringPos, 50));
                await Connection.SetImageAsync(bmp);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error drawing currency data {ex}");
            }
        }

    }
}

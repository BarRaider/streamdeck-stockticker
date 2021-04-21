using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using StockTicker.Backend.Crypto;
using StockTicker.Models;

namespace StockTicker.Actions
{
    [PluginActionId("com.barraider.cryptoticker")]
    public class CryptoTickerAction : PluginBase
    {
        #region Settings

        protected class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    BaseCurrency = "BTC",
                    Quote = "TUSD",
                    ForegroundColor = "#ffffff",
                    BackgroundColor = "#000000",
                    BackgroundImage = null
                };

                return instance;
            }

            [JsonProperty(PropertyName = "currenciesSelected")]
            public string BaseCurrency { get; set; }

            [JsonProperty(PropertyName = "quotesSelected")]
            public string Quote { get; set; }

            [JsonProperty(PropertyName = "currencies")]
            public List<Currency> Currencies { get; set; }

            [JsonProperty(PropertyName = "quotes")]
            public List<Quote> Quotes { get; set; }

            [JsonProperty(PropertyName = "foregroundColor")]
            public string ForegroundColor { get; set; }

            [JsonProperty(PropertyName = "backgroundColor")]
            public string BackgroundColor { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage")]
            public string BackgroundImage { get; set; }
        }

        #endregion

        private readonly object backgroundImageLock = new object();

        protected readonly PluginSettings settings;
        protected CryptoSymbolData symbolData;
        protected string defaultImageLocation = null;
        private Image backgroundImage;

        public string CryptoSymbol
        {
            get
            {
                return $"{settings.BaseCurrency}{settings.Quote}";
            }
        }
        
        public CryptoTickerAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0) // Called the first time you drop a new action into the Stream Deck
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            CryptoComm.Instance.CryptoCurrencyUpdated += Instance_CryptoCurrencyUpdated;

            LoadCurrencyLists();
            CryptoComm.Instance.GetLatestSymbols();
            InitializeSettings();
            SaveSettings();
        }

        public override void Dispose()
        {
            CryptoComm.Instance.CryptoCurrencyUpdated -= Instance_CryptoCurrencyUpdated;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) { }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            if (symbolData == null)
            {
                return;
            }

            await DrawCurrencyData(symbolData.Price, 1);
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            LoadCurrencyLists();
            CryptoComm.Instance.GetLatestSymbols();
            InitializeSettings();
            SaveSettings();
        }

        private void Instance_CryptoCurrencyUpdated(object sender, CryptoCurrencyUpdatedEventArgs e)
        {
            symbolData = e.Symbols.FirstOrDefault(x => x.SymbolName == CryptoSymbol);
        }

        private void LoadCurrencyLists()
        {
            var symbols = CryptoComm.Instance.GetAllSymbols().GetAwaiter().GetResult();

            if (symbols != null)
            {
                settings.Currencies = symbols.Select(x => new Currency() { Name = x.BaseCurrency }).GroupBy(c => c.Name).Select(g => g.First()).OrderBy(c => c.Name).ToList();
                settings.Quotes = symbols.Where(x => x.BaseCurrency == settings.BaseCurrency).Select(x => new Quote() { Name = x.Quote }).OrderBy(q => q.Name).ToList();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} LoadCurrencyLists returned no symbols");
            }
        }

        protected async Task DrawCurrencyData(double currency, double multiplier)
        {
            const int STARTING_TEXT_Y = 3;
            const int CURRENCY_BUFFER_Y = 16;
            try
            {
                using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    int height = bmp.Height;
                    int width = bmp.Width;

                    var fontDefault = new Font("Verdana", 28, FontStyle.Bold, GraphicsUnit.Pixel);
                    var fontCurrency = new Font("Verdana", 28, FontStyle.Bold, GraphicsUnit.Pixel);

                    // Background
                    if (backgroundImage != null)
                    {
                        lock (backgroundImageLock)
                        {
                            if (backgroundImage != null)
                            {
                                graphics.DrawImage(backgroundImage, 0, 0, width, height);
                            }
                        }
                    }
                    else
                    {
                        var bgBrush = new SolidBrush(ColorTranslator.FromHtml(settings.BackgroundColor));
                        graphics.FillRectangle(bgBrush, 0, 0, width, height);
                    }
                    var fgBrush = new SolidBrush(ColorTranslator.FromHtml(settings.ForegroundColor));


                    // Top title
                    string title = $"{multiplier} {settings.BaseCurrency}:";
                    float stringHeight = STARTING_TEXT_Y;
                    float stringWidth = graphics.GetTextCenter(title, width, fontDefault);
                    stringHeight = graphics.DrawAndMeasureString(title, fontDefault, fgBrush, new PointF(stringWidth, stringHeight)) + CURRENCY_BUFFER_Y;
                    string currStr = currency.ToString("0.00######");

                    // Start: Dynamic font size based on currency
                    float fontSize = graphics.GetFontSizeWhereTextFitsImage(currStr, width, fontCurrency, 8);
                    fontCurrency = new Font(fontCurrency.Name, fontSize, fontCurrency.Style, GraphicsUnit.Pixel);
                    stringWidth = graphics.GetTextCenter(currStr, width, fontCurrency);

                    if (stringWidth >= width)
                    {
                        currStr = currency.ToString("0.00#####");
                        stringWidth = 0;
                    }
                    // End: Dynamic font size based on currency
                    stringHeight = graphics.DrawAndMeasureString(currStr, fontCurrency, fgBrush, new PointF(stringWidth, stringHeight));

                    stringWidth = graphics.GetTextCenter(settings.Quote, width, fontDefault);
                    graphics.DrawAndMeasureString(settings.Quote, fontDefault, fgBrush, new PointF(stringWidth, 100));
                    await Connection.SetImageAsync(bmp);
                    graphics.Dispose();
                    fontDefault.Dispose();
                    fontCurrency.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"DrawCurrencyData Error drawing currency data {ex}");
            }
        }

        protected virtual void InitializeSettings()
        {
            PrefetchBackgroundImage();
        }

        private void PrefetchBackgroundImage()
        {
            lock (backgroundImageLock)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.Dispose();
                    backgroundImage = null;
                }

                if (!String.IsNullOrEmpty(settings.BackgroundImage))
                {
                    if (!File.Exists(settings.BackgroundImage))
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} Background image file not found {settings.BackgroundImage}");
                    }
                    else
                    {
                        backgroundImage = Image.FromFile(settings.BackgroundImage);
                    }
                }
                else if (!String.IsNullOrEmpty(defaultImageLocation))
                {
                    backgroundImage = Image.FromFile(defaultImageLocation);
                }
            }
        }
        private void SaveSettings()
        {
            Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}

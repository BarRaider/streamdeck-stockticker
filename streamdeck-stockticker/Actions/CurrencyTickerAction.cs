using BarRaider.SdTools;
using StockTicker.Backend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockTicker.BarRaider.StockTicker;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;


namespace StockTicker.Actions
{
    [PluginActionId("com.barraider.currencyexchange")]
    public class CurrencyTickerAction
        : PluginBase
    {
        private const int DEFAULT_REFRESH_TIME = 120000;

        #region Settings

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    BaseCurrency = "USD",
                    Symbol = "EUR",
                    ForegroundColor = "#ffffff",
                    BackgroundColor = "#000000",
                    Multiplier = "1",
                    BackgroundImage = null
                };

                return instance;
            }

            [JsonProperty(PropertyName = "baseCurrency")]
            public string BaseCurrency { get; set; }

            [JsonProperty(PropertyName = "symbol")]
            public string Symbol { get; set; }

            [JsonProperty(PropertyName = "foregroundColor")]
            public string ForegroundColor { get; set; }

            [JsonProperty(PropertyName = "backgroundColor")]
            public string BackgroundColor { get; set; }

            [JsonProperty(PropertyName = "multiplier")]
            public string Multiplier { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage")]
            public string BackgroundImage { get; set; }
        }

        #endregion
        
        private readonly object backgroundImageLock = new object();

        private readonly PluginSettings settings;
        private DateTime lastRefresh;
        private Image backgroundImage;

        public CurrencyTickerAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {

            if (payload.Settings == null || payload.Settings.Count == 0) // Called the first time you drop a new action into the Stream Deck
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            InitializeSettings();
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public async override void OnTick()
        {
            try
            {
                if ((DateTime.Now - lastRefresh).TotalSeconds >= DEFAULT_REFRESH_TIME)
                {
                    lastRefresh = DateTime.Now;
                    JObject obj = await CurrencyManager.Instance.FetchCurrencyData(settings.BaseCurrency, settings.Symbol, DEFAULT_REFRESH_TIME);
                    if (obj != null)
                    {
                        var token = obj["rates"];

                        if (token != null)
                        {
                            var value = token[settings.Symbol];

                            await DrawCurrencyData(Convert.ToDouble(value.ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"OnTick error: {ex}");
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            InitializeSettings();
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        private async Task DrawCurrencyData(double currency)
        {
            const int STARTING_TEXT_Y = 5;
            const int CURRENCY_BUFFER_Y = 14;
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
                    if (!String.IsNullOrWhiteSpace(settings.Multiplier) && int.TryParse(settings.Multiplier, out int multiplier))
                    {
                        currency *= multiplier;
                    }

                    string title = $"{settings.Multiplier} {settings.BaseCurrency}:";
                    float stringHeight = STARTING_TEXT_Y;
                    float stringWidth = graphics.GetTextCenter(title, width, fontDefault);
                    stringHeight = graphics.DrawAndMeasureString(title, fontDefault, fgBrush, new PointF(stringWidth, stringHeight)) + CURRENCY_BUFFER_Y;

                    string currStr = currency.ToString("0.00");
                    float fontSize = graphics.GetFontSizeWhereTextFitsImage(currStr, width, fontCurrency, 8);
                    fontCurrency = new Font(fontCurrency.Name, fontSize, fontCurrency.Style, GraphicsUnit.Pixel);
                    stringWidth = graphics.GetTextCenter(currStr, width, fontCurrency);
                    stringHeight = graphics.DrawAndMeasureString(currStr, fontCurrency, fgBrush, new PointF(stringWidth, stringHeight));

                    stringWidth = graphics.GetTextCenter(settings.Symbol, width, fontDefault);
                    graphics.DrawAndMeasureString(settings.Symbol, fontDefault, fgBrush, new PointF(stringWidth, 100));
                    await Connection.SetImageAsync(bmp);
                    graphics.Dispose();
                    fontDefault.Dispose();
                    fontCurrency.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} Error drawing currency data {ex}");
            }
        }

        private void InitializeSettings()
        {
            lastRefresh = DateTime.MinValue;
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
            }
        }
        private void SaveSettings()
        {
            Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}

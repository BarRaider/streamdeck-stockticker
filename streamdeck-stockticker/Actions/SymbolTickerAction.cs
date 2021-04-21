using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.SymbolStore;
using StockTicker.Backend.Stocks;

namespace StockTicker.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // Subscriber: iMackx
    // 10 Bits: NomanSheikh
    //---------------------------------------------------
    [PluginActionId("com.barraider.stockticker")]
    public class SymbolTickerAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    SymbolName = String.Empty,
                    RefreshSeconds = 60,
                    ApiToken = String.Empty,
                    BackgroundColor = "#000000",
                    BackgroundImage = null,
                    StockProvider = StockProviders.YAHOO,
                    ModeSingle = true,
                    ModeMultiple = false,
                    RotationSpeed = DEFAULT_ROTATION_SPEED_SECONDS.ToString(),
                    Symbols = String.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "symbolName")]
            public string SymbolName { get; set; }

            [JsonProperty(PropertyName = "refreshSeconds")]
            public int RefreshSeconds { get; set; }

            [JsonProperty(PropertyName = "apiToken")]
            public string ApiToken { get; set; }

            [JsonProperty(PropertyName = "backgroundColor")]
            public string BackgroundColor { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage")]
            public string BackgroundImage { get; set; }

            [JsonProperty(PropertyName = "stockProvider")]
            public StockProviders StockProvider { get; set; }

            [JsonProperty(PropertyName = "modeSingle")]
            public bool ModeSingle { get; set; }

            [JsonProperty(PropertyName = "modeMultiple")]
            public bool ModeMultiple { get; set; }

            [JsonProperty(PropertyName = "rotationSpeed")]
            public string RotationSpeed { get; set; }

            [JsonProperty(PropertyName = "symbols")]
            public string Symbols { get; set; }
        }
        #region Private members

        private const string UP_ARROW = "▲";
        private const string DOWN_ARROW = "▼";
        private const string HIDDEN_API_TOKEN = "*****";
        private const int CLOSED_MARKET_DELAY = 600;
        private const int FORCE_REFRESH_LENGTH = 2;
        private const int DEFAULT_ROTATION_SPEED_SECONDS = 5;
        private readonly object backgroundImageLock = new object();
        private readonly PluginSettings settings;
        private readonly System.Timers.Timer tmrRotateStock = new System.Timers.Timer();

        private DateTime lastRefresh;
        private bool showDetails = false;
        protected SymbolData stockData;
        private IStockInfoProvider stockComm = null;
        private int closedMarketDelay = 0;
        private bool keyPressed = false;
        private DateTime keyPressStart;
        private Image backgroundImage;
        private int rotationSpeed = DEFAULT_ROTATION_SPEED_SECONDS;
        private string[] symbols;
        private int currentSymbol = 0;

        #endregion

        #region Public Methods

        public SymbolTickerAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            tmrRotateStock.Elapsed += TmrRotateStock_Elapsed;
            InitializeSettings();
            HideAPIKey();
            SaveSettings();
        }

        #endregion

        #region IPluginable Implementation
        public override void Dispose()
        {
            tmrRotateStock.Stop();
            tmrRotateStock.Elapsed -= TmrRotateStock_Elapsed;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} Destructor called");
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            keyPressed = true;
            keyPressStart = DateTime.Now;

            if (settings.ModeSingle)
            {
                showDetails = !showDetails;
                await DrawSymbolData(stockData);
            }
            else
            {
                TmrRotateStock_Elapsed(this, null);
                if (tmrRotateStock.Enabled) // Reset the timer length
                {
                    tmrRotateStock.Stop();
                    tmrRotateStock.Start();
                }
            }
        }

        public override void KeyReleased(KeyPayload payload)
        {
            keyPressed = false;
        }

        public override async void OnTick()
        {
            if (stockComm == null || 
               (settings.ModeSingle   && String.IsNullOrWhiteSpace(settings.SymbolName)) ||
               (settings.ModeMultiple && String.IsNullOrWhiteSpace(settings.Symbols)))
            {
                await Connection.SetImageAsync(Properties.Settings.Default.SymbolNotSet);
                return;
            }

            if (!stockComm.TokenExists())
            {
                await Connection.SetImageAsync(Properties.Settings.Default.StockNoToken);
                return;
            }

            if (keyPressed && (DateTime.Now - keyPressStart).TotalSeconds >= FORCE_REFRESH_LENGTH)
            {
                lastRefresh = DateTime.MinValue;
                await Connection.ShowOk();
            }

            if ((DateTime.Now - lastRefresh).TotalSeconds >= Math.Max(settings.RefreshSeconds, closedMarketDelay)) // Delay added if market is closed to preserve API calls
            {
                try
                {
                    lastRefresh = DateTime.Now;
                    if (stockComm != null)
                    {
                        string symbolName = null;
                        if (settings.ModeSingle)
                        {
                            symbolName = settings.SymbolName;
                        }
                        else if (settings.ModeMultiple)
                        {
                            if (currentSymbol < symbols.Length)
                            {
                                symbolName = symbols[currentSymbol];
                            }
                        }

                        if (String.IsNullOrEmpty(symbolName))
                        {
                            Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} Not Symbol Set!");
                            return;
                        }

                        stockData = await stockComm.GetSymbol(symbolName, settings.RefreshSeconds * 1000);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} OnTick Exception: {ex}");
                    return;
                }
            }

            if (stockData == null)
            {
                await Connection.SetImageAsync(System.Configuration.ConfigurationManager.AppSettings["SymbolNotSet"]);
                return;
            }

            await DrawSymbolData(stockData);
            if (stockData.IsMarketClosed) // Add a delay if the market is closed, to preserve API calls;
            {
                closedMarketDelay = CLOSED_MARKET_DELAY;
            }
            else
            {
                closedMarketDelay = 0;
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            string apiToken = settings.ApiToken;
            Tools.AutoPopulateSettings(settings, payload.Settings);
            lastRefresh = DateTime.MinValue;
            InitializeSettings();
            if (apiToken != settings.ApiToken)
            {
                if (settings.ApiToken != HIDDEN_API_TOKEN)
                {
                    stockComm.SetStockToken(settings.ApiToken);
                }
                settings.ApiToken = HIDDEN_API_TOKEN;
            }
            HideAPIKey();
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }
        #endregion

        #region Private Methods

        private async Task DrawSymbolData(SymbolData data)
        {
            const int PADDING_X = 5;
            const int STARTING_ARROW_Y = 7;
            const int STARTING_TEXT_Y = 5;
            const int CURRENCY_BUFFER_Y = 5;
            try
            {
                using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    int height = bmp.Height;
                    int width = bmp.Width;
                    string stockArrow = UP_ARROW;
                    Brush stockBrush = Brushes.Green;
                    Font fontTitle = new Font("Verdana", 26, FontStyle.Bold, GraphicsUnit.Pixel);
                    Font fontStock = new Font("Verdana", 22, FontStyle.Bold, GraphicsUnit.Pixel);
                    Font fontDetails = new Font("Verdana", 18, FontStyle.Regular, GraphicsUnit.Pixel);

                    if (showDetails) // Show market details
                    {
                        string details = $"Close:\r\n  {data.Quote.Close}\r\nHigh:\r\n  {data.Quote.High}\r\nLow:\r\n  {data.Quote.Low}";
                        graphics.DrawString(details, fontDetails, Brushes.LightGray, new PointF(0, 5));
                    }
                    else
                    {
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

                        // Top Arrow
                        if (data.Quote.Change < 0)
                        {
                            stockArrow = DOWN_ARROW;
                            stockBrush = Brushes.Red;
                        }
                        var sizeF = graphics.MeasureString(stockArrow, fontTitle);
                        graphics.DrawString(stockArrow, fontStock, stockBrush, new PointF(width - sizeF.Width - 3, STARTING_ARROW_Y));

                        // Top title
                        string title = data.SymbolName;
                        float stringHeight = STARTING_TEXT_Y;
                        stringHeight = graphics.DrawAndMeasureString(title, fontTitle, Brushes.White, new PointF(PADDING_X, stringHeight)) + CURRENCY_BUFFER_Y;

                        string stockStr = $"{data.Quote.LatestPrice}\r\n({(data.Quote.ChangePercent.HasValue ? (data.Quote.ChangePercent.Value).ToString("0.00") : "ERR")}%)";

                        float stringWidth = graphics.GetTextCenter(stockStr, width, fontStock);
                        stringHeight = graphics.DrawAndMeasureString(stockStr, fontStock, stockBrush, new PointF(stringWidth, stringHeight));

                        if (data.Quote.High != null && data.Quote.Low != null)
                        {
                            graphics.DrawAndMeasureString($"{data.Quote.High}-{data.Quote.Low}", fontDetails, Brushes.LightGray, new PointF(PADDING_X, stringHeight + 10));
                        }
                    }
                    await Connection.SetImageAsync(bmp);
                    graphics.Dispose();
                    fontTitle.Dispose();
                    fontStock.Dispose();
                    fontDetails.Dispose();
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} DrawSymbolData Exception: {ex}");
            }
        }

        private void SaveSettings()
        {
            Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void InitializeSettings()
        {
            tmrRotateStock.Stop();

            GetStockInfoProvider();

            // Backwards compatibility 
            if (!settings.ModeSingle && !settings.ModeMultiple)
            {
                settings.ModeSingle = true;
            }

            if (!Int32.TryParse(settings.RotationSpeed, out rotationSpeed))
            {
                settings.RotationSpeed = DEFAULT_ROTATION_SPEED_SECONDS.ToString();
                rotationSpeed = DEFAULT_ROTATION_SPEED_SECONDS;
            }

            if (settings.ModeMultiple)
            {
                currentSymbol = 0;
                symbols = settings.Symbols.Replace("\r\n","\n").Split('\n');
                tmrRotateStock.Interval = rotationSpeed * 1000;
                tmrRotateStock.Start();
            }
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

        private void GetStockInfoProvider()
        {
            stockComm = StocksFactory.Build(settings.StockProvider);
        }

        private void HideAPIKey()
        {
            if (stockComm.TokenExists() && settings.ApiToken != HIDDEN_API_TOKEN)
            {
                this.settings.ApiToken = HIDDEN_API_TOKEN;
            }
            else if (!stockComm.TokenExists())
            {
                this.settings.ApiToken = String.Empty;
            }
        }

        private void TmrRotateStock_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            currentSymbol = (currentSymbol + 1) % symbols.Length;
            lastRefresh = DateTime.MinValue;
        }

        #endregion
    }
}

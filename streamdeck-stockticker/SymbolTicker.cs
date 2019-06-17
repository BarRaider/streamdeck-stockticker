using BarRaider.SdTools;
using BarRaider.StockTicker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker
{
    [PluginActionId("com.barraider.stockticker")]
    public class SymbolTicker : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.SymbolName = String.Empty;
                instance.RefreshSeconds = 60;
                instance.ApiToken = String.Empty;

                return instance;
            }

            [JsonProperty(PropertyName = "symbolName")]
            public string SymbolName { get; set; }

            [JsonProperty(PropertyName = "refreshSeconds")]
            public int RefreshSeconds { get; set; }

            [JsonProperty(PropertyName = "apiToken")]
            public string ApiToken { get; set; }
        }
        #region Private members

        private const string UP_ARROW = "↑";
        private const string DOWN_ARROW = "↓";
        private const string HIDDEN_API_TOKEN = "*****";


        private PluginSettings settings;
        private DateTime lastRefresh;
        private bool showDetails = false;
        private SymbolData stockData;
        private StockComm stockComm = new StockComm();

        #endregion

        #region Public Methods

        public SymbolTicker(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                if (stockComm.TokenExists)
                {
                    this.settings.ApiToken = HIDDEN_API_TOKEN;
                }
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
                if (stockComm.TokenExists && settings.ApiToken != HIDDEN_API_TOKEN)
                {
                    this.settings.ApiToken = HIDDEN_API_TOKEN;
                    SaveSettings();
                }
            }
        }

        #endregion

        #region IPluginable Implementation
        public override void Dispose()
        {
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            showDetails = !showDetails;
            await DrawSymbolData(stockData);
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override async void OnTick()
        {
            if (!TokenManager.Instance.TokenExists)
            {
                await Connection.SetImageAsync(Properties.Settings.Default.StockNoToken);
                return;
            }

            if (String.IsNullOrWhiteSpace(settings.SymbolName))
            {
                await Connection.SetImageAsync(Properties.Settings.Default.SymbolNotSet);
                return;
            }

            if ((DateTime.Now - lastRefresh).TotalSeconds >= settings.RefreshSeconds)
            {
                try
                {
                    lastRefresh = DateTime.Now;
                    stockData = await stockComm.GetSymbol(settings.SymbolName);
                    if (stockData != null)
                    {
                        await DrawSymbolData(stockData);
                    }
                    else
                    {
                        await Connection.SetImageAsync(System.Configuration.ConfigurationManager.AppSettings["SymbolNotSet"]);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"OnTick Exception: {ex}");
                }

            }
        }

        public async override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            string apiToken = settings.ApiToken;
            Tools.AutoPopulateSettings(settings, payload.Settings);
            lastRefresh = DateTime.MinValue;
            if (apiToken != settings.ApiToken)
            {
                if (settings.ApiToken != HIDDEN_API_TOKEN)
                {
                    stockComm.SetStockToken(settings.ApiToken);
                }
                settings.ApiToken = HIDDEN_API_TOKEN;
                await SaveSettings();
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }
        #endregion

        #region Private Methods

        private async Task DrawSymbolData(SymbolData data)
        {
            try
            {
                Graphics graphics;
                Bitmap bmp = Tools.GenerateKeyImage(out graphics);
                SizeF stringSize;
                float stringPos;
                string stockArrow = UP_ARROW;
                Brush stockBrush = Brushes.Green;
                var fontDefault = new Font("Verdana", 10, FontStyle.Bold);
                var fontStock = new Font("Verdana", 8, FontStyle.Bold);
                var fontDetails = new Font("Verdana", 7, FontStyle.Regular);

                if (showDetails) // Show market details
                {
                    string details = $"Close:\r\n  {data.Quote.Close}\r\nHigh:\r\n  {data.Quote.High}\r\nLow:\r\n  {data.Quote.Low}";
                    graphics.DrawString(details, fontDetails, Brushes.LightGray, new PointF(0, 5));
                }
                else
                {
                    stringSize = graphics.MeasureString(data.SymbolName, fontDefault);
                    stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                    graphics.DrawString(data.SymbolName, fontDefault, Brushes.White, new PointF(stringPos, 5));

                    if (data.Quote.Change < 0)
                    {
                        stockArrow = DOWN_ARROW;
                        stockBrush = Brushes.Red;
                    }
                    string stockStr = $"{data.Quote.LatestPrice}\r\n({(data.Quote.ChangePercent * 100).ToString("0.00")}%)";
                    stringSize = graphics.MeasureString(stockStr, fontStock);
                    stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;

                    graphics.DrawString(stockStr, fontStock, stockBrush, new PointF(stringPos, 25));
                    graphics.DrawString(stockArrow, fontStock, stockBrush, new PointF(Tools.KEY_DEFAULT_WIDTH / 2 - 5, 25 + stringSize.Height));
                }
                string imgBase64 = Tools.ImageToBase64(bmp, true);
                await Connection.SetImageAsync(imgBase64);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"DrawSymbolData Exception: {ex}");
            }
        }

        private async Task SaveSettings()
        {
            await Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
        #endregion
    }
}

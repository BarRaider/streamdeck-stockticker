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
    public class SymbolTicker : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.SymbolName = String.Empty;
                instance.RefreshSeconds = 60;

                return instance;
            }

            [JsonProperty(PropertyName = "symbolName")]
            public string SymbolName { get; set; }

            [JsonProperty(PropertyName = "refreshSeconds")]
            public int RefreshSeconds { get; set; }
        }
        #region Private members

        private const string UP_ARROW = "↑";
        private const string DOWN_ARROW = "↓";


        private PluginSettings settings;
        private DateTime lastRefresh;
        private bool showDetails = false;
        private SymbolData stockData;

        #endregion

        #region Public Methods

        public SymbolTicker(SDConnection connection, JObject settings) : base(connection, settings)
        {
            if (settings == null || settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = settings.ToObject<PluginSettings>();
            }
        }

        #endregion

        #region IPluginable Implementation
        public override void Dispose()
        {
        }

        public async override void KeyPressed()
        {
            showDetails = !showDetails;
            await DrawSymbolData(stockData);
        }

        public override void KeyReleased()
        {
        }

        public override async void OnTick()
        {
            if (String.IsNullOrWhiteSpace(settings.SymbolName))
            {
                await Connection.SetImageAsync(System.Configuration.ConfigurationManager.AppSettings["SymbolNotSet"]);
                return;
            }

            if ((DateTime.Now - lastRefresh).TotalSeconds >= settings.RefreshSeconds)
            {
                try
                {
                    lastRefresh = DateTime.Now;
                    stockData = await StockManager.Instance.GetSymbol(settings.SymbolName);
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

        public override async void UpdateSettings(JObject payload)
        {
            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLower())
                {
                    case "propertyinspectorconnected":
                        await Connection.SendToPropertyInspectorAsync(JObject.FromObject(settings));
                        break;

                    case "propertyinspectorwilldisappear":
                        await Connection.SetSettingsAsync(JObject.FromObject(settings));
                        break;

                    case "updatesettings":
                        settings.SymbolName = (string)payload["symbolName"];
                        settings.RefreshSeconds = (int)payload["refreshSeconds"];
                        lastRefresh = DateTime.MinValue;
                        await Connection.SetSettingsAsync(JObject.FromObject(settings));
                        await Connection.SendToPropertyInspectorAsync(JObject.FromObject(settings));
                        break;
                }
            }
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

        #endregion
    }
}

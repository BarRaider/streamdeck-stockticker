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

        public override void KeyPressed()
        {
        }

        public override void KeyReleased()
        {
        }

        public override async void OnTick()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"OnTick Start");
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
                    SymbolData data = await StockManager.Instance.GetSymbol(settings.SymbolName);
                    if (data != null)
                    {
                        await DrawSymbolData(data);
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
            Logger.Instance.LogMessage(TracingLevel.INFO, $"OnTick End");
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
            Logger.Instance.LogMessage(TracingLevel.INFO, $"DrawSymbolData Start");
            try
            {
                //MemoryStream ms = GenerateSymbolChart(data);
                //Image imgChart = Image.FromStream(ms);
                //Graphics graphicsChart = Graphics.FromImage(imgChart);
                Graphics graphics;
                Bitmap bmp = Tools.GenerateKeyImage(out graphics);
                string stockArrow = UP_ARROW;
                Brush stockBrush = Brushes.Green;
                var fontDefault = new Font("Verdana", 10, FontStyle.Bold);
                var fontStock = new Font("Verdana", 8, FontStyle.Bold);

                //graphics.DrawImage(imgChart, 0, 0, Tools.KEY_DEFAULT_WIDTH, Tools.KEY_DEFAULT_HEIGHT);

                SizeF size = graphics.MeasureString(data.SymbolName, fontDefault);
                float pos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - size.Width)) / 2;
                graphics.DrawString(data.SymbolName, fontDefault, Brushes.White, new PointF(pos, 5));

                if (data.Quote.Change < 0)
                {
                    stockArrow = DOWN_ARROW;
                    stockBrush = Brushes.Red;
                }
                string stockStr = $"{data.Quote.LatestPrice}\r\n({(data.Quote.ChangePercent * 100).ToString("0.00")}%)";
                size = graphics.MeasureString(stockStr, fontStock);
                pos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - size.Width)) / 2;

                graphics.DrawString(stockStr, fontStock, stockBrush, new PointF(pos, 25));
                graphics.DrawString(stockArrow, fontStock, stockBrush, new PointF(Tools.KEY_DEFAULT_WIDTH / 2 - 5, 25 + size.Height));
                string imgBase64 = Tools.ImageToBase64(bmp, true);
                await Connection.SetImageAsync(imgBase64);
                //bmp.Save($"{data.SymbolName}{DateTime.Now.ToString("HHmmss")}.png");
                //imgChart.Save($"{data.SymbolName}{DateTime.Now.ToString("HH:mm:ss")}.png");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"DrawSymbolData Exception: {ex}");
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"DrawSymbolData End");
        }

        #endregion
    }
}
